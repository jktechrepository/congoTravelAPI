using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    /// <summary>Mode A (<c>SeatNumbered</c>) : verrou optimiste siège <c>Available</c> → <c>Held</c>.</summary>
    public class EvenementSeatNumberedHoldStrategy : IEvenementInventoryHoldStrategy
    {
        private const string ReserveSeatSql = @"
UPDATE `EvenementSessionSeats`
SET `SeatStatus` = 'Held',
    `HoldExpireAtUtc` = {0}
WHERE `IdEvenementSessionSeat` = {1}
  AND `SeatStatus` = 'Available'";

        private readonly CongoTravelDbContext _context;

        public EvenementSeatNumberedHoldStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public EvenementInventoryMode SupportedMode => EvenementInventoryMode.SeatNumbered;

        public async Task<EvenementHoldStrategyResult> ReserveHoldAsync(
            EvenementInventoryHoldRequest request,
            CancellationToken cancellationToken = default)
        {
            var session = request.Session;
            if (session.InventoryMode != EvenementInventoryMode.SeatNumbered)
            {
                throw new InvalidOperationException(
                    $"La stratégie SeatNumbered ne s'applique pas au mode {session.InventoryMode}.");
            }

            if (session.Status != EvenementSessionStatus.Published)
            {
                throw new InvalidOperationException(
                    "La session doit être publiée pour créer un hold.");
            }

            var seatIds = ValidateAndCollectSeatIds(request.Items);
            var seats = await LoadSessionSeatsAsync(session, seatIds, cancellationToken);

            var lines = new List<EvenementHoldLineResult>();
            decimal montantSousTotal = 0;
            string? codeDevise = null;

            foreach (var seatId in seatIds)
            {
                var seat = seats.First(s => s.IdEvenementSessionSeat == seatId);
                if (seat.IdEvenementSession != session.IdEvenementSession)
                {
                    throw new InvalidOperationException(
                        $"Siège {seatId} n'appartient pas à la session {session.IdEvenementSession}.");
                }

                var reserved = await TryReserveSeatAsync(
                    seat.IdEvenementSessionSeat,
                    request.HoldExpiresAtUtc,
                    cancellationToken);

                if (!reserved)
                {
                    throw new EvenementHoldConflictException(
                        $"Siège {seat.SeatCode} indisponible (session {session.IdEvenementSession}).");
                }

                codeDevise ??= seat.CodeDevise;
                if (!string.Equals(codeDevise, seat.CodeDevise, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Mode SeatNumbered V1 : tous les sièges d'un hold doivent partager la même devise.");
                }

                lines.Add(new EvenementHoldLineResult
                {
                    LineType = EvenementReservationLineType.Seat,
                    Quantite = 1,
                    PrixUnitaire = seat.PrixUnitaire,
                    CodeDevise = seat.CodeDevise,
                    IdEvenementSessionSeat = seat.IdEvenementSessionSeat
                });

                montantSousTotal += seat.PrixUnitaire;
            }

            return new EvenementHoldStrategyResult
            {
                Lines = lines,
                MontantSousTotal = montantSousTotal
            };
        }

        public static List<int> ValidateAndCollectSeatIds(IReadOnlyList<EvenementHoldItemRequestDto> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new InvalidOperationException(
                    "Au moins un item est requis pour un hold SeatNumbered.");
            }

            var seatIds = new List<int>();

            foreach (var item in items)
            {
                if (item.ClassId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Mode SeatNumbered : les items ne doivent pas contenir classId.");
                }

                if (!item.SeatId.HasValue || item.SeatId.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "Mode SeatNumbered : seatId est obligatoire sur chaque item.");
                }

                if (item.Quantity != 1)
                {
                    throw new InvalidOperationException(
                        "Mode SeatNumbered : la quantité doit être 1 par siège.");
                }

                if (seatIds.Contains(item.SeatId.Value))
                {
                    throw new InvalidOperationException(
                        $"Hold SeatNumbered : siège {item.SeatId.Value} en doublon.");
                }

                seatIds.Add(item.SeatId.Value);
            }

            return seatIds;
        }

        private async Task<List<EvenementSessionSeat>> LoadSessionSeatsAsync(
            EvenementSession session,
            IReadOnlyList<int> seatIds,
            CancellationToken cancellationToken)
        {
            var seats = await _context.EvenementSessionSeats
                .AsNoTracking()
                .Where(s => s.IdEvenementSession == session.IdEvenementSession
                            && seatIds.Contains(s.IdEvenementSessionSeat))
                .ToListAsync(cancellationToken);

            if (seats.Count != seatIds.Count)
            {
                throw new InvalidOperationException(
                    "Un ou plusieurs sièges demandés sont introuvables pour cette session.");
            }

            return seats;
        }

        private async Task<bool> TryReserveSeatAsync(
            int idEvenementSessionSeat,
            DateTime holdExpiresAtUtc,
            CancellationToken cancellationToken)
        {
            if (_context.Database.IsRelational()
                && string.Equals(
                    _context.Database.ProviderName,
                    "Pomelo.EntityFrameworkCore.MySql",
                    StringComparison.Ordinal))
            {
                var rows = await _context.Database.ExecuteSqlRawAsync(
                    ReserveSeatSql,
                    new object[] { holdExpiresAtUtc, idEvenementSessionSeat },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReserveSeatViaEfAsync(idEvenementSessionSeat, holdExpiresAtUtc, cancellationToken);
        }

        private async Task<bool> TryReserveSeatViaEfAsync(
            int idEvenementSessionSeat,
            DateTime holdExpiresAtUtc,
            CancellationToken cancellationToken)
        {
            var seat = await _context.EvenementSessionSeats
                .FirstOrDefaultAsync(s => s.IdEvenementSessionSeat == idEvenementSessionSeat, cancellationToken);

            if (seat == null || seat.SeatStatus != EvenementSessionSeatStatus.Available)
                return false;

            seat.SeatStatus = EvenementSessionSeatStatus.Held;
            seat.HoldExpireAtUtc = holdExpiresAtUtc;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
