using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    /// <summary>Mode A : restitution siège hold ou vendu à l'annulation.</summary>
    public class EvenementSeatNumberedCancelStrategy : IEvenementInventoryCancelStrategy
    {
        private const string ReleaseHoldSql = @"
UPDATE `EvenementSessionSeats`
SET `SeatStatus` = 'Available',
    `IdEvenementReservationCourante` = NULL,
    `HoldExpireAtUtc` = NULL
WHERE `IdEvenementSessionSeat` = {0}
  AND `SeatStatus` = 'Held'";

        private const string ReleaseSoldSql = @"
UPDATE `EvenementSessionSeats`
SET `SeatStatus` = 'Available',
    `IdEvenementReservationCourante` = NULL
WHERE `IdEvenementSessionSeat` = {0}
  AND `SeatStatus` = 'Sold'";

        private readonly CongoTravelDbContext _context;

        public EvenementSeatNumberedCancelStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public EvenementInventoryMode SupportedMode => EvenementInventoryMode.SeatNumbered;

        public async Task ReleaseReservationAsync(
            EvenementInventoryCancelRequest request,
            CancellationToken cancellationToken = default)
        {
            var session = request.Session;
            if (session.InventoryMode != EvenementInventoryMode.SeatNumbered)
            {
                throw new InvalidOperationException(
                    $"La stratégie SeatNumbered ne s'applique pas au mode {session.InventoryMode}.");
            }

            var seatIds = EvenementSeatNumberedConfirmStrategy.GetSeatLineIds(request.Reservation.Lines);

            foreach (var seatId in seatIds)
            {
                var released = request.FromConfirmedSale
                    ? await TryReleaseSoldAsync(seatId, cancellationToken)
                    : await TryReleaseHoldAsync(seatId, cancellationToken);

                if (!released)
                {
                    var stockType = request.FromConfirmedSale ? "Sold" : "Held";
                    throw new EvenementHoldConflictException(
                        $"Impossible d'annuler : siège {seatId} non en statut {stockType}.");
                }
            }
        }

        private async Task<bool> TryReleaseHoldAsync(
            int idEvenementSessionSeat,
            CancellationToken cancellationToken)
        {
            if (_context.Database.IsRelational()
                && string.Equals(
                    _context.Database.ProviderName,
                    "Pomelo.EntityFrameworkCore.MySql",
                    StringComparison.Ordinal))
            {
                var rows = await _context.Database.ExecuteSqlRawAsync(
                    ReleaseHoldSql,
                    new object[] { idEvenementSessionSeat },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseHoldViaEfAsync(idEvenementSessionSeat, cancellationToken);
        }

        private async Task<bool> TryReleaseSoldAsync(
            int idEvenementSessionSeat,
            CancellationToken cancellationToken)
        {
            if (_context.Database.IsRelational()
                && string.Equals(
                    _context.Database.ProviderName,
                    "Pomelo.EntityFrameworkCore.MySql",
                    StringComparison.Ordinal))
            {
                var rows = await _context.Database.ExecuteSqlRawAsync(
                    ReleaseSoldSql,
                    new object[] { idEvenementSessionSeat },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseSoldViaEfAsync(idEvenementSessionSeat, cancellationToken);
        }

        private async Task<bool> TryReleaseHoldViaEfAsync(
            int idEvenementSessionSeat,
            CancellationToken cancellationToken)
        {
            var seat = await _context.EvenementSessionSeats
                .FirstOrDefaultAsync(s => s.IdEvenementSessionSeat == idEvenementSessionSeat, cancellationToken);

            if (seat == null || seat.SeatStatus != EvenementSessionSeatStatus.Held)
                return false;

            seat.SeatStatus = EvenementSessionSeatStatus.Available;
            seat.IdEvenementReservationCourante = null;
            seat.HoldExpireAtUtc = null;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<bool> TryReleaseSoldViaEfAsync(
            int idEvenementSessionSeat,
            CancellationToken cancellationToken)
        {
            var seat = await _context.EvenementSessionSeats
                .FirstOrDefaultAsync(s => s.IdEvenementSessionSeat == idEvenementSessionSeat, cancellationToken);

            if (seat == null || seat.SeatStatus != EvenementSessionSeatStatus.Sold)
                return false;

            seat.SeatStatus = EvenementSessionSeatStatus.Available;
            seat.IdEvenementReservationCourante = null;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
