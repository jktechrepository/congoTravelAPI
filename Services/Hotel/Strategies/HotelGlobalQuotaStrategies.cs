using CongoTravel.Data;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel.Strategies
{
    /// <summary>Mode GlobalQuota : hold multi-nuit sur <c>HotelNights</c> (pool hôtel × nuit).</summary>
    public class HotelGlobalQuotaHoldStrategy : IHotelInventoryHoldStrategy
    {
        private const string HoldSql = @"
UPDATE `HotelNights`
SET `QuantiteHold` = `QuantiteHold` + {0}
WHERE `IdHotelNight` = {1}
  AND `Status` = 'Published'
  AND (`QuantiteHold` + `QuantiteVendue` + {0}) <= `CapaciteTotale`";

        private readonly CongoTravelDbContext _context;
        public HotelGlobalQuotaHoldStrategy(CongoTravelDbContext context) => _context = context;

        public async Task<HotelHoldStrategyResult> ReserveHoldAsync(
            int idHotel, int idSociete, DateTime checkIn, DateTime checkOut,
            IReadOnlyList<HotelHoldItemRequestDto> items, CancellationToken cancellationToken = default)
        {
            var quantity = ValidateAndSumItems(items);
            var nights = Enumerable.Range(0, (checkOut - checkIn).Days)
                .Select(i => checkIn.AddDays(i).Date).ToList();

            var hotelNights = await _context.HotelNights
                .Where(n => n.IdHotel == idHotel && n.IdSociete == idSociete
                    && n.NightDate >= checkIn && n.NightDate < checkOut)
                .OrderBy(n => n.NightDate)
                .ToListAsync(cancellationToken);

            foreach (var night in nights)
            {
                var row = hotelNights.SingleOrDefault(n => n.NightDate.Date == night);
                if (row == null || row.Status != HotelStatus.Published)
                    throw new InvalidOperationException(
                        $"Nuit Published manquante pour l'hôtel {idHotel}, nuit {night:yyyy-MM-dd}.");
                if (row.QuantiteHold + row.QuantiteVendue + quantity > row.CapaciteTotale)
                    throw new HotelHoldConflictException(
                        $"Capacité insuffisante pour la nuit {night:yyyy-MM-dd}.");
            }

            var currencies = hotelNights.Select(n => n.CodeDevise.ToUpperInvariant()).Distinct().ToList();
            if (currencies.Count != 1)
                throw new InvalidOperationException("Toutes les nuits du séjour doivent utiliser la même devise.");

            foreach (var row in hotelNights)
            {
                if (IsMySql())
                {
                    var rows = await _context.Database.ExecuteSqlRawAsync(
                        HoldSql, new object[] { quantity, row.IdHotelNight }, cancellationToken);
                    if (rows == 0)
                        throw new HotelHoldConflictException(
                            $"Capacité devenue insuffisante pour la nuit {row.NightDate:yyyy-MM-dd}.");
                }
                else
                {
                    row.QuantiteHold += quantity;
                }
            }
            if (!IsMySql())
                await _context.SaveChangesAsync(cancellationToken);

            var unitStay = hotelNights.Sum(n => n.PrixNuit);
            var line = new HotelHoldLineResult(
                null, quantity, unitStay,
                decimal.Round(unitStay * quantity, 2), currencies[0],
                HotelReservationLineType.GlobalQuota);
            return new HotelHoldStrategyResult(new[] { line }, line.MontantLigne, currencies[0]);
        }

        public static int ValidateAndSumItems(IReadOnlyList<HotelHoldItemRequestDto> items)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Au moins un item est requis pour un hold GlobalQuota.");

            var total = 0;
            foreach (var item in items)
            {
                if (item.RoomTypeId is > 0)
                    throw new InvalidOperationException(
                        "Mode GlobalQuota : les items ne doivent pas contenir roomTypeId.");
                if (item.Quantity <= 0)
                    throw new InvalidOperationException("La quantité doit être strictement positive.");
                total += item.Quantity;
            }
            return total;
        }

        private bool IsMySql() => _context.Database.IsRelational()
            && _context.Database.ProviderName == "Pomelo.EntityFrameworkCore.MySql";
    }

    public class HotelGlobalQuotaConfirmStrategy : IHotelInventoryConfirmStrategy
    {
        private const string Sql = @"
UPDATE `HotelNights`
SET `QuantiteHold` = `QuantiteHold` - {0}, `QuantiteVendue` = `QuantiteVendue` + {0}
WHERE `IdHotel` = {1}
  AND `NightDate` >= {2} AND `NightDate` < {3} AND `QuantiteHold` >= {0}";

        private readonly CongoTravelDbContext _context;
        public HotelGlobalQuotaConfirmStrategy(CongoTravelDbContext context) => _context = context;

        public async Task ConfirmHoldAsync(HotelReservation reservation, CancellationToken cancellationToken = default)
        {
            var quantity = SumGlobalQuotaQuantity(reservation.Lines);
            if (IsMySql())
            {
                var rows = await _context.Database.ExecuteSqlRawAsync(Sql,
                    new object[] { quantity, reservation.IdHotel,
                        reservation.CheckInDate, reservation.CheckOutDate }, cancellationToken);
                if (rows != reservation.NombreNuits)
                    throw new HotelHoldConflictException("Stock hold insuffisant pour confirmer tout le séjour.");
            }
            else
            {
                var rows = await LoadAsync(reservation, cancellationToken);
                if (rows.Count != reservation.NombreNuits || rows.Any(n => n.QuantiteHold < quantity))
                    throw new HotelHoldConflictException("Stock hold insuffisant pour confirmer tout le séjour.");
                foreach (var row in rows) { row.QuantiteHold -= quantity; row.QuantiteVendue += quantity; }
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public static int SumGlobalQuotaQuantity(IEnumerable<HotelReservationLine> lines)
        {
            var total = 0;
            foreach (var line in lines)
            {
                if (line.LineType != HotelReservationLineType.GlobalQuota)
                    throw new InvalidOperationException(
                        "Mode GlobalQuota : toutes les lignes doivent être de type GlobalQuota.");
                if (line.Quantity <= 0)
                    throw new InvalidOperationException("Quantité de ligne invalide.");
                total += line.Quantity;
            }
            if (total <= 0)
                throw new InvalidOperationException("Aucune ligne GlobalQuota valide pour confirmer cette réservation.");
            return total;
        }

        private Task<List<HotelNight>> LoadAsync(HotelReservation r, CancellationToken ct) =>
            _context.HotelNights.Where(n => n.IdHotel == r.IdHotel
                && n.NightDate >= r.CheckInDate && n.NightDate < r.CheckOutDate).ToListAsync(ct);

        private bool IsMySql() => _context.Database.IsRelational()
            && _context.Database.ProviderName == "Pomelo.EntityFrameworkCore.MySql";
    }

    public class HotelGlobalQuotaCancelStrategy : IHotelInventoryCancelStrategy
    {
        private readonly CongoTravelDbContext _context;
        public HotelGlobalQuotaCancelStrategy(CongoTravelDbContext context) => _context = context;

        public async Task ReleaseReservationAsync(
            HotelReservation reservation, bool fromConfirmedSale,
            CancellationToken cancellationToken = default)
        {
            var quantity = HotelGlobalQuotaConfirmStrategy.SumGlobalQuotaQuantity(reservation.Lines);
            var rows = await _context.HotelNights.Where(n =>
                    n.IdHotel == reservation.IdHotel
                    && n.NightDate >= reservation.CheckInDate && n.NightDate < reservation.CheckOutDate)
                .ToListAsync(cancellationToken);
            if (rows.Count != reservation.NombreNuits ||
                rows.Any(n => (fromConfirmedSale ? n.QuantiteVendue : n.QuantiteHold) < quantity))
                throw new HotelHoldConflictException("Stock insuffisant pour restituer tout le séjour.");
            foreach (var row in rows)
            {
                if (fromConfirmedSale) row.QuantiteVendue -= quantity;
                else row.QuantiteHold -= quantity;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
