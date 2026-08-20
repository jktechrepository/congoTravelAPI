using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueFlexPayInitiationService : ISiteTouristiqueFlexPayInitiationService
    {
        private const int MaxReferenceAttempts = 10;

        private readonly CongoTravelDbContext _context;
        private readonly IFlexPayService _flexPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FlexPayOptions _flexPayOptions;
        private readonly IInfoPaiementResolutionService _infoPaiementResolution;
        private readonly ISiteTouristiqueReservationConfirmationService _confirmationService;
        private readonly IDeviseMontantConverter _deviseMontantConverter;
        private readonly ILogger<SiteTouristiqueFlexPayInitiationService> _logger;

        public SiteTouristiqueFlexPayInitiationService(
            CongoTravelDbContext context,
            IFlexPayService flexPayService,
            IHttpContextAccessor httpContextAccessor,
            IOptions<FlexPayOptions> flexPayOptions,
            IInfoPaiementResolutionService infoPaiementResolution,
            ISiteTouristiqueReservationConfirmationService confirmationService,
            IDeviseMontantConverter deviseMontantConverter,
            ILogger<SiteTouristiqueFlexPayInitiationService> logger)
        {
            _context = context;
            _flexPayService = flexPayService;
            _httpContextAccessor = httpContextAccessor;
            _flexPayOptions = flexPayOptions.Value;
            _infoPaiementResolution = infoPaiementResolution;
            _confirmationService = confirmationService;
            _deviseMontantConverter = deviseMontantConverter;
            _logger = logger;
        }

        public async Task<SiteTouristiqueInitiateFlexPayResponseDto> InitiateAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            SiteTouristiqueInitiateFlexPayRequestDto request,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "L'initiation FlexPay site touristique legacy est désactivée par le Plan A. " +
                "Utilisez l'endpoint with-paiement-electronique.");
