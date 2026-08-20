using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiquePaymentService : ISiteTouristiquePaymentService
    {
        private const int MaxReferenceAttempts = 10;
        private const string CashProvider = "CASH";

        private readonly CongoTravelDbContext _context;
        private readonly ISiteTouristiqueReservationConfirmationService _confirmationService;
        private readonly ILogger<SiteTouristiquePaymentService> _logger;

        public SiteTouristiquePaymentService(
            CongoTravelDbContext context,
            ISiteTouristiqueReservationConfirmationService confirmationService,
            ILogger<SiteTouristiquePaymentService> logger)
        {
            _context = context;
            _confirmationService = confirmationService;
            _logger = logger;
        }

        public async Task<SiteTouristiqueConfirmPaymentResponseDto> ConfirmPaymentAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            SiteTouristiqueConfirmPaymentRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var idempotencyKey = SiteTouristiqueIdempotencyHelper.NormalizeKey(request.IdempotencyKey);
            if (idempotencyKey != null)
            {
                var existingPayment = await _context.SiteTouristiquePayments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken);

                if (existingPayment != null)
                {
                    if (existingPayment.IdSiteTouristiqueReservation is null or <= 0)
                        throw new InvalidOperationException(
                            "Cette clé d'idempotence est liée à une commande FlexPay en attente.");

                    var reservation = await LoadReservationGraphAsync(
                        existingPayment.IdSiteTouristiqueReservation.Value,
                        idSociete,
                        cancellationToken);

                    if (reservation == null)
                    {
                        throw new KeyNotFoundException(
                            $"Paiement site touristique {existingPayment.IdSiteTouristiquePayment} introuvable pour la société {idSociete}.");
                    }

                    _logger.LogInformation(
                        "Confirm paiement site touristique idempotent — IdPayment={Id}, IdempotencyKey={Key}",
                        existingPayment.IdSiteTouristiquePayment,
                        idempotencyKey);

                    return SiteTouristiqueReservationMapper.ToConfirmPaymentResponse(
                        reservation,
                        existingPayment,
                        alreadyConfirmed: true);
                }
            }

            var reservationSnapshot = await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (reservationSnapshot == null)
            {
                throw new KeyNotFoundException(
                    $"Réservation site touristique {idSiteTouristiqueReservation} introuvable pour la société {idSociete}.");
            }

            if (reservationSnapshot.Status == SiteTouristiqueReservationStatus.CONFIRMED)
            {
                var confirmed = await LoadReservationGraphAsync(idSiteTouristiqueReservation, idSociete, cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Réservation site touristique {idSiteTouristiqueReservation} introuvable pour la société {idSociete}.");

                var payment = confirmed.Payments
                    .OrderByDescending(p => p.DateCreation)
                    .FirstOrDefault(p => p.Status == SiteTouristiquePaymentStatus.SUCCEEDED);

                if (payment == null)
                {
                    throw new InvalidOperationException(
                        "Réservation confirmée sans paiement réussi associé.");
                }

                return SiteTouristiqueReservationMapper.ToConfirmPaymentResponse(confirmed, payment, alreadyConfirmed: true);
            }

            var dbStrategy = _context.Database.CreateExecutionStrategy();
            return await dbStrategy.ExecuteAsync(async () =>
            {
                IDbContextTransaction? transaction = null;
                if (_context.Database.IsRelational())
                    transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var reservation = await _context.SiteTouristiqueReservations
                        .Include(r => r.Lines)
                        .Include(r => r.Payments)
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
                        var succeededPayment = reservation.Payments
                            .OrderByDescending(p => p.DateCreation)
                            .First(p => p.Status == SiteTouristiquePaymentStatus.SUCCEEDED);

                        if (transaction != null)
                            await transaction.CommitAsync(cancellationToken);

                        return SiteTouristiqueReservationMapper.ToConfirmPaymentResponse(
                            reservation,
                            succeededPayment,
                            alreadyConfirmed: true);
                    }

                    var paymentReference = await GenerateUniquePaymentReferenceAsync(idSociete, cancellationToken);
                    var payment = new SiteTouristiquePayment
                    {
                        ReferencePaiement = paymentReference,
                        Provider = CashProvider,
                        ProviderTxRef = string.IsNullOrWhiteSpace(request.ReferenceTransaction)
                            ? null
                            : request.ReferenceTransaction.Trim(),
                        Montant = reservation.MontantSousTotal,
                        CodeDevise = reservation.CodeDevise,
                        MontantTarif = reservation.MontantSousTotal,
                        CodeDeviseTarif = reservation.CodeDevise,
                        TauxVersDevisePaiement = 1m,
                        IdempotencyKey = idempotencyKey,
                        IdSite = reservation.IdSite
                    };

                    await _confirmationService.ConfirmHoldAndEmitTicketsAsync(
                        reservation,
                        payment,
                        idSociete,
                        cancellationToken);

                    await _context.SaveChangesAsync(cancellationToken);

                    if (transaction != null)
                        await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "Paiement site touristique confirmé — IdReservation={Id}, IdPayment={PaymentId}, Tickets={TicketCount}",
                        reservation.IdSiteTouristiqueReservation,
                        payment.IdSiteTouristiquePayment,
                        reservation.Lines.Sum(l => l.Quantite));

                    return SiteTouristiqueReservationMapper.ToConfirmPaymentResponse(
                        reservation,
                        payment,
                        alreadyConfirmed: false);
                }
                catch
                {
                    if (transaction != null)
                        await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
                finally
                {
                    if (transaction != null)
                        await transaction.DisposeAsync();
                }
            });
        }

        private static void ValidateRequest(SiteTouristiqueConfirmPaymentRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.MethodePaiement))
                throw new InvalidOperationException("MethodePaiement est obligatoire.");

            if (!string.Equals(request.MethodePaiement.Trim(), CashProvider, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Phase 2 V1 : seul le paiement CASH est supporté.");
            }
        }

        private async Task<SiteTouristiqueReservation?> LoadReservationGraphAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken)
        {
            return await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .Include(r => r.Payments)
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
