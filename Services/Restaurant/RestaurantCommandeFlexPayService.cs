using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Repositories;
using CongoTravel.Services.Restaurant.Strategies;

namespace CongoTravel.Services.Restaurant
{
    public interface IRestaurantCommandeFlexPayService
    {
        Task<RestaurantReservationWithPaiementResponseDto> InitiateElectronicAsync(RestaurantReservationWithPaiementRequestDto request, int idSociete, int effectiveIdSite, CancellationToken cancellationToken = default);
        Task<RestaurantReservation> FinalizeCommandeSuccessAsync(RestaurantCommandeEnAttente commande, RestaurantPayment payment, CancellationToken cancellationToken = default);
        Task FailCommandeAsync(RestaurantCommandeEnAttente commande, RestaurantPayment? payment, CancellationToken cancellationToken = default);
    }

    public class RestaurantCommandeFlexPayService : IRestaurantCommandeFlexPayService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly CongoTravelDbContext _context;
        private readonly IRestaurantInventoryHoldStrategyFactory _holdStrategies;
        private readonly IRestaurantInventoryCancelStrategyFactory _cancelStrategies;
        private readonly IRestaurantReservationConfirmationService _confirmation;
        private readonly IConfigSocieteRepository _config;
        private readonly IFlexPayService _flexPay;
        private readonly IHttpContextAccessor _http;
        private readonly FlexPayOptions _options;
        private readonly IInfoPaiementResolutionService _infoPaiement;
        private readonly IDeviseMontantConverter _converter;
        private readonly ICurrentUserService? _currentUser;
        private readonly ILogger<RestaurantCommandeFlexPayService> _logger;

        public RestaurantCommandeFlexPayService(CongoTravelDbContext context, IRestaurantInventoryHoldStrategyFactory holdStrategies,
            IRestaurantInventoryCancelStrategyFactory cancelStrategies, IRestaurantReservationConfirmationService confirmation,
            IConfigSocieteRepository config, IFlexPayService flexPay, IHttpContextAccessor http, IOptions<FlexPayOptions> options,
            IInfoPaiementResolutionService infoPaiement, IDeviseMontantConverter converter,
            ILogger<RestaurantCommandeFlexPayService> logger, ICurrentUserService? currentUser = null)
        {
            _context = context; _holdStrategies = holdStrategies; _cancelStrategies = cancelStrategies; _confirmation = confirmation;
            _config = config; _flexPay = flexPay; _http = http; _options = options.Value; _infoPaiement = infoPaiement;
            _converter = converter; _logger = logger; _currentUser = currentUser;
        }

