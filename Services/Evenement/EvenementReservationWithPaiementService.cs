using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Evenement;

namespace CongoTravel.Services.Evenement
{
    public class EvenementReservationWithPaiementService : IEvenementReservationWithPaiementService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IEvenementHoldService _holdService;
        private readonly IEvenementPaymentService _paymentService;
        private readonly IEvenementFlexPayInitiationService _flexPayInitiationService;
        private readonly IEvenementReservationService _reservationService;
        private readonly ILogger<EvenementReservationWithPaiementService> _logger;

        public EvenementReservationWithPaiementService(
            CongoTravelDbContext context,
            IEvenementHoldService holdService,
            IEvenementPaymentService paymentService,
            IEvenementFlexPayInitiationService flexPayInitiationService,
            IEvenementReservationService reservationService,
            ILogger<EvenementReservationWithPaiementService> logger)
        {
            _context = context;
            _holdService = holdService;
            _paymentService = paymentService;
            _flexPayInitiationService = flexPayInitiationService;
            _reservationService = reservationService;
            _logger = logger;
        }

        public async Task<EvenementReservationWithPaiementResponseDto> CreateCashAsync(
            int idSociete,
            EvenementReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateSharedRequest(request);
            if (MethodePaiementHelper.IsElectronic(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "Les paiements Mobile Money et carte bancaire doivent utiliser " +
                    "POST /api/events/reservations/with-paiement-electronique. " +
                    "Cet endpoint accepte uniquement CASH (ou espèces).");
            }

            if (!MethodePaiementHelper.IsCash(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "Cet endpoint accepte uniquement CASH (ou espèces).");
            }

            var effectiveIdSite = await ResolveEffectiveIdSiteAsync(
                idSociete, request, requireSite: false, cancellationToken);

            var hold = await _holdService.CreateHoldAsync(
                request.IdEvenementSession,
                idSociete,
                ToHoldRequest(request, effectiveIdSite),
                cancellationToken);

            try
            {
                var confirmed = await _paymentService.ConfirmPaymentAsync(
                    hold.IdEvenementReservation,
                    idSociete,
                    new EvenementConfirmPaymentRequestDto
                    {
                        MethodePaiement = MethodePaiementHelper.Cash,
                        IdempotencyKey = ResolvePaymentIdempotencyKey(request),
                        ReferenceTransaction = request.Paiement.ReferenceTransaction
                    },
                    cancellationToken);

                return new EvenementReservationWithPaiementResponseDto
                {
                    Reservation = confirmed.Reservation,
                    Payment = confirmed.Payment,
                    Tickets = confirmed.Reservation.Tickets ?? new List<EvenementTicketResponseDto>(),
                    TransactionStatut = "Succes",
                    Message = confirmed.AlreadyConfirmed
                        ? "Réservation déjà confirmée (idempotent)."
                        : "Réservation confirmée et tickets émis.",
                    AlreadyConfirmed = confirmed.AlreadyConfirmed,
                    ReservationExpiresAtUtc = null
                };
            }
            catch (Exception ex)
            {
                await TryRollbackHoldAsync(hold.IdEvenementReservation, idSociete, ex, cancellationToken);
                throw;
            }
        }

