using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantPaymentService : IRestaurantPaymentService
    {
        private const int MaxReferenceAttempts = 10;
        private const string CashProvider = "CASH";

        private readonly CongoTravelDbContext _context;
        private readonly IRestaurantReservationConfirmationService _confirmationService;
        private readonly ILogger<RestaurantPaymentService> _logger;

        public RestaurantPaymentService(
            CongoTravelDbContext context,
            IRestaurantReservationConfirmationService confirmationService,
            ILogger<RestaurantPaymentService> logger)
        {
            _context = context;
            _confirmationService = confirmationService;
            _logger = logger;
        }

        public async Task<RestaurantConfirmPaymentResponseDto> ConfirmPaymentAsync(
            int idRestaurantReservation,
            int idSociete,
            RestaurantConfirmPaymentRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var idempotencyKey = RestaurantIdempotencyHelper.NormalizeKey(request.IdempotencyKey);
            if (idempotencyKey != null)
            {
                var existingPayment = await _context.RestaurantPayments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken);

                if (existingPayment != null)
                {
                    var reservation = await LoadReservationGraphAsync(
                        existingPayment.IdRestaurantReservation,
                        idSociete,
                        cancellationToken);

                    if (reservation == null)
                    {
                        throw new KeyNotFoundException(
                            $"Paiement restaurant {existingPayment.IdRestaurantPayment} introuvable pour la société {idSociete}.");
                    }

                    _logger.LogInformation(
                        "Confirm paiement restaurant idempotent — IdPayment={Id}, IdempotencyKey={Key}",
                        existingPayment.IdRestaurantPayment,
                        idempotencyKey);

                    return RestaurantReservationMapper.ToConfirmPaymentResponse(
                        reservation,
                        existingPayment,
                        alreadyConfirmed: true);
                }
            }

            var reservationSnapshot = await _context.RestaurantReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdRestaurantReservation == idRestaurantReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (reservationSnapshot == null)
            {
                throw new KeyNotFoundException(
                    $"Réservation restaurant {idRestaurantReservation} introuvable pour la société {idSociete}.");
            }

            if (reservationSnapshot.Status == RestaurantReservationStatus.CONFIRMED)
            {
                var confirmed = await LoadReservationGraphAsync(idRestaurantReservation, idSociete, cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Réservation restaurant {idRestaurantReservation} introuvable pour la société {idSociete}.");

                var payment = confirmed.Payments
                    .OrderByDescending(p => p.DateCreation)
                    .FirstOrDefault(p => p.Status == RestaurantPaymentStatus.SUCCEEDED);

                if (payment == null)
                {
                    throw new InvalidOperationException(
                        "Réservation confirmée sans paiement réussi associé.");
                }

                return RestaurantReservationMapper.ToConfirmPaymentResponse(confirmed, payment, alreadyConfirmed: true);
            }

            if (reservationSnapshot.MontantSousTotal <= 0)
            {
                throw new InvalidOperationException(
                    "Montant d'acompte invalide (doit être strictement positif) pour confirmer en CASH.");
            }

            var dbStrategy = _context.Database.CreateExecutionStrategy();
            return await dbStrategy.ExecuteAsync(async () =>
            {
                IDbContextTransaction? transaction = null;
                if (_context.Database.IsRelational())
                    transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var reservation = await _context.RestaurantReservations
                        .Include(r => r.Lines)
                            .ThenInclude(l => l.Tickets)
                        .Include(r => r.Payments)
                        .FirstOrDefaultAsync(
                            r => r.IdRestaurantReservation == idRestaurantReservation && r.IdSociete == idSociete,
                            cancellationToken);

                    if (reservation == null)
                    {
                        throw new KeyNotFoundException(
                            $"Réservation restaurant {idRestaurantReservation} introuvable pour la société {idSociete}.");
                    }

                    if (reservation.Status == RestaurantReservationStatus.CONFIRMED)
                    {
                        var succeededPayment = reservation.Payments
                            .OrderByDescending(p => p.DateCreation)
                            .First(p => p.Status == RestaurantPaymentStatus.SUCCEEDED);

                        if (transaction != null)
                            await transaction.CommitAsync(cancellationToken);

                        return RestaurantReservationMapper.ToConfirmPaymentResponse(
                            reservation,
                            succeededPayment,
                            alreadyConfirmed: true);
                    }

                    var paymentReference = await GenerateUniquePaymentReferenceAsync(idSociete, cancellationToken);
                    var payment = new RestaurantPayment
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
                        "Paiement restaurant confirmé — IdReservation={Id}, IdPayment={PaymentId}",
                        reservation.IdRestaurantReservation,
                        payment.IdRestaurantPayment);

                    return RestaurantReservationMapper.ToConfirmPaymentResponse(
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

        private static void ValidateRequest(RestaurantConfirmPaymentRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.MethodePaiement))
                throw new InvalidOperationException("MethodePaiement est obligatoire.");

            if (!string.Equals(request.MethodePaiement.Trim(), CashProvider, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Phase 2 V1 : seul le paiement CASH est supporté.");
            }
        }

        private async Task<RestaurantReservation?> LoadReservationGraphAsync(
            int idRestaurantReservation,
            int idSociete,
            CancellationToken cancellationToken)
        {
            return await _context.RestaurantReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(
                    r => r.IdRestaurantReservation == idRestaurantReservation && r.IdSociete == idSociete,
                    cancellationToken);
        }

        private async Task<string> GenerateUniquePaymentReferenceAsync(
            int idSociete,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxReferenceAttempts; attempt++)
            {
                var candidate = RestaurantReferenceGenerator.GeneratePaymentReferenceCandidate(idSociete);
                var exists = await _context.RestaurantPayments
                    .AsNoTracking()
                    .AnyAsync(p => p.ReferencePaiement == candidate, cancellationToken);

                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException(
                "Impossible de générer une référence de paiement restaurant unique.");
        }
    }
}