        public async Task<RestaurantReservationWithPaiementResponseDto> InitiateElectronicAsync(
            RestaurantReservationWithPaiementRequestDto request, int idSociete, int effectiveIdSite, CancellationToken cancellationToken = default)
        {
            MethodePaiementHelper.EnsureElectronicOnly(request.Paiement.MethodePaiement);
            var methode = MethodePaiementHelper.NormalizeForStorage(request.Paiement.MethodePaiement);
            if (!_options.IsRestaurantEnabled) throw new InvalidOperationException("Le paiement électronique FlexPay restaurant n'est pas activé sur cet environnement.");
            if (methode == MethodePaiementHelper.MobileMoney && string.IsNullOrWhiteSpace(request.Paiement.Phone)) throw new InvalidOperationException("Le numéro de téléphone est requis pour MOBILE_MONEY.");
            var info = await _infoPaiement.ResolveActiveForSiteAsync(effectiveIdSite, idSociete, cancellationToken);
            if (methode == MethodePaiementHelper.MobileMoney && !info.ActifMobileMoney) throw new InvalidOperationException("Mobile Money désactivé pour ce site.");
            if (methode == MethodePaiementHelper.CarteBancaire && !info.ActifCarteBancaire) throw new InvalidOperationException("Carte bancaire désactivée pour ce site.");
            var key = RestaurantIdempotencyHelper.NormalizeKey(request.Paiement.IdempotencyKey) ?? RestaurantIdempotencyHelper.NormalizeKey(request.IdempotencyKey);
            if (key != null)
            {
                var existing = await _context.RestaurantCommandesEnAttente.AsNoTracking().FirstOrDefaultAsync(c => c.IdempotencyKey == key, cancellationToken);
                if (existing != null)
                    return await BuildResponseAsync(existing, true, cancellationToken);
            }

            await _config.EnsureReservationsActivesAsync(idSociete, cancellationToken);
            RestaurantCommandeEnAttente? commande = null;
            try
            {
                var creneau = await LoadCreneauAsync(request.IdRestaurantCreneau, idSociete, cancellationToken);
                var utcNow = DateTime.UtcNow;
                var expiresAt = RestaurantHoldDurationHelper.ComputeExpiresAtUtc(utcNow, await _config.GetOrCreateAsync(idSociete, cancellationToken));
                var holdRequest = new RestaurantInventoryHoldRequest { Creneau = creneau, Items = request.Items, CodeDevise = creneau.CodeDevise, HoldExpiresAtUtc = expiresAt };
                if (creneau.InventoryMode == RestaurantInventoryMode.GlobalQuota)
                {
                    if (creneau.GlobalQuota == null) throw new InvalidOperationException("Inventaire global manquant pour ce créneau.");
                    holdRequest.PrixUnitaire = RestaurantAcompteHelper.ComputeAcompteUnitaire(creneau.MontantAcompte, creneau.GlobalQuota.PrixUnitaire, creneau.Restaurant!.AcomptePourcentDefaut);
                }
                var held = await _holdStrategies.GetStrategy(creneau.InventoryMode).ReserveHoldAsync(holdRequest, cancellationToken);
                var reference = await NewReservationReferenceAsync(idSociete, cancellationToken);
                var (user, client) = await ResolveBuyerAsync(request.IdClient, cancellationToken);
                var tarif = held.MontantSousTotal; var tarifDevise = held.Lines.First().CodeDevise;
                var payDevise = FlexPayCurrencyPolicy.NormalizePaymentCurrencyOrThrow(string.IsNullOrWhiteSpace(request.Paiement.CodeDevisePaiement) ? tarifDevise : request.Paiement.CodeDevisePaiement.Trim().ToUpperInvariant(), "FlexPay restaurant");
                FlexPayCurrencyPolicy.EnsureChannelCurrencySupported(_options, methode, payDevise, "FlexPay restaurant");
                decimal montant = tarif, taux = 1m;
                if (!string.Equals(tarifDevise, payDevise, StringComparison.Ordinal))
                {
                    var conversion = await _converter.ConvertAsync(idSociete, tarif, tarifDevise, payDevise, utcNow, cancellationToken);
                    montant = conversion.MontantCible; taux = conversion.Taux;
                }
                if (payDevise == "CDF") montant = Math.Round(montant, 0, MidpointRounding.AwayFromZero);
                var snapshot = new RestaurantCommandeSnapshotDto
                {
                    Request = request, MontantSousTotal = tarif, CodeDevise = tarifDevise, NombreCouverts = held.NombreCouverts,
                    ReferenceReservation = reference, CustomerRef = string.IsNullOrWhiteSpace(request.CustomerRef) ? null : request.CustomerRef.Trim(),
                    Lines = held.Lines.Select(l => new RestaurantCommandeSnapshotLineDto { LineType = l.LineType, Quantite = l.Quantite, PrixUnitaire = l.PrixUnitaire, MontantLigne = l.MontantLigne, CodeDevise = l.CodeDevise, IdRestaurantCreneauGlobalQuota = l.IdRestaurantCreneauGlobalQuota, IdRestaurantCreneauZoneQuota = l.IdRestaurantCreneauZoneQuota }).ToList()
                };
                commande = new RestaurantCommandeEnAttente { IdSociete = idSociete, IdRestaurant = creneau.IdRestaurant, IdRestaurantCreneau = creneau.IdRestaurantCreneau, IdSite = effectiveIdSite, IdUtilisateur = user, IdClient = client, MethodePaiement = methode, MontantTarif = tarif, CodeDeviseTarif = tarifDevise, MontantFlexPay = montant, CodeDevisePaiement = payDevise, TauxVersDevisePaiement = taux, IdempotencyKey = key, PayloadMetierJson = JsonSerializer.Serialize(snapshot, JsonOptions), DateCreation = utcNow, DateExpiration = expiresAt };
                _context.RestaurantCommandesEnAttente.Add(commande); await _context.SaveChangesAsync(cancellationToken);
                var payment = new RestaurantPayment { IdRestaurantReservation = null, IdRestaurantCommandeEnAttente = commande.IdRestaurantCommandeEnAttente, IdSite = effectiveIdSite, ReferencePaiement = await NewPaymentReferenceAsync(cancellationToken), Provider = RestaurantFlexPayConstants.Provider, ProviderTxRef = RestaurantFlexPayReferenceHelper.BuildPendingOrderNumberForCommande(commande.IdRestaurantCommandeEnAttente), Status = RestaurantPaymentStatus.PENDING, Montant = montant, CodeDevise = payDevise, MontantTarif = tarif, CodeDeviseTarif = tarifDevise, TauxVersDevisePaiement = taux, IdempotencyKey = key, DateCreation = utcNow };
                _context.RestaurantPayments.Add(payment); await _context.SaveChangesAsync(cancellationToken);
                commande.IdPaiementEnAttente = payment.IdRestaurantPayment;
                commande.ReferenceFlexPay = RestaurantFlexPayReferenceHelper.BuildMerchantReferenceForCommande(commande.IdRestaurantCommandeEnAttente);
                commande.OrderNumberFlexPay = payment.ProviderTxRef;
                await _context.SaveChangesAsync(cancellationToken);
                var callback = FlexPayUrlHelper.ResolveRestaurantCallbackUrl(_http.HttpContext, _options.CallbackBaseUrl, _options.RestaurantCallbackRelativePath, _options.ForceProductionCallbackInDev);
                FlexPayPaymentResponseDto result = methode == MethodePaiementHelper.CarteBancaire
                    ? await _flexPay.InitierPaiementCarteV1Async(info.CodeMarchand, info.ApiToken, commande.ReferenceFlexPay, montant, payDevise, $"Réservation restaurant {reference}", callback, FlexPayUrlHelper.DeriveRedirectUrl(callback, "approve"), FlexPayUrlHelper.DeriveRedirectUrl(callback, "cancel"), FlexPayUrlHelper.DeriveRedirectUrl(callback, "decline"), cancellationToken)
                    : await _flexPay.InitierPaiementMobileMoneyAsync(info.CodeMarchand, info.ApiToken, commande.ReferenceFlexPay, request.Paiement.Phone!.Trim(), montant, payDevise, callback, cancellationToken);
                payment.ProviderTxRef = string.IsNullOrWhiteSpace(result.OrderNumber) ? payment.ProviderTxRef : result.OrderNumber.Trim();
                commande.OrderNumberFlexPay = payment.ProviderTxRef; payment.DateModification = DateTime.UtcNow; await _context.SaveChangesAsync(cancellationToken);
                if (!result.IsSuccess) { await FailCommandeAsync(commande, payment, cancellationToken); throw new InvalidOperationException($"FlexPay a refusé l'initiation : {result.Message ?? result.Code ?? "erreur"}"); }
                return await BuildResponseAsync(commande, false, cancellationToken, result.ResolvePaymentUrl());
            }
            catch
            {
                if (commande != null) await FailCommandeAsync(commande, null, cancellationToken);
                throw;
            }
        }

