using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Evenement
{
    public class EvenementFlexPayInitiationService : IEvenementFlexPayInitiationService
    {
        private const int MaxReferenceAttempts = 10;

        private readonly CongoTravelDbContext _context;
        private readonly IFlexPayService _flexPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FlexPayOptions _flexPayOptions;
        private readonly IInfoPaiementResolutionService _infoPaiementResolution;
        private readonly IEvenementReservationConfirmationService _confirmationService;
        private readonly IDeviseMontantConverter _deviseMontantConverter;
        private readonly ILogger<EvenementFlexPayInitiationService> _logger;

        public EvenementFlexPayInitiationService(
            CongoTravelDbContext context,
            IFlexPayService flexPayService,
            IHttpContextAccessor httpContextAccessor,
            IOptions<FlexPayOptions> flexPayOptions,
            IInfoPaiementResolutionService infoPaiementResolution,
            IEvenementReservationConfirmationService confirmationService,
            IDeviseMontantConverter deviseMontantConverter,
            ILogger<EvenementFlexPayInitiationService> logger)
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

        public async Task<EvenementInitiateFlexPayResponseDto> InitiateAsync(
            int idEvenementReservation,
            int idSociete,
            EvenementInitiateFlexPayRequestDto request,
            CancellationToken cancellationToken = default)
        {
            MethodePaiementHelper.EnsureElectronicOnly(request.MethodePaiement);
            var methode = MethodePaiementHelper.NormalizeForStorage(request.MethodePaiement);

            if (!_flexPayOptions.IsEventEnabled)
            {
                throw new InvalidOperationException(
                    "Le paiement électronique FlexPay événement n'est pas activé sur cet environnement.");
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

            var idempotencyKey = EvenementIdempotencyHelper.NormalizeKey(request.IdempotencyKey);
            if (idempotencyKey != null)
            {
                var existingPayment = await _context.EvenementPayments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken);

                if (existingPayment != null)
                {
                    if (existingPayment.IdEvenementReservation != idEvenementReservation)
                    {
                        throw new InvalidOperationException(
                            "Cette clé d'idempotence est déjà utilisée pour une autre réservation événement.");
                    }

                    var existingReservation = await LoadReservationAsync(
                        idEvenementReservation, idSociete, cancellationToken);

                    if (existingReservation == null)
                    {
                        throw new KeyNotFoundException(
                            $"Réservation événement {idEvenementReservation} introuvable pour la société {idSociete}.");
                    }

                    _logger.LogInformation(
                        "Initiation FlexPay événement idempotente — IdPayment={Id}, IdempotencyKey={Key}",
                        existingPayment.IdEvenementPayment,
                        idempotencyKey);

                    return EvenementReservationMapper.ToInitiateFlexPayResponse(
                        existingReservation,
                        existingPayment,
                        orderNumber: existingPayment.ProviderTxRef ?? string.Empty,
                        paymentUrl: null,
                        flexPayAccepted: true,
                        message: "Paiement FlexPay déjà initié pour cette clé d'idempotence.",
                        alreadyInitiated: true);
                }
            }

            var reservation = await _context.EvenementReservations
                .Include(r => r.Payments)
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(
                    r => r.IdEvenementReservation == idEvenementReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (reservation == null)
            {
                throw new KeyNotFoundException(
                    $"Réservation événement {idEvenementReservation} introuvable pour la société {idSociete}.");
            }

            if (reservation.Status == EvenementReservationStatus.CONFIRMED)
            {
                throw new InvalidOperationException("La réservation est déjà confirmée.");
            }

            _confirmationService.EnsureHoldConfirmable(reservation);

            var pendingPayment = reservation.Payments
                .FirstOrDefault(p => p.Status == EvenementPaymentStatus.PENDING
                                     && string.Equals(p.Provider, EvenementFlexPayConstants.Provider, StringComparison.OrdinalIgnoreCase));

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

            var codeDevisePaiement = string.IsNullOrWhiteSpace(request.CodeDevisePaiement)
                ? codeDeviseTarif
                : request.CodeDevisePaiement.Trim().ToUpperInvariant();

            if (codeDevisePaiement is not ("CDF" or "USD"))
            {
                throw new InvalidOperationException(
                    "FlexPay événement n'accepte que CDF ou USD comme devise de paiement.");
            }

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
            var flexReference = EvenementFlexPayReferenceHelper.BuildMerchantReference(idEvenementReservation);
            var pendingOrder = EvenementFlexPayReferenceHelper.BuildPendingOrderNumber(idEvenementReservation);
            var utcNow = DateTime.UtcNow;

            var payment = new EvenementPayment
            {
                IdEvenementReservation = reservation.IdEvenementReservation,
                ReferencePaiement = paymentReference,
                Provider = EvenementFlexPayConstants.Provider,
                ProviderTxRef = pendingOrder,
                Status = EvenementPaymentStatus.PENDING,
                Montant = montantFlexPay,
                CodeDevise = codeDevisePaiement,
                MontantTarif = montantTarif,
                CodeDeviseTarif = codeDeviseTarif,
                TauxVersDevisePaiement = taux,
                IdempotencyKey = idempotencyKey,
                DateCreation = utcNow
            };

            _context.EvenementPayments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            var callbackUrl = FlexPayUrlHelper.ResolveEvenementCallbackUrl(
                _httpContextAccessor.HttpContext,
                _flexPayOptions.CallbackBaseUrl,
                _flexPayOptions.EventCallbackRelativePath,
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
                    $"Réservation événement {reservation.ReferenceReservation}",
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
                "FlexPay événement initiation OK — IdReservation={Id}, Order={OrderNumber}, MontantFlexPay={Montant} {Devise} (tarif {MontantTarif} {DeviseTarif}, taux={Taux})",
                idEvenementReservation,
                orderNumber,
                montantFlexPay,
                codeDevisePaiement,
                montantTarif,
                codeDeviseTarif,
                taux);

            return EvenementReservationMapper.ToInitiateFlexPayResponse(
                reservation,
                payment,
                orderNumber,
                flexResponse.ResolvePaymentUrl(),
                flexPayAccepted: true,
                message,
                alreadyInitiated: false);
        }

        private async Task<EvenementReservation?> LoadReservationAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken)
        {
            return await _context.EvenementReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdEvenementReservation == idEvenementReservation && r.IdSociete == idSociete,
                    cancellationToken);
        }

        private async Task<string> GenerateUniquePaymentReferenceAsync(
            int idSociete,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxReferenceAttempts; attempt++)
            {
                var candidate = EvenementReferenceGenerator.GeneratePaymentReferenceCandidate(idSociete);
                var exists = await _context.EvenementPayments
                    .AsNoTracking()
                    .AnyAsync(p => p.ReferencePaiement == candidate, cancellationToken);

                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException(
                "Impossible de générer une référence de paiement événement unique.");
        }
    }
}