        public async Task<EvenementReservationWithPaiementResponseDto> InitiateElectronicAsync(
            int idSociete,
            EvenementReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateSharedRequest(request);
            if (!MethodePaiementHelper.IsElectronic(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "L'endpoint électronique accepte uniquement MOBILE_MONEY ou CARTE_BANCAIRE. " +
                    "Utilisez POST /api/events/reservations/with-paiement pour CASH.");
            }

            var effectiveIdSite = await ResolveEffectiveIdSiteAsync(
                idSociete, request, requireSite: true, cancellationToken);

            var hold = await _holdService.CreateHoldAsync(
                request.IdEvenementSession,
                idSociete,
                ToHoldRequest(request, effectiveIdSite),
                cancellationToken);

            try
            {
                var initiated = await _flexPayInitiationService.InitiateAsync(
                    hold.IdEvenementReservation,
                    idSociete,
                    new EvenementInitiateFlexPayRequestDto
                    {
                        MethodePaiement = request.Paiement.MethodePaiement,
                        Phone = request.Paiement.Phone,
                        IdSite = effectiveIdSite!.Value,
                        CodeDevisePaiement = request.Paiement.CodeDevisePaiement,
                        IdempotencyKey = ResolvePaymentIdempotencyKey(request)
                    },
                    cancellationToken);

                var reservation = await _reservationService.GetByIdAsync(
                    hold.IdEvenementReservation,
                    idSociete,
                    cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Réservation événement {hold.IdEvenementReservation} introuvable après initiation FlexPay.");

                return new EvenementReservationWithPaiementResponseDto
                {
                    Reservation = reservation,
                    Payment = initiated.Payment,
                    Tickets = new List<EvenementTicketResponseDto>(),
                    TransactionStatut = "EnAttente",
                    Message = string.IsNullOrWhiteSpace(initiated.Message)
                        ? "Paiement FlexPay initié. Hold conservé jusqu'à confirmation ou expiration."
                        : initiated.Message,
                    OrderNumber = initiated.OrderNumber,
                    PaymentUrl = initiated.PaymentUrl,
                    ReservationExpiresAtUtc = initiated.ReservationExpiresAtUtc,
                    MontantFlexPay = initiated.MontantFlexPay,
                    CodeDevisePaiement = initiated.CodeDevisePaiement,
                    MontantTarif = initiated.MontantTarif,
                    CodeDeviseTarif = initiated.CodeDeviseTarif,
                    TauxApplique = initiated.TauxApplique,
                    FlexPayAccepted = initiated.FlexPayAccepted,
                    AlreadyInitiated = initiated.AlreadyInitiated
                };
            }
            catch (Exception ex)
            {
                await TryRollbackHoldAsync(hold.IdEvenementReservation, idSociete, ex, cancellationToken);
                throw;
            }
        }

        private async Task<int?> ResolveEffectiveIdSiteAsync(
            int idSociete,
            EvenementReservationWithPaiementRequestDto request,
            bool requireSite,
            CancellationToken cancellationToken)
        {
            int? fromPaiement = request.Paiement.IdSite is > 0
                ? request.Paiement.IdSite
                : null;

            var sessionRow = await _context.EvenementSessions
                .AsNoTracking()
                .Where(s => s.IdEvenementSession == request.IdEvenementSession
                            && s.IdSociete == idSociete)
                .Select(s => new { s.IdSite })
                .FirstOrDefaultAsync(cancellationToken);

            if (sessionRow == null)
            {
                throw new KeyNotFoundException(
                    $"Session événement {request.IdEvenementSession} introuvable pour la société {idSociete}.");
            }

            var effective = fromPaiement ?? sessionRow.IdSite;

            if (requireSite && (!effective.HasValue || effective.Value <= 0))
            {
                throw new InvalidOperationException(
                    "IdSite est obligatoire pour le paiement électronique événement " +
                    "(fournir paiement.idSite ou définir idSite sur la session).");
            }

            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                _context, effective, idSociete, cancellationToken);

            return effective;
        }

        private static void ValidateSharedRequest(EvenementReservationWithPaiementRequestDto request)
        {
            if (request.IdEvenementSession <= 0)
                throw new InvalidOperationException("IdEvenementSession est obligatoire.");

            if (request.Items == null || request.Items.Count == 0)
                throw new InvalidOperationException("Au moins un item de hold est requis.");

            if (request.Paiement == null || string.IsNullOrWhiteSpace(request.Paiement.MethodePaiement))
                throw new InvalidOperationException("Paiement.MethodePaiement est obligatoire.");
        }

        private static EvenementHoldRequestDto ToHoldRequest(
            EvenementReservationWithPaiementRequestDto request,
            int? effectiveIdSite) =>
            new()
            {
                CustomerRef = request.CustomerRef,
                IdempotencyKey = request.IdempotencyKey,
                IdSite = effectiveIdSite,
                Items = request.Items
            };

        private static string? ResolvePaymentIdempotencyKey(EvenementReservationWithPaiementRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Paiement.IdempotencyKey))
                return request.Paiement.IdempotencyKey.Trim();

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return request.IdempotencyKey.Trim() + ":pay";

            return null;
        }

        private async Task TryRollbackHoldAsync(
            int idEvenementReservation,
            int idSociete,
            Exception cause,
            CancellationToken cancellationToken)
        {
            try
            {
                await _reservationService.CancelAsync(idEvenementReservation, idSociete, cancellationToken);
                _logger.LogWarning(
                    cause,
                    "Rollback hold événement après échec 2e étape — IdReservation={Id}",
                    idEvenementReservation);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(
                    rollbackEx,
                    "Échec rollback hold événement IdReservation={Id} (cause initiale loguée séparément)",
                    idEvenementReservation);
            }
        }
    }
}
