using System.Text.Json;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel.Strategies;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelCommandeFlexPayService
    {
        Task<HotelReservationWithPaiementResponseDto> InitiateElectronicAsync(HotelReservationWithPaiementRequestDto request, CancellationToken cancellationToken = default);
        Task<HotelReservation> FinalizeCommandeSuccessAsync(HotelCommandeEnAttente commande, HotelPayment payment, CancellationToken cancellationToken = default);
        Task FailCommandeAsync(HotelCommandeEnAttente commande, HotelPayment? payment, CancellationToken cancellationToken = default);
    }

    public class HotelCommandeFlexPayService : IHotelCommandeFlexPayService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly CongoTravelDbContext _context;
        private readonly IHotelInventoryHoldStrategyFactory _holdStrategyFactory;
        private readonly IHotelInventoryCancelStrategyFactory _cancelStrategyFactory;
        private readonly IHotelReservationConfirmationService _confirmation;
        private readonly IConfigSocieteRepository _config;
        private readonly IFlexPayService _flexPay;
        private readonly IHttpContextAccessor _http;
        private readonly FlexPayOptions _options;
        private readonly IInfoPaiementResolutionService _info;
        private readonly IDeviseMontantConverter _converter;
        private readonly ICurrentUserService? _currentUser;
        private readonly ILogger<HotelCommandeFlexPayService> _logger;

        public HotelCommandeFlexPayService(
            CongoTravelDbContext context, IHotelInventoryHoldStrategyFactory holdStrategyFactory,
            IHotelInventoryCancelStrategyFactory cancelStrategyFactory, IHotelReservationConfirmationService confirmation,
            IConfigSocieteRepository config, IFlexPayService flexPay, IHttpContextAccessor http,
            IOptions<FlexPayOptions> options, IInfoPaiementResolutionService info,
            IDeviseMontantConverter converter, ILogger<HotelCommandeFlexPayService> logger,
            ICurrentUserService? currentUser = null)
        {
            _context = context; _holdStrategyFactory = holdStrategyFactory; _cancelStrategyFactory = cancelStrategyFactory;
            _confirmation = confirmation; _config = config; _flexPay = flexPay; _http = http;
            _options = options.Value; _info = info; _converter = converter;
            _logger = logger; _currentUser = currentUser;
        }

        public async Task<HotelReservationWithPaiementResponseDto> InitiateElectronicAsync(
            HotelReservationWithPaiementRequestDto request, CancellationToken cancellationToken = default)
        {
            MethodePaiementHelper.EnsureElectronicOnly(request.Paiement.MethodePaiement);
            var methode = MethodePaiementHelper.NormalizeForStorage(request.Paiement.MethodePaiement);
            if (!_options.IsHotelEnabled)
                throw new InvalidOperationException("Le paiement électronique FlexPay hôtel n'est pas activé sur cet environnement.");
            if (methode == MethodePaiementHelper.MobileMoney && string.IsNullOrWhiteSpace(request.Paiement.Phone))
                throw new InvalidOperationException("Le numéro de téléphone est requis pour MOBILE_MONEY.");

            var hotel = await _context.Hotels.AsNoTracking().FirstOrDefaultAsync(
                h => h.IdHotel == request.IdHotel && h.Status == HotelStatus.Published, cancellationToken)
                ?? throw new KeyNotFoundException($"Hôtel {request.IdHotel} introuvable ou non publié.");
            if (_currentUser?.IsStaff == true && !_currentUser.IsSuperAdmin && _currentUser.SocieteId != hotel.IdSociete)
                throw new UnauthorizedAccessException("Cet hôtel n'appartient pas à la société du JWT.");
            await _config.EnsureReservationsActivesAsync(hotel.IdSociete, cancellationToken);

            var idSite = request.Paiement.IdSite ?? hotel.IdSite
                ?? throw new InvalidOperationException("Le site bénéficiaire de l'hôtel est requis.");
            var paymentInfo = await _info.ResolveActiveForSiteAsync(idSite, hotel.IdSociete, cancellationToken);
            if (methode == MethodePaiementHelper.MobileMoney && !paymentInfo.ActifMobileMoney)
                throw new InvalidOperationException("Mobile Money désactivé pour ce site.");
            if (methode == MethodePaiementHelper.CarteBancaire && !paymentInfo.ActifCarteBancaire)
                throw new InvalidOperationException("Carte bancaire désactivée pour ce site.");

            var key = string.IsNullOrWhiteSpace(request.Paiement.IdempotencyKey)
                ? (string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim())
                : request.Paiement.IdempotencyKey.Trim();
            if (key != null)
            {
                var existing = await _context.HotelCommandesEnAttente.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdempotencyKey == key, cancellationToken);
                if (existing != null) return await BuildResponseAsync(existing, true, cancellationToken);
            }

            HotelCommandeEnAttente? commande = null;
            try
            {
                var checkIn = request.CheckInDate.Date;
                var checkOut = request.CheckOutDate.Date;
                if (checkOut <= checkIn) throw new InvalidOperationException("CheckOutDate doit être postérieur à CheckInDate.");
                if (request.Items == null || request.Items.Count == 0) throw new InvalidOperationException("Au moins un item est requis.");
                var inventoryMode = HotelInventoryModeResolver.FromHoldItems(request.Items);
                var holdStrategy = _holdStrategyFactory.GetStrategy(inventoryMode);
                var held = await holdStrategy.ReserveHoldAsync(hotel.IdHotel, hotel.IdSociete, checkIn, checkOut, request.Items, cancellationToken);
                var now = DateTime.UtcNow;
                var config = await _config.GetOrCreateAsync(hotel.IdSociete, cancellationToken);
                var expires = now.AddMinutes(Math.Clamp(config.DureeHoldHotelMinutes, 1, 120));
                var tariff = decimal.Round(held.MontantSejour * Math.Clamp(hotel.AcomptePourcentDefaut, 0m, 100m) / 100m, 2);
                var payCurrency = FlexPayCurrencyPolicy.NormalizePaymentCurrencyOrThrow(
                    string.IsNullOrWhiteSpace(request.Paiement.CodeDevisePaiement) ? held.CodeDevise : request.Paiement.CodeDevisePaiement.Trim().ToUpperInvariant(),
                    "FlexPay hôtel");
                FlexPayCurrencyPolicy.EnsureChannelCurrencySupported(_options, methode, payCurrency, "FlexPay hôtel");
                decimal amount = tariff, rate = 1m;
                if (!string.Equals(held.CodeDevise, payCurrency, StringComparison.Ordinal))
                {
                    var conversion = await _converter.ConvertAsync(hotel.IdSociete, tariff, held.CodeDevise, payCurrency, now, cancellationToken);
                    amount = conversion.MontantCible; rate = conversion.Taux;
                }
                if (payCurrency == "CDF") amount = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
                int? userId = _currentUser?.UserId > 0 ? _currentUser.UserId : null;
                var clientId = request.IdClient;
                if (clientId is null && userId is > 0)
                    clientId = await _context.Utilisateurs.AsNoTracking().Where(u => u.IdUtilisateur == userId)
                        .Select(u => u.IdClient).FirstOrDefaultAsync(cancellationToken);
                var reference = $"HTL-{hotel.IdSociete}-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
                var snapshot = new HotelCommandeSnapshotDto
                {
                    Request = request,
                    ReferenceReservation = reference.Length <= 64 ? reference : reference[..64],
                    MontantSejour = held.MontantSejour,
                    MontantSousTotal = tariff,
                    CodeDevise = held.CodeDevise,
                    InventoryMode = inventoryMode,
                    Lines = held.Lines.Select(l => new HotelCommandeSnapshotLineDto
                    {
                        LineType = l.LineType.ToString(),
                        IdHotelRoomType = l.IdHotelRoomType,
                        IdHotelNight = l.IdHotelNight,
                        Quantity = l.Quantity,
                        PrixSejourUnitaire = l.PrixSejourUnitaire, MontantLigne = l.MontantLigne,
                        CodeDevise = l.CodeDevise
                    }).ToList()
                };
                commande = new HotelCommandeEnAttente
                {
                    IdSociete = hotel.IdSociete, IdHotel = hotel.IdHotel, IdSite = idSite,
                    IdUtilisateur = userId, IdClient = clientId, MethodePaiement = methode,
                    MontantTarif = tariff, CodeDeviseTarif = held.CodeDevise,
                    MontantFlexPay = amount, CodeDevisePaiement = payCurrency,
                    TauxVersDevisePaiement = rate, IdempotencyKey = key,
                    PayloadMetierJson = JsonSerializer.Serialize(snapshot, JsonOptions),
                    DateCreation = now, DateExpiration = expires
                };
                _context.HotelCommandesEnAttente.Add(commande);
                await _context.SaveChangesAsync(cancellationToken);
                var payment = new HotelPayment
                {
                    IdHotelCommandeEnAttente = commande.IdHotelCommandeEnAttente, IdSite = idSite,
                    ReferencePaiement = $"PAY-HC-{Guid.NewGuid():N}",
                    Provider = HotelFlexPayConstants.Provider,
                    ProviderTxRef = HotelFlexPayReferenceHelper.BuildPendingOrderNumberForCommande(commande.IdHotelCommandeEnAttente),
                    Status = HotelPaymentStatus.PENDING, Montant = amount, CodeDevise = payCurrency,
                    MontantTarif = tariff, CodeDeviseTarif = held.CodeDevise,
                    TauxVersDevisePaiement = rate, IdempotencyKey = key, DateCreation = now
                };
                _context.HotelPayments.Add(payment);
                await _context.SaveChangesAsync(cancellationToken);
                commande.IdPaiementEnAttente = payment.IdHotelPayment;
                commande.ReferenceFlexPay = HotelFlexPayReferenceHelper.BuildMerchantReferenceForCommande(commande.IdHotelCommandeEnAttente);
                commande.OrderNumberFlexPay = payment.ProviderTxRef;
                await _context.SaveChangesAsync(cancellationToken);
                var callback = FlexPayUrlHelper.ResolveHotelCallbackUrl(_http.HttpContext, _options.CallbackBaseUrl,
                    _options.HotelCallbackRelativePath, _options.ForceProductionCallbackInDev);
                FlexPayPaymentResponseDto response = methode == MethodePaiementHelper.CarteBancaire
                    ? await _flexPay.InitierPaiementCarteV1Async(paymentInfo.CodeMarchand, paymentInfo.ApiToken,
                        commande.ReferenceFlexPay, amount, payCurrency, $"Réservation hôtel {snapshot.ReferenceReservation}",
                        callback, FlexPayUrlHelper.DeriveRedirectUrl(callback, "approve"),
                        FlexPayUrlHelper.DeriveRedirectUrl(callback, "cancel"),
                        FlexPayUrlHelper.DeriveRedirectUrl(callback, "decline"), cancellationToken)
                    : await _flexPay.InitierPaiementMobileMoneyAsync(paymentInfo.CodeMarchand, paymentInfo.ApiToken,
                        commande.ReferenceFlexPay, request.Paiement.Phone!.Trim(), amount, payCurrency, callback, cancellationToken);
                payment.ProviderTxRef = string.IsNullOrWhiteSpace(response.OrderNumber) ? payment.ProviderTxRef : response.OrderNumber.Trim();
                payment.DateModification = DateTime.UtcNow;
                commande.OrderNumberFlexPay = payment.ProviderTxRef;
                await _context.SaveChangesAsync(cancellationToken);
                if (!response.IsSuccess)
                {
                    await FailCommandeAsync(commande, payment, cancellationToken);
                    throw new InvalidOperationException($"FlexPay a refusé l'initiation : {response.Message ?? response.Code ?? "erreur"}");
                }
                return await BuildResponseAsync(commande, false, cancellationToken, response.ResolvePaymentUrl());
            }
            catch
            {
                if (commande != null) await FailCommandeAsync(commande, null, cancellationToken);
                throw;
            }
        }

        public async Task<HotelReservation> FinalizeCommandeSuccessAsync(
            HotelCommandeEnAttente commande, HotelPayment payment, CancellationToken cancellationToken = default)
        {
            var snapshot = JsonSerializer.Deserialize<HotelCommandeSnapshotDto>(commande.PayloadMetierJson, JsonOptions)
                ?? throw new InvalidOperationException("Payload commande hôtel invalide.");
            var reservation = new HotelReservation
            {
                IdSociete = commande.IdSociete, IdHotel = commande.IdHotel, IdSite = commande.IdSite,
                IdUtilisateur = commande.IdUtilisateur, IdClient = commande.IdClient,
                ReferenceReservation = snapshot.ReferenceReservation, CustomerRef = snapshot.Request.CustomerRef,
                CheckInDate = snapshot.Request.CheckInDate.Date, CheckOutDate = snapshot.Request.CheckOutDate.Date,
                NombreNuits = (snapshot.Request.CheckOutDate.Date - snapshot.Request.CheckInDate.Date).Days,
                Status = HotelReservationStatus.HOLD, ExpiresAtUtc = commande.DateExpiration,
                MontantSejour = snapshot.MontantSejour, MontantSousTotal = snapshot.MontantSousTotal,
                CodeDevise = snapshot.CodeDevise, InventoryMode = snapshot.InventoryMode,
                IdempotencyKey = commande.IdempotencyKey,
                DateCreation = DateTime.UtcNow,
                Lines = snapshot.Lines.Select(ToReservationLine).ToList()
            };
            _context.HotelReservations.Add(reservation);
            await _context.SaveChangesAsync(cancellationToken);
            var trackedPayment = await _context.HotelPayments.FirstAsync(p => p.IdHotelPayment == payment.IdHotelPayment, cancellationToken);
            await _confirmation.ConfirmHoldAsync(reservation, trackedPayment, cancellationToken);
            trackedPayment.IdHotelReservation = reservation.IdHotelReservation;
            trackedPayment.IdHotelCommandeEnAttente = null;
            var tracked = await _context.HotelCommandesEnAttente.FirstAsync(c => c.IdHotelCommandeEnAttente == commande.IdHotelCommandeEnAttente, cancellationToken);
            tracked.IdPaiementEnAttente = null;
            await _context.SaveChangesAsync(cancellationToken);
            _context.HotelCommandesEnAttente.Remove(tracked);
            await _context.SaveChangesAsync(cancellationToken);
            return reservation;
        }

        public async Task FailCommandeAsync(HotelCommandeEnAttente commande, HotelPayment? payment, CancellationToken cancellationToken = default)
        {
            var tracked = await _context.HotelCommandesEnAttente.FirstOrDefaultAsync(
                c => c.IdHotelCommandeEnAttente == commande.IdHotelCommandeEnAttente, cancellationToken);
            if (tracked == null) return;
            try
            {
                var snapshot = JsonSerializer.Deserialize<HotelCommandeSnapshotDto>(tracked.PayloadMetierJson, JsonOptions);
                if (snapshot != null)
                {
                    var transient = new HotelReservation
                    {
                        IdHotel = tracked.IdHotel, IdSociete = tracked.IdSociete,
                        CheckInDate = snapshot.Request.CheckInDate.Date,
                        CheckOutDate = snapshot.Request.CheckOutDate.Date,
                        NombreNuits = (snapshot.Request.CheckOutDate.Date - snapshot.Request.CheckInDate.Date).Days,
                        Status = HotelReservationStatus.HOLD,
                        InventoryMode = snapshot.InventoryMode,
                        Lines = snapshot.Lines.Select(ToReservationLine).ToList()
                    };
                    var cancelStrategy = _cancelStrategyFactory.GetStrategy(snapshot.InventoryMode);
                    await cancelStrategy.ReleaseReservationAsync(transient, false, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Release inventaire commande hôtel échoué — Commande={Id}", tracked.IdHotelCommandeEnAttente);
            }
            var trackedPayment = payment == null
                ? await _context.HotelPayments.FirstOrDefaultAsync(p => p.IdHotelCommandeEnAttente == tracked.IdHotelCommandeEnAttente, cancellationToken)
                : await _context.HotelPayments.FirstOrDefaultAsync(p => p.IdHotelPayment == payment.IdHotelPayment, cancellationToken);
            if (trackedPayment != null && trackedPayment.Status is not (HotelPaymentStatus.SUCCEEDED or HotelPaymentStatus.REFUNDED))
            {
                trackedPayment.Status = HotelPaymentStatus.FAILED;
                trackedPayment.IdHotelCommandeEnAttente = null;
                trackedPayment.DateModification = DateTime.UtcNow;
            }
            tracked.IdPaiementEnAttente = null;
            await _context.SaveChangesAsync(cancellationToken);
            _context.HotelCommandesEnAttente.Remove(tracked);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<HotelReservationWithPaiementResponseDto> BuildResponseAsync(
            HotelCommandeEnAttente commande, bool already, CancellationToken cancellationToken, string? paymentUrl = null)
        {
            var payment = commande.IdPaiementEnAttente is int id
                ? await _context.HotelPayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdHotelPayment == id, cancellationToken)
                : null;
            var snapshot = JsonSerializer.Deserialize<HotelCommandeSnapshotDto>(commande.PayloadMetierJson, JsonOptions);
            return new HotelReservationWithPaiementResponseDto
            {
                Reservation = new HotelReservationResponseDto
                {
                    IdHotelReservation = 0, IdSociete = commande.IdSociete, IdHotel = commande.IdHotel,
                    IdSite = commande.IdSite, IdUtilisateur = commande.IdUtilisateur, IdClient = commande.IdClient,
                    ReferenceReservation = snapshot?.ReferenceReservation ?? "", CustomerRef = snapshot?.Request.CustomerRef,
                    CheckInDate = snapshot?.Request.CheckInDate.Date ?? default,
                    CheckOutDate = snapshot?.Request.CheckOutDate.Date ?? default,
                    NombreNuits = snapshot == null ? 0 : (snapshot.Request.CheckOutDate.Date - snapshot.Request.CheckInDate.Date).Days,
                    Status = "EN_ATTENTE_PAIEMENT", ExpiresAtUtc = commande.DateExpiration,
                    MontantSejour = snapshot?.MontantSejour ?? 0, MontantSousTotal = commande.MontantTarif,
                    CodeDevise = commande.CodeDeviseTarif,
                    InventoryMode = (snapshot?.InventoryMode ?? HotelInventoryMode.ClassQuota).ToString(),
                    DateCreation = commande.DateCreation
                },
                Payment = payment == null ? new HotelPaymentResponseDto() : HotelReservationMapper.ToPayment(payment),
                TransactionStatut = "EnAttente",
                Message = already ? "Paiement FlexPay déjà initié pour cette clé d'idempotence." : "Paiement FlexPay initié. Aucune réservation tant que le paiement n'est pas confirmé.",
                OrderNumber = commande.OrderNumberFlexPay ?? payment?.ProviderTxRef,
                PaymentUrl = paymentUrl, ReservationExpiresAtUtc = commande.DateExpiration,
                MontantFlexPay = commande.MontantFlexPay, CodeDevisePaiement = commande.CodeDevisePaiement,
                MontantTarif = commande.MontantTarif, CodeDeviseTarif = commande.CodeDeviseTarif,
                TauxApplique = commande.TauxVersDevisePaiement, FlexPayAccepted = true,
                AlreadyInitiated = already
            };
        }

        private static HotelReservationLine ToReservationLine(HotelCommandeSnapshotLineDto l)
        {
            var lineType = Enum.TryParse<HotelReservationLineType>(l.LineType, true, out var parsed)
                ? parsed
                : (l.IdHotelRoomType is > 0
                    ? HotelReservationLineType.ClassQuota
                    : HotelReservationLineType.GlobalQuota);
            return new HotelReservationLine
            {
                LineType = lineType,
                IdHotelRoomType = l.IdHotelRoomType,
                IdHotelNight = l.IdHotelNight,
                Quantity = l.Quantity,
                PrixSejourUnitaire = l.PrixSejourUnitaire,
                MontantLigne = l.MontantLigne,
                CodeDevise = l.CodeDevise
            };
        }
    }
}
