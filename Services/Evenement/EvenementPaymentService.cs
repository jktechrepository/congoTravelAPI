using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement
{
    public class EvenementPaymentService : IEvenementPaymentService
    {
        private const int MaxReferenceAttempts = 10;
        private const string CashProvider = "CASH";

        private readonly CongoTravelDbContext _context;
        private readonly IEvenementReservationConfirmationService _confirmationService;
        private readonly ILogger<EvenementPaymentService> _logger;

        public EvenementPaymentService(
            CongoTravelDbContext context,
            IEvenementReservationConfirmationService confirmationService,
            ILogger<EvenementPaymentService> logger)
        {
            _context = context;
            _confirmationService = confirmationService;
            _logger = logger;
        }

        public async Task<EvenementConfirmPaymentResponseDto> ConfirmPaymentAsync(
            int idEvenementReservation,
            int idSociete,
            EvenementConfirmPaymentRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var idempotencyKey = EvenementIdempotencyHelper.NormalizeKey(request.IdempotencyKey);
            if (idempotencyKey != null)
            {
                var existingPayment = await _context.EvenementPayments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken);

                if (existingPayment != null)
                {
                    if (!existingPayment.IdEvenementReservation.HasValue)
                    {
                        throw new InvalidOperationException(
                            "Ce paiement électronique n'est pas encore lié à une réservation. " +
                            "Utilisez la vérification FlexPay (orderNumber) plutôt que la confirmation cash.");
                    }

                    var reservation = await LoadReservationGraphAsync(
                        existingPayment.IdEvenementReservation.Value,
                        idSociete,
                        cancellationToken);

                    if (reservation == null)
                    {
                        throw new KeyNotFoundException(
                            $"Paiement événement {existingPayment.IdEvenementPayment} introuvable pour la société {idSociete}.");
                    }

                    _logger.LogInformation(
                        "Confirm paiement événement idempotent — IdPayment={Id}, IdempotencyKey={Key}",
                        existingPayment.IdEvenementPayment,
                        idempotencyKey);

                    return EvenementReservationMapper.ToConfirmPaymentResponse(
                        reservation,
                        existingPayment,
                        alreadyConfirmed: true);
                }
            }

            var reservationSnapshot = await _context.EvenementReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdEvenementReservation == idEvenementReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (reservationSnapshot == null)
            {
                throw new KeyNotFoundException(
                    $"Réservation événement {idEvenementReservation} introuvable pour la société {idSociete}.");
            }

            if (reservationSnapshot.Status == EvenementReservationStatus.CONFIRMED)
            {
                var confirmed = await LoadReservationGraphAsync(idEvenementReservation, idSociete, cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Réservation événement {idEvenementReservation} introuvable pour la société {idSociete}.");

                var payment = confirmed.Payments
                    .OrderByDescending(p => p.DateCreation)
                    .FirstOrDefault(p => p.Status == EvenementPaymentStatus.SUCCEEDED);

                if (payment == null)
                {
                    throw new InvalidOperationException(
                        "Réservation confirmée sans paiement réussi associé.");
                }

                return EvenementReservationMapper.ToConfirmPaymentResponse(confirmed, payment, alreadyConfirmed: true);
            }

            var dbStrategy = _context.Database.CreateExecutionStrategy();
            return await dbStrategy.ExecuteAsync(async () =>
            {
                IDbContextTransaction? transaction = null;
                if (_context.Database.IsRelational())
                    transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var reservation = await _context.EvenementReservations
                        .Include(r => r.Lines)
                        .Include(r => r.Payments)
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
                        var succeededPayment = reservation.Payments
                            .OrderByDescending(p => p.DateCreation)
                            .First(p => p.Status == EvenementPaymentStatus.SUCCEEDED);

                        if (transaction != null)
                            await transaction.CommitAsync(cancellationToken);

                        return EvenementReservationMapper.ToConfirmPaymentResponse(
                            reservation,
                            succeededPayment,
                            alreadyConfirmed: true);
                    }

                    var paymentReference = await GenerateUniquePaymentReferenceAsync(idSociete, cancellationToken);
                    var payment = new EvenementPayment
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
                        "Paiement événement confirmé — IdReservation={Id}, IdPayment={PaymentId}, Tickets={TicketCount}",
                        reservation.IdEvenementReservation,
                        payment.IdEvenementPayment,
                        reservation.Lines.Sum(l => l.Quantite));

                    return EvenementReservationMapper.ToConfirmPaymentResponse(
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

        private static void ValidateRequest(EvenementConfirmPaymentRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.MethodePaiement))
                throw new InvalidOperationException("MethodePaiement est obligatoire.");

            if (!string.Equals(request.MethodePaiement.Trim(), CashProvider, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Phase 2 V1 : seul le paiement CASH est supporté.");
            }
        }

        private async Task<EvenementReservation?> LoadReservationGraphAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken)
        {
            return await _context.EvenementReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .Include(r => r.Payments)
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