        public async Task<RestaurantReservation> FinalizeCommandeSuccessAsync(RestaurantCommandeEnAttente commande, RestaurantPayment payment, CancellationToken cancellationToken = default)
        {
            var snapshot = JsonSerializer.Deserialize<RestaurantCommandeSnapshotDto>(commande.PayloadMetierJson, JsonOptions) ?? throw new InvalidOperationException("Payload commande restaurant invalide.");
            var reservation = new RestaurantReservation { IdSociete = commande.IdSociete, IdRestaurant = commande.IdRestaurant, IdRestaurantCreneau = commande.IdRestaurantCreneau, IdSite = commande.IdSite, IdUtilisateur = commande.IdUtilisateur, IdClient = commande.IdClient, ReferenceReservation = snapshot.ReferenceReservation, CustomerRef = snapshot.CustomerRef, Status = RestaurantReservationStatus.HOLD, ExpiresAtUtc = commande.DateExpiration, MontantSousTotal = snapshot.MontantSousTotal, CodeDevise = snapshot.CodeDevise, NombreCouverts = snapshot.NombreCouverts, DateCreation = DateTime.UtcNow };
            foreach (var l in snapshot.Lines) reservation.Lines.Add(new RestaurantReservationLine { LineType = l.LineType, Quantite = l.Quantite, PrixUnitaire = l.PrixUnitaire, MontantLigne = l.MontantLigne, CodeDevise = l.CodeDevise, IdRestaurantCreneauGlobalQuota = l.IdRestaurantCreneauGlobalQuota, IdRestaurantCreneauZoneQuota = l.IdRestaurantCreneauZoneQuota });
            _context.RestaurantReservations.Add(reservation); await _context.SaveChangesAsync(cancellationToken);
            reservation = await _context.RestaurantReservations.Include(r => r.Lines).FirstAsync(r => r.IdRestaurantReservation == reservation.IdRestaurantReservation, cancellationToken);
            var trackedPayment = await _context.RestaurantPayments.FirstAsync(p => p.IdRestaurantPayment == payment.IdRestaurantPayment, cancellationToken);
            await _confirmation.ConfirmHoldAndEmitTicketsAsync(reservation, trackedPayment, commande.IdSociete, cancellationToken);
            trackedPayment.IdRestaurantReservation = reservation.IdRestaurantReservation; trackedPayment.IdRestaurantCommandeEnAttente = null; trackedPayment.DateModification = DateTime.UtcNow;
            var tracked = await _context.RestaurantCommandesEnAttente.FirstAsync(c => c.IdRestaurantCommandeEnAttente == commande.IdRestaurantCommandeEnAttente, cancellationToken);
            tracked.IdPaiementEnAttente = null; await _context.SaveChangesAsync(cancellationToken);
            _context.RestaurantCommandesEnAttente.Remove(tracked); await _context.SaveChangesAsync(cancellationToken);
            return reservation;
        }