#pragma warning disable CS0162
            MethodePaiementHelper.EnsureElectronicOnly(request.MethodePaiement);
            var methode = MethodePaiementHelper.NormalizeForStorage(request.MethodePaiement);

            if (!_flexPayOptions.IsSiteTouristiqueEnabled)
            {
                throw new InvalidOperationException(
                    "Le paiement électronique FlexPay site touristique n'est pas activé sur cet environnement.");
            }

            if (methode == MethodePaiementHelper.MobileMoney && string.IsNullOrWhiteSpace(request.Phone))
            {
                throw new InvalidOperationException("Le numéro de téléphone est requis pour MOBILE_MONEY.");
            }

            var infoPaiement = await _infoPaiementResolution.ResolveActiveForSiteAsync(
                request.IdSite, idSociete, cancellationToken);

            if (methode == MethodePaiementHelper.MobileMoney && !infoPaiement.ActifMobileMoney)
                throw new InvalidOperationException("Mobile Money désactivé pour ce site.");

            if (methode == MethodePaiementHelper.CarteBancaire && !infoPaiement.ActifCarteBancaire)
                throw new InvalidOperationException("Carte bancaire désactivée pour ce site.");

            var idempotencyKey = SiteTouristiqueIdempotencyHelper.NormalizeKey(request.IdempotencyKey);
            if (idempotencyKey != null)
            {
                var existingPayment = await _context.SiteTouristiquePayments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken);

                if (existingPayment != null)
                {
                    if (existingPayment.IdSiteTouristiqueReservation != idSiteTouristiqueReservation)
                    {
                        throw new InvalidOperationException(
                            "Cette clé d'idempotence est déjà utilisée pour une autre réservation site touristique.");
                    }

                    var existingReservation = await LoadReservationAsync(
                        idSiteTouristiqueReservation, idSociete, cancellationToken);

                    if (existingReservation == null)
                    {
                        throw new KeyNotFoundException(
                            $"Réservation site touristique {idSiteTouristiqueReservation} introuvable pour la société {idSociete}.");
                    }

                    _logger.LogInformation(
                        "Initiation FlexPay site touristique idempotente — IdPayment={Id}, IdempotencyKey={Key}",
                        existingPayment.IdSiteTouristiquePayment,
                        idempotencyKey);

                    return SiteTouristiqueReservationMapper.ToInitiateFlexPayResponse(
                        existingReservation,
                        existingPayment,
                        orderNumber: existingPayment.ProviderTxRef ?? string.Empty,
                        paymentUrl: null,
                        flexPayAccepted: true,
                        message: "Paiement FlexPay déjà initié pour cette clé d'idempotence.",
                        alreadyInitiated: true);
                }
            }

            var reservation = await _context.SiteTouristiqueReservations
                .Include(r => r.Payments)
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(
                    r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (reservation == null)
            {
                throw new KeyNotFoundException(
                    $"Réservation site touristique {idSiteTouristiqueReservation} introuvable pour la société {idSociete}.");
            }

            if (reservation.Status == SiteTouristiqueReservationStatus.CONFIRMED)
            {
                throw new InvalidOperationException("La réservation est déjà confirmée.");
            }

            _confirmationService.EnsureHoldConfirmable(reservation);

            var pendingPayment = reservation.Payments
                .FirstOrDefault(p => p.Status == SiteTouristiquePaymentStatus.PENDING
                                     && string.Equals(p.Provider, SiteTouristiqueFlexPayConstants.Provider, StringComparison.OrdinalIgnoreCase));

            if (pendingPayment != null)
            {
                throw new InvalidOperationException(
                    "Un paiement FlexPay est déjà en cours pour cette réservation. Utilisez verify ou attendez le callback.");
            }

            var codeDeviseTarif = string.IsNullOrWhiteSpace(reservation.CodeDevise)
                ? "CDF"
                : reservation.CodeDevise.Trim().ToUpperInvariant();

            var montantTarif = reservation.MontantSousTotal;
            if (montantTarif <= 0)
                throw new InvalidOperationException("Le montant de la réservation doit être > 0.");

            var codeDevisePaiementRaw = string.IsNullOrWhiteSpace(request.CodeDevisePaiement)
                ? codeDeviseTarif
                : request.CodeDevisePaiement.Trim().ToUpperInvariant();

            var codeDevisePaiement = FlexPayCurrencyPolicy.NormalizePaymentCurrencyOrThrow(
                codeDevisePaiementRaw,
                "FlexPay site touristique");
            FlexPayCurrencyPolicy.EnsureChannelCurrencySupported(
                _flexPayOptions,
                methode,
                codeDevisePaiement,
                "FlexPay site touristique");

            decimal montantFlexPay = montantTarif;
            decimal taux = 1m;
            if (!string.Equals(codeDeviseTarif, codeDevisePaiement, StringComparison.Ordinal))
            {
                var conversion = await _deviseMontantConverter.ConvertAsync(
                    idSociete,
                    montantTarif,
                    codeDeviseTarif,
                    codeDevisePaiement,
                    DateTime.UtcNow,
                    cancellationToken);
                montantFlexPay = conversion.MontantCible;
                taux = conversion.Taux;
            }

            if (codeDevisePaiement == "CDF")
                montantFlexPay = Math.Round(montantFlexPay, 0, MidpointRounding.AwayFromZero);

            var paymentReference = await GenerateUniquePaymentReferenceAsync(idSociete, cancellationToken);
            var flexReference = SiteTouristiqueFlexPayReferenceHelper.BuildMerchantReference(idSiteTouristiqueReservation);
            var pendingOrder = SiteTouristiqueFlexPayReferenceHelper.BuildPendingOrderNumber(idSiteTouristiqueReservation);
            var utcNow = DateTime.UtcNow;

            var payment = new SiteTouristiquePayment
            {
                IdSiteTouristiqueReservation = reservation.IdSiteTouristiqueReservation,
                IdSite = request.IdSite,
                ReferencePaiement = paymentReference,
                Provider = SiteTouristiqueFlexPayConstants.Provider,
                ProviderTxRef = pendingOrder,
                Status = SiteTouristiquePaymentStatus.PENDING,
                Montant = montantFlexPay,
                CodeDevise = codeDevisePaiement,
                MontantTarif = montantTarif,
                CodeDeviseTarif = codeDeviseTarif,
                TauxVersDevisePaiement = taux,
                IdempotencyKey = idempotencyKey,
                DateCreation = utcNow
            };

            if (reservation.IdSite != request.IdSite)
            {
                reservation.IdSite = request.IdSite;
                reservation.DateModification = utcNow;
            }

            _context.SiteTouristiquePayments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            var callbackUrl = FlexPayUrlHelper.ResolveSiteTouristiqueCallbackUrl(
                _httpContextAccessor.HttpContext,
                _flexPayOptions.CallbackBaseUrl,
                _flexPayOptions.SiteTouristiqueCallbackRelativePath,
                _flexPayOptions.ForceProductionCallbackInDev);

            FlexPayPaymentResponseDto flexResponse;
            if (methode == MethodePaiementHelper.CarteBancaire)
            {
                flexResponse = await _flexPayService.InitierPaiementCarteV1Async(
                    infoPaiement.CodeMarchand,
                    infoPaiement.ApiToken,
                    flexReference,
                    montantFlexPay,
                    codeDevisePaiement,
                    $"Réservation site touristique {reservation.ReferenceReservation}",
                    callbackUrl,
                    FlexPayUrlHelper.DeriveRedirectUrl(callbackUrl, "approve"),
                    FlexPayUrlHelper.DeriveRedirectUrl(callbackUrl, "cancel"),
                    FlexPayUrlHelper.DeriveRedirectUrl(callbackUrl, "decline"),
                    cancellationToken);
            }
            else
            {
                flexResponse = await _flexPayService.InitierPaiementMobileMoneyAsync(
                    infoPaiement.CodeMarchand,
                    infoPaiement.ApiToken,
                    flexReference,
                    request.Phone!.Trim(),
                    montantFlexPay,
                    codeDevisePaiement,
                    callbackUrl,
                    cancellationToken);
            }

            var orderNumber = string.IsNullOrWhiteSpace(flexResponse.OrderNumber)
                ? pendingOrder
                : flexResponse.OrderNumber.Trim();

            payment.ProviderTxRef = orderNumber;
            payment.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            if (!flexResponse.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"FlexPay a refusé l'initiation : {flexResponse.Message ?? flexResponse.Code ?? "erreur"}");
            }

            var message = methode == MethodePaiementHelper.CarteBancaire
                ? "Redirigez le client vers paymentUrl pour finaliser le paiement carte."
                : "Validez le paiement sur votre téléphone Mobile Money. La réservation sera confirmée après callback.";

            _logger.LogInformation(
                "FlexPay site touristique initiation OK — IdReservation={Id}, Order={OrderNumber}, MontantFlexPay={Montant} {Devise} (tarif {MontantTarif} {DeviseTarif}, taux={Taux})",
                idSiteTouristiqueReservation,
                orderNumber,
                montantFlexPay,
                codeDevisePaiement,
                montantTarif,
                codeDeviseTarif,
                taux);

            return SiteTouristiqueReservationMapper.ToInitiateFlexPayResponse(
                reservation,
                payment,
                orderNumber,
                flexResponse.ResolvePaymentUrl(),
                flexPayAccepted: true,
                message,
                alreadyInitiated: false);
#pragma warning restore CS0162
        }

        private async Task<SiteTouristiqueReservation?> LoadReservationAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken)
        {
            return await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation && r.IdSociete == idSociete,
                    cancellationToken);
        }

        private async Task<string> GenerateUniquePaymentReferenceAsync(
            int idSociete,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxReferenceAttempts; attempt++)
            {
                var candidate = SiteTouristiqueReferenceGenerator.GeneratePaymentReferenceCandidate(idSociete);
                var exists = await _context.SiteTouristiquePayments
                    .AsNoTracking()
                    .AnyAsync(p => p.ReferencePaiement == candidate, cancellationToken);

                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException(
                "Impossible de générer une référence de paiement site touristique unique.");
        }
    }
}
