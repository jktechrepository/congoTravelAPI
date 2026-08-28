using CongoTravel.Data;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel.Strategies;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelReservationConfirmationService : IHotelReservationConfirmationService
    {
        private readonly IHotelInventoryConfirmStrategyFactory _confirmStrategyFactory;
        public HotelReservationConfirmationService(IHotelInventoryConfirmStrategyFactory confirmStrategyFactory) =>
            _confirmStrategyFactory = confirmStrategyFactory;

        public async Task ConfirmHoldAsync(HotelReservation reservation, HotelPayment payment,
            CancellationToken cancellationToken = default)
        {
            if (reservation.Status != HotelReservationStatus.HOLD)
                throw new InvalidOperationException($"Impossible de confirmer une réservation au statut {reservation.Status}.");
            if (reservation.ExpiresAtUtc < DateTime.UtcNow)
                throw new InvalidOperationException("Le hold hôtel a expiré.");
            if (reservation.Lines.Count == 0)
                throw new InvalidOperationException("La réservation ne contient aucune ligne.");
            var strategy = _confirmStrategyFactory.GetStrategy(reservation.InventoryMode);
            await strategy.ConfirmHoldAsync(reservation, cancellationToken);
            var now = DateTime.UtcNow;
            reservation.Status = HotelReservationStatus.CONFIRMED;
            reservation.ExpiresAtUtc = null;
            reservation.DateModification = now;
            payment.Status = HotelPaymentStatus.SUCCEEDED;
            payment.DateCreation = now;
            payment.DateModification = now;
            reservation.Payments.Add(payment);
        }
    }

    public class HotelPaymentService : IHotelPaymentService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IHotelReservationConfirmationService _confirmation;
        public HotelPaymentService(CongoTravelDbContext context,
            IHotelReservationConfirmationService confirmation)
        {
            _context = context; _confirmation = confirmation;
        }

        public async Task<HotelConfirmPaymentResponseDto> ConfirmPaymentAsync(int idHotelReservation,
            int idSociete, HotelConfirmPaymentRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(request.MethodePaiement?.Trim(), "CASH", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(request.MethodePaiement?.Trim(), "ESPECES", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Phase 3 hôtel : seul le paiement CASH est supporté.");
            var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
            if (key != null)
            {
                var prior = await _context.HotelPayments.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdempotencyKey == key, cancellationToken);
                if (prior != null)
                {
                    if (prior.IdHotelReservation is null or <= 0)
                        throw new InvalidOperationException("Ce paiement idempotent appartient à une commande électronique en attente.");
                    var priorReservation = await LoadAsync(prior.IdHotelReservation.Value, idSociete, true, cancellationToken)
                        ?? throw new KeyNotFoundException("Réservation du paiement idempotent introuvable.");
                    return Build(priorReservation, prior, true);
                }
            }

            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;
            try
            {
                var reservation = await LoadAsync(idHotelReservation, idSociete, false, cancellationToken)
                    ?? throw new KeyNotFoundException($"Réservation hôtel {idHotelReservation} introuvable.");
                if (reservation.Status == HotelReservationStatus.CONFIRMED)
                {
                    var prior = reservation.Payments.FirstOrDefault(p => p.Status == HotelPaymentStatus.SUCCEEDED)
                        ?? throw new InvalidOperationException("Réservation confirmée sans paiement réussi.");
                    if (transaction != null) await transaction.CommitAsync(cancellationToken);
                    return Build(reservation, prior, true);
                }
                var now = DateTime.UtcNow;
                var paymentReference = $"PAY-HTL-{idSociete}-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
                var payment = new HotelPayment
                {
                    IdHotelReservation = reservation.IdHotelReservation,
                    IdSite = reservation.IdSite,
                    ReferencePaiement = paymentReference.Length <= 64
                        ? paymentReference : paymentReference.Substring(0, 64),
                    Provider = "CASH",
                    ProviderTxRef = string.IsNullOrWhiteSpace(request.ReferenceTransaction)
                        ? null : request.ReferenceTransaction.Trim(),
                    Montant = reservation.MontantSousTotal,
                    CodeDevise = reservation.CodeDevise,
                    MontantTarif = reservation.MontantSousTotal,
                    CodeDeviseTarif = reservation.CodeDevise,
                    TauxVersDevisePaiement = 1m,
                    IdempotencyKey = key
                };
                await _confirmation.ConfirmHoldAsync(reservation, payment, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null) await transaction.CommitAsync(cancellationToken);
                return Build(reservation, payment, false);
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private Task<HotelReservation?> LoadAsync(int id, int idSociete, bool noTracking, CancellationToken ct)
        {
            IQueryable<HotelReservation> query = _context.HotelReservations;
            if (noTracking) query = query.AsNoTracking();
            return query.Include(r => r.Lines).Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.IdHotelReservation == id && r.IdSociete == idSociete, ct);
        }

        private static HotelConfirmPaymentResponseDto Build(
            HotelReservation reservation, HotelPayment payment, bool already) => new()
        {
            Reservation = HotelReservationMapper.ToResponse(reservation),
            Payment = HotelReservationMapper.ToPayment(payment),
            AlreadyConfirmed = already
        };
    }
}