        public async Task FailCommandeAsync(RestaurantCommandeEnAttente commande, RestaurantPayment? payment, CancellationToken cancellationToken = default)
        {
            var tracked = await _context.RestaurantCommandesEnAttente.FirstOrDefaultAsync(c => c.IdRestaurantCommandeEnAttente == commande.IdRestaurantCommandeEnAttente, cancellationToken);
            if (tracked == null) return;
            try
            {
                var s = JsonSerializer.Deserialize<RestaurantCommandeSnapshotDto>(tracked.PayloadMetierJson, JsonOptions);
                if (s?.Lines.Count > 0)
                {
                    var creneau = await _context.RestaurantCreneaux.FirstOrDefaultAsync(c => c.IdRestaurantCreneau == tracked.IdRestaurantCreneau && c.IdSociete == tracked.IdSociete, cancellationToken);
                    if (creneau != null)
                    {
                        var transient = new RestaurantReservation { IdSociete = tracked.IdSociete, IdRestaurantCreneau = tracked.IdRestaurantCreneau, Status = RestaurantReservationStatus.HOLD, Lines = s.Lines.Select(l => new RestaurantReservationLine { LineType = l.LineType, Quantite = l.Quantite, PrixUnitaire = l.PrixUnitaire, MontantLigne = l.MontantLigne, CodeDevise = l.CodeDevise, IdRestaurantCreneauGlobalQuota = l.IdRestaurantCreneauGlobalQuota, IdRestaurantCreneauZoneQuota = l.IdRestaurantCreneauZoneQuota }).ToList() };
                        await _cancelStrategies.GetStrategy(creneau.InventoryMode).ReleaseReservationAsync(new RestaurantInventoryCancelRequest { Reservation = transient, Creneau = creneau, FromConfirmedSale = false }, cancellationToken);
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Release inventaire commande restaurant échoué — Commande={Id}", tracked.IdRestaurantCommandeEnAttente); }
            var trackedPayment = payment == null ? await _context.RestaurantPayments.FirstOrDefaultAsync(p => p.IdRestaurantCommandeEnAttente == tracked.IdRestaurantCommandeEnAttente, cancellationToken) : await _context.RestaurantPayments.FirstOrDefaultAsync(p => p.IdRestaurantPayment == payment.IdRestaurantPayment, cancellationToken);
            if (trackedPayment != null && trackedPayment.Status is not (RestaurantPaymentStatus.SUCCEEDED or RestaurantPaymentStatus.REFUNDED)) { trackedPayment.Status = RestaurantPaymentStatus.FAILED; trackedPayment.IdRestaurantCommandeEnAttente = null; trackedPayment.DateModification = DateTime.UtcNow; }
            tracked.IdPaiementEnAttente = null; await _context.SaveChangesAsync(cancellationToken); _context.RestaurantCommandesEnAttente.Remove(tracked); await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<RestaurantCreneau> LoadCreneauAsync(int id, int societe, CancellationToken ct)
        {
            var c = await _context.RestaurantCreneaux.Include(x => x.GlobalQuota).Include(x => x.ZoneQuotas).Include(x => x.Restaurant).FirstOrDefaultAsync(x => x.IdRestaurantCreneau == id && x.IdSociete == societe, ct);
            if (c == null) throw new KeyNotFoundException($"Créneau restaurant {id} introuvable pour la société {societe}.");
            if (c.Status != RestaurantStatus.Published || c.Restaurant?.Status != RestaurantStatus.Published) throw new InvalidOperationException("Le créneau et l'établissement doivent être publiés pour créer un hold.");
            return c;
        }
        private async Task<(int?, int?)> ResolveBuyerAsync(int? requested, CancellationToken ct) { var user = _currentUser?.UserId ?? 0; var client = requested is > 0 ? requested : null; if (user > 0 && client is null) client = await _context.Utilisateurs.AsNoTracking().Where(x => x.IdUtilisateur == user).Select(x => x.IdClient).FirstOrDefaultAsync(ct); return (user > 0 ? user : null, client); }
        private async Task<string> NewReservationReferenceAsync(int societe, CancellationToken ct) { for (var i = 0; i < 10; i++) { var value = RestaurantReferenceGenerator.GenerateReservationReferenceCandidate(societe); if (!await _context.RestaurantReservations.AnyAsync(x => x.IdSociete == societe && x.ReferenceReservation == value, ct)) return value; } throw new InvalidOperationException("Impossible de générer une référence de réservation restaurant unique."); }
        private async Task<string> NewPaymentReferenceAsync(CancellationToken ct) { for (var i = 0; i < 10; i++) { var value = RestaurantReferenceGenerator.GeneratePaymentReferenceCandidate(0); if (!await _context.RestaurantPayments.AnyAsync(x => x.ReferencePaiement == value, ct)) return value; } throw new InvalidOperationException("Impossible de générer une référence de paiement restaurant unique."); }
        private async Task<RestaurantReservationWithPaiementResponseDto> BuildResponseAsync(RestaurantCommandeEnAttente c, bool already, CancellationToken ct, string? paymentUrl = null)
        {
            var p = c.IdPaiementEnAttente is int id ? await _context.RestaurantPayments.AsNoTracking().FirstOrDefaultAsync(x => x.IdRestaurantPayment == id, ct) : null;
            var s = JsonSerializer.Deserialize<RestaurantCommandeSnapshotDto>(c.PayloadMetierJson, JsonOptions);
            return new RestaurantReservationWithPaiementResponseDto { Reservation = new RestaurantReservationResponseDto { IdRestaurantReservation = 0, IdSociete = c.IdSociete, IdRestaurant = c.IdRestaurant, IdRestaurantCreneau = c.IdRestaurantCreneau, IdSite = c.IdSite, IdUtilisateur = c.IdUtilisateur, IdClient = c.IdClient, ReferenceReservation = s?.ReferenceReservation ?? "", CustomerRef = s?.CustomerRef, Status = "EN_ATTENTE_PAIEMENT", ExpiresAtUtc = c.DateExpiration, MontantSousTotal = c.MontantTarif, CodeDevise = c.CodeDeviseTarif, NombreCouverts = s?.NombreCouverts ?? 0, DateCreation = c.DateCreation }, Payment = p == null ? null : RestaurantReservationMapper.ToPaymentResponse(p), TransactionStatut = "EnAttente", Message = already ? "Paiement FlexPay déjà initié pour cette clé d'idempotence." : "Paiement FlexPay initié. Aucune réservation tant que le paiement n'est pas confirmé.", OrderNumber = c.OrderNumberFlexPay ?? p?.ProviderTxRef, PaymentUrl = paymentUrl, ReservationExpiresAtUtc = c.DateExpiration, MontantFlexPay = c.MontantFlexPay, CodeDevisePaiement = c.CodeDevisePaiement, MontantTarif = c.MontantTarif, CodeDeviseTarif = c.CodeDeviseTarif, TauxApplique = c.TauxVersDevisePaiement, FlexPayAccepted = true, AlreadyInitiated = already };
        }
    }
}
