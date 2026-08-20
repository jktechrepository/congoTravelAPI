using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.Repositories;
using CongoTravel.Services.SiteTouristique.Strategies;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueCommandeFlexPayService
    {
        Task<SiteTouristiqueReservationWithPaiementResponseDto> InitiateElectronicAsync(
            SiteTouristiqueReservationWithPaiementRequestDto request, int idSociete, int effectiveIdSite,
            CancellationToken cancellationToken = default);
        Task<SiteTouristiqueReservation> FinalizeCommandeSuccessAsync(
            SiteTouristiqueCommandeEnAttente commande, SiteTouristiquePayment payment,
            CancellationToken cancellationToken = default);
        Task FailCommandeAsync(SiteTouristiqueCommandeEnAttente commande, SiteTouristiquePayment? payment,
            CancellationToken cancellationToken = default);
    }

    public class SiteTouristiqueCommandeFlexPayService : ISiteTouristiqueCommandeFlexPayService
    {
        private const int MaxReferenceAttempts = 10;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly CongoTravelDbContext _context;
        private readonly ISiteTouristiqueInventoryHoldStrategyFactory _holdStrategies;
        private readonly ISiteTouristiqueInventoryCancelStrategyFactory _cancelStrategies;
        private readonly ISiteTouristiqueReservationConfirmationService _confirmation;
        private readonly IConfigSocieteRepository _config;
        private readonly IFlexPayService _flexPay;
        private readonly IHttpContextAccessor _http;
        private readonly FlexPayOptions _options;
        private readonly IInfoPaiementResolutionService _info;
        private readonly IDeviseMontantConverter _converter;
        private readonly ICurrentUserService? _currentUser;
        private readonly ILogger<SiteTouristiqueCommandeFlexPayService> _logger;

        public SiteTouristiqueCommandeFlexPayService(CongoTravelDbContext context,
            ISiteTouristiqueInventoryHoldStrategyFactory holdStrategies,
            ISiteTouristiqueInventoryCancelStrategyFactory cancelStrategies,
            ISiteTouristiqueReservationConfirmationService confirmation,
            IConfigSocieteRepository config, IFlexPayService flexPay, IHttpContextAccessor http,
            IOptions<FlexPayOptions> options, IInfoPaiementResolutionService info,
            IDeviseMontantConverter converter, ILogger<SiteTouristiqueCommandeFlexPayService> logger,
            ICurrentUserService? currentUser = null)
        {
            _context = context; _holdStrategies = holdStrategies; _cancelStrategies = cancelStrategies;
            _confirmation = confirmation; _config = config; _flexPay = flexPay; _http = http;
            _options = options.Value; _info = info; _converter = converter; _logger = logger; _currentUser = currentUser;
        }

        public async Task<SiteTouristiqueReservationWithPaiementResponseDto> InitiateElectronicAsync(
            SiteTouristiqueReservationWithPaiementRequestDto request, int idSociete, int effectiveIdSite,
            CancellationToken cancellationToken = default)
        {
            MethodePaiementHelper.EnsureElectronicOnly(request.Paiement.MethodePaiement);
            var methode = MethodePaiementHelper.NormalizeForStorage(request.Paiement.MethodePaiement);
            if (!_options.IsSiteTouristiqueEnabled) throw new InvalidOperationException("Le paiement électronique FlexPay site touristique n'est pas activé sur cet environnement.");
            if (methode == MethodePaiementHelper.MobileMoney && string.IsNullOrWhiteSpace(request.Paiement.Phone)) throw new InvalidOperationException("Le numéro de téléphone est requis pour MOBILE_MONEY.");
            var info = await _info.ResolveActiveForSiteAsync(effectiveIdSite, idSociete, cancellationToken);
            if (methode == MethodePaiementHelper.MobileMoney && !info.ActifMobileMoney) throw new InvalidOperationException("Mobile Money désactivé pour ce site.");
            if (methode == MethodePaiementHelper.CarteBancaire && !info.ActifCarteBancaire) throw new InvalidOperationException("Carte bancaire désactivée pour ce site.");

            var key = SiteTouristiqueIdempotencyHelper.NormalizeKey(request.Paiement.IdempotencyKey)
                ?? SiteTouristiqueIdempotencyHelper.NormalizeKey(request.IdempotencyKey);
            if (key != null)
            {
                var existing = await _context.SiteTouristiqueCommandesEnAttente.AsNoTracking().FirstOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
                if (existing != null)
                {
                    var oldPayment = existing.IdPaiementEnAttente is int id ? await _context.SiteTouristiquePayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdSiteTouristiquePayment == id, cancellationToken) : null;
                    return BuildPendingResponse(existing, oldPayment, true);
                }
            }
            await _config.EnsureReservationsActivesAsync(idSociete, cancellationToken);

            SiteTouristiqueCommandeEnAttente? commande = null;
            try
            {
                var journee = await LoadJourneeAsync(request.IdSiteTouristiqueJournee, idSociete, cancellationToken);
                var utcNow = DateTime.UtcNow;
                var expires = SiteTouristiqueHoldDurationHelper.ComputeExpiresAtUtc(utcNow, await _config.GetOrCreateAsync(idSociete, cancellationToken));
                var holdRequest = new SiteTouristiqueInventoryHoldRequest { Journee = journee, Items = request.Items, HoldExpiresAtUtc = expires };
                if (journee.InventoryMode == SiteTouristiqueInventoryMode.GlobalQuota)
                {
                    if (journee.GlobalQuota == null) throw new InvalidOperationException("Inventaire global manquant pour cette journée.");
                    holdRequest.PrixUnitaire = journee.GlobalQuota.PrixUnitaire; holdRequest.CodeDevise = journee.CodeDevise;
                }
                var hold = await _holdStrategies.GetStrategy(journee.InventoryMode).ReserveHoldAsync(holdRequest, cancellationToken);
                var codeTarif = hold.Lines.First().CodeDevise;
                var paymentCurrency = FlexPayCurrencyPolicy.NormalizePaymentCurrencyOrThrow(string.IsNullOrWhiteSpace(request.Paiement.CodeDevisePaiement) ? codeTarif : request.Paiement.CodeDevisePaiement.Trim().ToUpperInvariant(), "FlexPay site touristique");
                FlexPayCurrencyPolicy.EnsureChannelCurrencySupported(_options, methode, paymentCurrency, "FlexPay site touristique");
                var amount = hold.MontantSousTotal; var rate = 1m;
                if (codeTarif != paymentCurrency)
                {
                    var conversion = await _converter.ConvertAsync(idSociete, amount, codeTarif, paymentCurrency, utcNow, cancellationToken);
                    amount = conversion.MontantCible; rate = conversion.Taux;
                }
                if (paymentCurrency == "CDF") amount = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
                var buyer = await ResolveBuyerAsync(request.IdClient, cancellationToken);
                var snapshot = new SiteTouristiqueCommandeSnapshotDto
                {
                    Request = request,
                    Lines = hold.Lines.Select(l => new SiteTouristiqueCommandeSnapshotLineDto { LineType = l.LineType, Quantite = l.Quantite, PrixUnitaire = l.PrixUnitaire, CodeDevise = l.CodeDevise, IdSiteTouristiqueClassQuota = l.IdSiteTouristiqueClassQuota }).ToList(),
                    MontantSousTotal = hold.MontantSousTotal, CodeDevise = codeTarif,
                    ReferenceReservation = await GenerateReservationReferenceAsync(idSociete, cancellationToken),
                    CustomerRef = string.IsNullOrWhiteSpace(request.CustomerRef) ? null : request.CustomerRef.Trim()
                };
                commande = new SiteTouristiqueCommandeEnAttente
                {
                    IdSociete = idSociete, IdSiteTouristiqueJournee = journee.IdSiteTouristiqueJournee, IdSite = effectiveIdSite,
                    IdUtilisateur = buyer.IdUtilisateur, IdClient = buyer.IdClient, MethodePaiement = methode,
                    MontantTarif = hold.MontantSousTotal, CodeDeviseTarif = codeTarif, MontantFlexPay = amount,
                    CodeDevisePaiement = paymentCurrency, TauxVersDevisePaiement = rate, IdempotencyKey = key,
                    PayloadMetierJson = JsonSerializer.Serialize(snapshot, JsonOptions), DateCreation = utcNow, DateExpiration = expires
                };
                _context.SiteTouristiqueCommandesEnAttente.Add(commande); await _context.SaveChangesAsync(cancellationToken);
                var pendingOrder = SiteTouristiqueFlexPayReferenceHelper.BuildPendingOrderNumberForCommande(commande.IdSiteTouristiqueCommandeEnAttente);
                var payment = new SiteTouristiquePayment { IdSiteTouristiqueReservation = null, IdSiteTouristiqueCommandeEnAttente = commande.IdSiteTouristiqueCommandeEnAttente, IdSite = effectiveIdSite, ReferencePaiement = await GeneratePaymentReferenceAsync(idSociete, cancellationToken), Provider = SiteTouristiqueFlexPayConstants.Provider, ProviderTxRef = pendingOrder, Status = SiteTouristiquePaymentStatus.PENDING, Montant = amount, CodeDevise = paymentCurrency, MontantTarif = hold.MontantSousTotal, CodeDeviseTarif = codeTarif, TauxVersDevisePaiement = rate, IdempotencyKey = key, DateCreation = utcNow };
                _context.SiteTouristiquePayments.Add(payment); await _context.SaveChangesAsync(cancellationToken);
                commande.IdPaiementEnAttente = payment.IdSiteTouristiquePayment; commande.OrderNumberFlexPay = pendingOrder; commande.ReferenceFlexPay = SiteTouristiqueFlexPayReferenceHelper.BuildMerchantReferenceForCommande(commande.IdSiteTouristiqueCommandeEnAttente); await _context.SaveChangesAsync(cancellationToken);
                var callback = FlexPayUrlHelper.ResolveSiteTouristiqueCallbackUrl(_http.HttpContext, _options.CallbackBaseUrl, _options.SiteTouristiqueCallbackRelativePath, _options.ForceProductionCallbackInDev);
                FlexPayPaymentResponseDto response = methode == MethodePaiementHelper.CarteBancaire
                    ? await _flexPay.InitierPaiementCarteV1Async(info.CodeMarchand, info.ApiToken, commande.ReferenceFlexPay, amount, paymentCurrency, $"Réservation site touristique {snapshot.ReferenceReservation}", callback, FlexPayUrlHelper.DeriveRedirectUrl(callback, "approve"), FlexPayUrlHelper.DeriveRedirectUrl(callback, "cancel"), FlexPayUrlHelper.DeriveRedirectUrl(callback, "decline"), cancellationToken)
                    : await _flexPay.InitierPaiementMobileMoneyAsync(info.CodeMarchand, info.ApiToken, commande.ReferenceFlexPay, request.Paiement.Phone!.Trim(), amount, paymentCurrency, callback, cancellationToken);
                var order = string.IsNullOrWhiteSpace(response.OrderNumber) ? pendingOrder : response.OrderNumber.Trim();
                payment.ProviderTxRef = order; payment.DateModification = DateTime.UtcNow; commande.OrderNumberFlexPay = order; await _context.SaveChangesAsync(cancellationToken);
                if (!response.IsSuccess) { await FailCommandeAsync(commande, payment, cancellationToken); throw new InvalidOperationException($"FlexPay a refusé l'initiation : {response.Message ?? response.Code ?? "erreur"}"); }
                return BuildPendingResponse(commande, payment, false, order, response.ResolvePaymentUrl(), methode == MethodePaiementHelper.CarteBancaire ? "Redirigez le client vers paymentUrl pour finaliser le paiement carte." : "Validez le paiement sur votre téléphone Mobile Money. La réservation sera confirmée après callback.", true);
            }
            catch
            {
                if (commande != null && await _context.SiteTouristiqueCommandesEnAttente.AnyAsync(c => c.IdSiteTouristiqueCommandeEnAttente == commande.IdSiteTouristiqueCommandeEnAttente, cancellationToken))
                    await FailCommandeAsync(commande, null, cancellationToken);
                throw;
            }
        }

        public async Task<SiteTouristiqueReservation> FinalizeCommandeSuccessAsync(SiteTouristiqueCommandeEnAttente commande, SiteTouristiquePayment payment, CancellationToken cancellationToken = default)
        {
            var snapshot = JsonSerializer.Deserialize<SiteTouristiqueCommandeSnapshotDto>(commande.PayloadMetierJson, JsonOptions) ?? throw new InvalidOperationException("Payload commande site touristique invalide.");
            var reservation = new SiteTouristiqueReservation { IdSociete = commande.IdSociete, IdSiteTouristiqueJournee = commande.IdSiteTouristiqueJournee, IdSite = commande.IdSite, ReferenceReservation = snapshot.ReferenceReservation, CustomerRef = snapshot.CustomerRef, IdUtilisateur = commande.IdUtilisateur, IdClient = commande.IdClient, Status = SiteTouristiqueReservationStatus.HOLD, ExpiresAtUtc = commande.DateExpiration, MontantSousTotal = snapshot.MontantSousTotal, CodeDevise = snapshot.CodeDevise, DateCreation = DateTime.UtcNow };
            foreach (var line in snapshot.Lines) reservation.Lines.Add(new SiteTouristiqueReservationLine { LineType = line.LineType, Quantite = line.Quantite, PrixUnitaire = line.PrixUnitaire, CodeDevise = line.CodeDevise, IdSiteTouristiqueClassQuota = line.IdSiteTouristiqueClassQuota });
            _context.SiteTouristiqueReservations.Add(reservation); await _context.SaveChangesAsync(cancellationToken);
            reservation = await _context.SiteTouristiqueReservations.Include(r => r.Lines).FirstAsync(r => r.IdSiteTouristiqueReservation == reservation.IdSiteTouristiqueReservation, cancellationToken);
            var trackedPayment = await _context.SiteTouristiquePayments.FirstAsync(p => p.IdSiteTouristiquePayment == payment.IdSiteTouristiquePayment, cancellationToken);
            await _confirmation.ConfirmHoldAndEmitTicketsAsync(reservation, trackedPayment, commande.IdSociete, cancellationToken);
            trackedPayment.IdSiteTouristiqueReservation = reservation.IdSiteTouristiqueReservation; trackedPayment.IdSiteTouristiqueCommandeEnAttente = null; trackedPayment.DateModification = DateTime.UtcNow;
            var tracked = await _context.SiteTouristiqueCommandesEnAttente.FirstAsync(c => c.IdSiteTouristiqueCommandeEnAttente == commande.IdSiteTouristiqueCommandeEnAttente, cancellationToken);
            tracked.IdPaiementEnAttente = null; await _context.SaveChangesAsync(cancellationToken); _context.SiteTouristiqueCommandesEnAttente.Remove(tracked); await _context.SaveChangesAsync(cancellationToken);
            return reservation;
        }

        public async Task FailCommandeAsync(SiteTouristiqueCommandeEnAttente commande, SiteTouristiquePayment? payment, CancellationToken cancellationToken = default)
        {
            var tracked = await _context.SiteTouristiqueCommandesEnAttente.FirstOrDefaultAsync(c => c.IdSiteTouristiqueCommandeEnAttente == commande.IdSiteTouristiqueCommandeEnAttente, cancellationToken);
            if (tracked == null) return;
            var snapshot = JsonSerializer.Deserialize<SiteTouristiqueCommandeSnapshotDto>(tracked.PayloadMetierJson, JsonOptions);
            if (snapshot?.Lines.Count > 0)
            {
                var journee = await LoadJourneeAsync(tracked.IdSiteTouristiqueJournee, tracked.IdSociete, cancellationToken);
                var transient = new SiteTouristiqueReservation { IdSociete = tracked.IdSociete, IdSiteTouristiqueJournee = tracked.IdSiteTouristiqueJournee, Status = SiteTouristiqueReservationStatus.HOLD, Lines = snapshot.Lines.Select(l => new SiteTouristiqueReservationLine { LineType = l.LineType, Quantite = l.Quantite, PrixUnitaire = l.PrixUnitaire, CodeDevise = l.CodeDevise, IdSiteTouristiqueClassQuota = l.IdSiteTouristiqueClassQuota }).ToList() };
                await _cancelStrategies.GetStrategy(journee.InventoryMode).ReleaseReservationAsync(new SiteTouristiqueInventoryCancelRequest { Reservation = transient, Journee = journee, FromConfirmedSale = false }, cancellationToken);
            }
            var trackedPayment = payment == null ? await _context.SiteTouristiquePayments.FirstOrDefaultAsync(p => p.IdSiteTouristiqueCommandeEnAttente == tracked.IdSiteTouristiqueCommandeEnAttente, cancellationToken) : await _context.SiteTouristiquePayments.FirstOrDefaultAsync(p => p.IdSiteTouristiquePayment == payment.IdSiteTouristiquePayment, cancellationToken);
            if (trackedPayment != null && trackedPayment.Status is not (SiteTouristiquePaymentStatus.SUCCEEDED or SiteTouristiquePaymentStatus.REFUNDED)) { trackedPayment.Status = SiteTouristiquePaymentStatus.FAILED; trackedPayment.IdSiteTouristiqueCommandeEnAttente = null; trackedPayment.DateModification = DateTime.UtcNow; }
            tracked.IdPaiementEnAttente = null; await _context.SaveChangesAsync(cancellationToken); _context.SiteTouristiqueCommandesEnAttente.Remove(tracked); await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<SiteTouristiqueJournee> LoadJourneeAsync(int id, int societe, CancellationToken ct)
        {
            var journee = await _context.SiteTouristiqueJournees.Include(j => j.GlobalQuota).Include(j => j.ClassQuotas).Include(j => j.Lieu)
                .FirstOrDefaultAsync(j => j.IdSiteTouristiqueJournee == id && j.IdSociete == societe, ct)
                ?? throw new KeyNotFoundException($"Journée site touristique {id} introuvable pour la société {societe}.");
            SiteTouristiqueJourneeSalesEligibilityHelper.EnsureCanSell(journee, DateTime.UtcNow);
            return journee;
        }
        private async Task<(int? IdUtilisateur, int? IdClient)> ResolveBuyerAsync(int? requested, CancellationToken ct) { var user = _currentUser?.UserId ?? 0; var client = requested is > 0 ? requested : null; if (user > 0) { client ??= await _context.Utilisateurs.AsNoTracking().Where(u => u.IdUtilisateur == user).Select(u => u.IdClient).FirstOrDefaultAsync(ct); return (user, client); } return (null, client); }
        private async Task<string> GenerateReservationReferenceAsync(int societe, CancellationToken ct) { for (var i = 0; i < MaxReferenceAttempts; i++) { var v = SiteTouristiqueReferenceGenerator.GenerateReservationReferenceCandidate(societe); if (!await _context.SiteTouristiqueReservations.AsNoTracking().AnyAsync(r => r.IdSociete == societe && r.ReferenceReservation == v, ct)) return v; } throw new InvalidOperationException("Impossible de générer une référence de réservation site touristique unique."); }
        private async Task<string> GeneratePaymentReferenceAsync(int societe, CancellationToken ct) { for (var i = 0; i < MaxReferenceAttempts; i++) { var v = SiteTouristiqueReferenceGenerator.GeneratePaymentReferenceCandidate(societe); if (!await _context.SiteTouristiquePayments.AsNoTracking().AnyAsync(p => p.ReferencePaiement == v, ct)) return v; } throw new InvalidOperationException("Impossible de générer une référence de paiement site touristique unique."); }
        private static SiteTouristiqueReservationWithPaiementResponseDto BuildPendingResponse(SiteTouristiqueCommandeEnAttente c, SiteTouristiquePayment? p, bool already, string? order = null, string? url = null, string? message = null, bool? accepted = null) { var s = JsonSerializer.Deserialize<SiteTouristiqueCommandeSnapshotDto>(c.PayloadMetierJson, JsonOptions); return new() { Reservation = new SiteTouristiqueReservationResponseDto { IdSiteTouristiqueReservation = 0, IdSociete = c.IdSociete, IdSiteTouristiqueJournee = c.IdSiteTouristiqueJournee, IdSite = c.IdSite, ReferenceReservation = s?.ReferenceReservation ?? "", CustomerRef = s?.CustomerRef, IdUtilisateur = c.IdUtilisateur, IdClient = c.IdClient, Status = "EN_ATTENTE_PAIEMENT", ExpiresAtUtc = c.DateExpiration, MontantSousTotal = c.MontantTarif, CodeDevise = c.CodeDeviseTarif, DateCreation = c.DateCreation }, Payment = p == null ? null : new SiteTouristiquePaymentResponseDto { IdSiteTouristiquePayment = p.IdSiteTouristiquePayment, IdSite = p.IdSite, ReferencePaiement = p.ReferencePaiement, Provider = p.Provider, ProviderTxRef = p.ProviderTxRef, Status = p.Status.ToString(), Montant = p.Montant, CodeDevise = p.CodeDevise, MontantTarif = p.MontantTarif, CodeDeviseTarif = p.CodeDeviseTarif, TauxVersDevisePaiement = p.TauxVersDevisePaiement, DateCreation = p.DateCreation }, TransactionStatut = "EnAttente", Message = message ?? (already ? "Paiement FlexPay déjà initié pour cette clé d'idempotence." : "Paiement FlexPay initié. Aucune réservation tant que le paiement n'est pas confirmé."), OrderNumber = order ?? c.OrderNumberFlexPay ?? p?.ProviderTxRef, PaymentUrl = url, ReservationExpiresAtUtc = c.DateExpiration, MontantFlexPay = c.MontantFlexPay, CodeDevisePaiement = c.CodeDevisePaiement, MontantTarif = c.MontantTarif, CodeDeviseTarif = c.CodeDeviseTarif, TauxApplique = c.TauxVersDevisePaiement, FlexPayAccepted = accepted ?? true, AlreadyInitiated = already }; }
    }
}
