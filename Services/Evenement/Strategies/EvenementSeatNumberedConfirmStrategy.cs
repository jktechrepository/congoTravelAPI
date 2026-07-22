using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    /// <summary>Mode A : transfert siège <c>Held</c> → <c>Sold</c> à la confirmation.</summary>
    public class EvenementSeatNumberedConfirmStrategy : IEvenementInventoryConfirmStrategy
    {
        private const string ConfirmSeatSql = @"
UPDATE `EvenementSessionSeats`
SET `SeatStatus` = 'Sold',
    `HoldExpireAtUtc` = NULL
WHERE `IdEvenementSessionSeat` = {0}
  AND `SeatStatus` = 'Held'";

        private readonly CongoTravelDbContext _context;

        public EvenementSeatNumberedConfirmStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public EvenementInventoryMode SupportedMode => EvenementInventoryMode.SeatNumbered;

        public async Task ConfirmHoldAsync(
            EvenementInventoryConfirmRequest request,
            CancellationToken cancellationToken = default)
        {
            var session = request.Session;
            if (session.InventoryMode != EvenementInventoryMode.SeatNumbered)
            {
                throw new InvalidOperationException(
                    $"La stratégie SeatNumbered ne s'applique pas au mode {session.InventoryMode}.");
            }

            var seatIds = GetSeatLineIds(request.Reservation.Lines);

            foreach (var seatId in seatIds)
            {
                var transferred = await TryConfirmSeatAsync(seatId, cancellationToken);
                if (!transferred)
                {
                    throw new EvenementHoldConflictException(
                        $"Impossible de confirmer : siège {seatId} non en statut Held.");
                }
            }
        }

        public static IReadOnlyList<int> GetSeatLineIds(IEnumerable<EvenementReservationLine> lines)
        {
            var seatIds = new List<int>();

            foreach (var line in lines)
            {
                if (line.LineType != EvenementReservationLineType.Seat)
                {
                    throw new InvalidOperationException(
                        "Mode SeatNumbered : toutes les lignes doivent être de type Seat.");
                }

                if (!line.IdEvenementSessionSeat.HasValue || line.IdEvenementSessionSeat.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "Ligne Seat sans IdEvenementSessionSeat.");
                }

                if (line.Quantite != 1)
                    throw new InvalidOperationException("Quantité de ligne Seat invalide.");

                seatIds.Add(line.IdEvenementSessionSeat.Value);
            }

            if (seatIds.Count == 0)
            {
                throw new InvalidOperationException(
                    "Aucune ligne Seat valide pour confirmer cette réservation.");
            }

            return seatIds;
        }

        private async Task<bool> TryConfirmSeatAsync(
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
                    ConfirmSeatSql,
                    new object[] { idEvenementSessionSeat },
                    cancellationToken);
                return rows > 0;
            }

            return await TryConfirmSeatViaEfAsync(idEvenementSessionSeat, cancellationToken);
        }

        private async Task<bool> TryConfirmSeatViaEfAsync(
            int idEvenementSessionSeat,
            CancellationToken cancellationToken)
        {
            var seat = await _context.EvenementSessionSeats
                .FirstOrDefaultAsync(s => s.IdEvenementSessionSeat == idEvenementSessionSeat, cancellationToken);

            if (seat == null || seat.SeatStatus != EvenementSessionSeatStatus.Held)
                return false;

            seat.SeatStatus = EvenementSessionSeatStatus.Sold;
            seat.HoldExpireAtUtc = null;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
