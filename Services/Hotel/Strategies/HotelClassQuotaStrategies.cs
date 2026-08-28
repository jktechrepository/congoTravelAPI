using CongoTravel.Data;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel.Strategies
{
    public sealed record HotelHoldLineResult(
        int? IdHotelRoomType, int Quantity, decimal PrixSejourUnitaire,
        decimal MontantLigne, string CodeDevise,
        HotelReservationLineType LineType = HotelReservationLineType.ClassQuota,
        int? IdHotelNight = null);

    public sealed record HotelHoldStrategyResult(
        IReadOnlyList<HotelHoldLineResult> Lines, decimal MontantSejour, string CodeDevise);

    public interface IHotelInventoryHoldStrategy
    {
        Task<HotelHoldStrategyResult> ReserveHoldAsync(
            int idHotel, int idSociete, DateTime checkIn, DateTime checkOut,
            IReadOnlyList<HotelHoldItemRequestDto> items, CancellationToken cancellationToken = default);
    }

    public interface IHotelInventoryConfirmStrategy
    {
        Task ConfirmHoldAsync(HotelReservation reservation, CancellationToken cancellationToken = default);
    }

    public interface IHotelInventoryCancelStrategy
    {
        Task ReleaseReservationAsync(
            HotelReservation reservation, bool fromConfirmedSale,
            CancellationToken cancellationToken = default);
    }

    public class HotelClassQuotaHoldStrategy : IHotelInventoryHoldStrategy
    {
        private const string HoldSql = @"
UPDATE `HotelNightAllotments`
SET `QuantiteHold` = `QuantiteHold` + {0}
WHERE `IdHotelNightAllotment` = {1}
  AND `Status` = 'Published'
  AND (`QuantiteHold` + `QuantiteVendue` + {0}) <= `CapaciteTotale`";
        private readonly CongoTravelDbContext _context;
        public HotelClassQuotaHoldStrategy(CongoTravelDbContext context) => _context = context;

        public async Task<HotelHoldStrategyResult> ReserveHoldAsync(
            int idHotel, int idSociete, DateTime checkIn, DateTime checkOut,
            IReadOnlyList<HotelHoldItemRequestDto> items, CancellationToken cancellationToken = default)
        {
            if (items == null || items.Count == 0 || items.Any(i => i.RoomTypeId is null or <= 0 || i.Quantity <= 0))
                throw new InvalidOperationException("Chaque item doit contenir un roomTypeId et une quantité strictement positifs.");
            var aggregated = items.GroupBy(i => i.RoomTypeId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            var nights = Enumerable.Range(0, (checkOut - checkIn).Days)
                .Select(i => checkIn.AddDays(i).Date).ToList();
            var roomTypeIds = aggregated.Keys.ToList();
            var allotments = await _context.HotelNightAllotments
                .Where(a => a.IdHotel == idHotel && a.IdSociete == idSociete
                    && roomTypeIds.Contains(a.IdHotelRoomType)
                    && a.NightDate >= checkIn && a.NightDate < checkOut)
                .OrderBy(a => a.NightDate).ThenBy(a => a.IdHotelRoomType)
                .ToListAsync(cancellationToken);

            foreach (var roomTypeId in roomTypeIds)
            foreach (var night in nights)
            {
                var allotment = allotments.SingleOrDefault(a =>
                    a.IdHotelRoomType == roomTypeId && a.NightDate.Date == night);
                if (allotment == null || allotment.Status != HotelStatus.Published)
                    throw new InvalidOperationException(
                        $"Allotment Published manquant pour le type {roomTypeId}, nuit {night:yyyy-MM-dd}.");
                if (allotment.QuantiteHold + allotment.QuantiteVendue + aggregated[roomTypeId] > allotment.CapaciteTotale)
                    throw new HotelHoldConflictException(
                        $"Capacité insuffisante pour le type {roomTypeId}, nuit {night:yyyy-MM-dd}.");
            }

            var currencies = allotments.Select(a => a.CodeDevise.ToUpperInvariant()).Distinct().ToList();
            if (currencies.Count != 1)
                throw new InvalidOperationException("Tous les allotments du séjour doivent utiliser la même devise.");

            foreach (var allotment in allotments)
            {
                var quantity = aggregated[allotment.IdHotelRoomType];
                if (IsMySql())
                {
                    var rows = await _context.Database.ExecuteSqlRawAsync(
                        HoldSql, new object[] { quantity, allotment.IdHotelNightAllotment }, cancellationToken);
                    if (rows == 0)
                        throw new HotelHoldConflictException(
                            $"Capacité devenue insuffisante pour la nuit {allotment.NightDate:yyyy-MM-dd}.");
                }
                else
                {
                    allotment.QuantiteHold += quantity;
                }
            }
            if (!IsMySql())
                await _context.SaveChangesAsync(cancellationToken);

            var lines = aggregated.Select(item =>
            {
                var unitStay = allotments.Where(a => a.IdHotelRoomType == item.Key).Sum(a => a.PrixNuit);
                return new HotelHoldLineResult(item.Key, item.Value, unitStay,
                    decimal.Round(unitStay * item.Value, 2), currencies[0],
                    HotelReservationLineType.ClassQuota);
            }).ToList();
            return new HotelHoldStrategyResult(lines, lines.Sum(l => l.MontantLigne), currencies[0]);
        }

        private bool IsMySql() => _context.Database.IsRelational()
            && _context.Database.ProviderName == "Pomelo.EntityFrameworkCore.MySql";
    }

    public class HotelClassQuotaConfirmStrategy : IHotelInventoryConfirmStrategy
    {
        private const string Sql = @"
UPDATE `HotelNightAllotments`
SET `QuantiteHold` = `QuantiteHold` - {0}, `QuantiteVendue` = `QuantiteVendue` + {0}
WHERE `IdHotel` = {1} AND `IdHotelRoomType` = {2}
  AND `NightDate` >= {3} AND `NightDate` < {4} AND `QuantiteHold` >= {0}";
        private readonly CongoTravelDbContext _context;
        public HotelClassQuotaConfirmStrategy(CongoTravelDbContext context) => _context = context;

        public async Task ConfirmHoldAsync(HotelReservation reservation, CancellationToken cancellationToken = default)
        {
            foreach (var line in reservation.Lines)
            {
                var roomTypeId = line.IdHotelRoomType
                    ?? throw new InvalidOperationException("IdHotelRoomType requis pour ClassQuota.");
                if (IsMySql())
                {
                    var rows = await _context.Database.ExecuteSqlRawAsync(Sql,
                        new object[] { line.Quantity, reservation.IdHotel, roomTypeId,
                            reservation.CheckInDate, reservation.CheckOutDate }, cancellationToken);
                    if (rows != reservation.NombreNuits)
                        throw new HotelHoldConflictException("Stock hold insuffisant pour confirmer tout le séjour.");
                }
                else
                {
                    var rows = await LoadAsync(reservation, roomTypeId, cancellationToken);
                    if (rows.Count != reservation.NombreNuits || rows.Any(a => a.QuantiteHold < line.Quantity))
                        throw new HotelHoldConflictException("Stock hold insuffisant pour confirmer tout le séjour.");
                    foreach (var row in rows) { row.QuantiteHold -= line.Quantity; row.QuantiteVendue += line.Quantity; }
                }
            }
            if (!IsMySql()) await _context.SaveChangesAsync(cancellationToken);
        }

        private Task<List<HotelNightAllotment>> LoadAsync(HotelReservation r, int roomTypeId, CancellationToken ct) =>
            _context.HotelNightAllotments.Where(a => a.IdHotel == r.IdHotel && a.IdHotelRoomType == roomTypeId
                && a.NightDate >= r.CheckInDate && a.NightDate < r.CheckOutDate).ToListAsync(ct);
        private bool IsMySql() => _context.Database.IsRelational()
            && _context.Database.ProviderName == "Pomelo.EntityFrameworkCore.MySql";
    }

    public class HotelClassQuotaCancelStrategy : IHotelInventoryCancelStrategy
    {
        private readonly CongoTravelDbContext _context;
        public HotelClassQuotaCancelStrategy(CongoTravelDbContext context) => _context = context;

        public async Task ReleaseReservationAsync(
            HotelReservation reservation, bool fromConfirmedSale,
            CancellationToken cancellationToken = default)
        {
            foreach (var line in reservation.Lines)
            {
                var rows = await _context.HotelNightAllotments.Where(a =>
                    a.IdHotel == reservation.IdHotel && a.IdHotelRoomType == line.IdHotelRoomType
                    && a.NightDate >= reservation.CheckInDate && a.NightDate < reservation.CheckOutDate)
                    .ToListAsync(cancellationToken);
                if (rows.Count != reservation.NombreNuits ||
                    rows.Any(a => (fromConfirmedSale ? a.QuantiteVendue : a.QuantiteHold) < line.Quantity))
                    throw new HotelHoldConflictException("Stock insuffisant pour restituer tout le séjour.");
                foreach (var row in rows)
                {
                    if (fromConfirmedSale) row.QuantiteVendue -= line.Quantity;
                    else row.QuantiteHold -= line.Quantity;
                }
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
