using CongoTravel.Data;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelAvailabilityService : IHotelAvailabilityService
    {
        private readonly CongoTravelDbContext _context;

        public HotelAvailabilityService(CongoTravelDbContext context)
        {
            _context = context;
        }

        public async Task<HotelAvailabilityResponseDto> GetAvailabilityAsync(
            int idHotel,
            DateTime from,
            DateTime to,
            int? idHotelRoomType = null,
            int? idSociete = null,
            bool publishedOnly = true,
            HotelInventoryMode? inventoryMode = null,
            CancellationToken cancellationToken = default)
        {
            var fromDate = from.Date;
            var toDate = to.Date;
            if (toDate <= fromDate)
                throw new InvalidOperationException("To doit être strictement postérieur à From (intervalle [from, to)).");
            if (idHotel <= 0)
                throw new InvalidOperationException("IdHotel est obligatoire.");

            var mode = inventoryMode ?? await DetectModeAsync(
                idHotel, fromDate, toDate, idSociete, publishedOnly, cancellationToken);

            return mode == HotelInventoryMode.GlobalQuota
                ? await GetGlobalAsync(idHotel, fromDate, toDate, idSociete, publishedOnly, cancellationToken)
                : await GetClassAsync(idHotel, fromDate, toDate, idHotelRoomType, idSociete, publishedOnly, cancellationToken);
        }

        private async Task<HotelInventoryMode> DetectModeAsync(
            int idHotel, DateTime fromDate, DateTime toDate, int? idSociete, bool publishedOnly,
            CancellationToken cancellationToken)
        {
            var nightsQuery = _context.HotelNights.AsNoTracking()
                .Where(n => n.IdHotel == idHotel && n.NightDate >= fromDate && n.NightDate < toDate);
            if (idSociete is > 0)
                nightsQuery = nightsQuery.Where(n => n.IdSociete == idSociete);
            if (publishedOnly)
                nightsQuery = nightsQuery.Where(n => n.Status == HotelStatus.Published);

            if (await nightsQuery.AnyAsync(cancellationToken))
                return HotelInventoryMode.GlobalQuota;
            return HotelInventoryMode.ClassQuota;
        }

        private async Task<HotelAvailabilityResponseDto> GetClassAsync(
            int idHotel, DateTime fromDate, DateTime toDate, int? idHotelRoomType,
            int? idSociete, bool publishedOnly, CancellationToken cancellationToken)
        {
            var query = _context.HotelNightAllotments.AsNoTracking()
                .Include(a => a.RoomType)
                .Include(a => a.Hotel)
                .Where(a => a.IdHotel == idHotel
                    && a.NightDate >= fromDate
                    && a.NightDate < toDate);

            if (idSociete is > 0)
                query = query.Where(a => a.IdSociete == idSociete);

            if (publishedOnly)
            {
                query = query.Where(a =>
                    a.Status == HotelStatus.Published
                    && a.Hotel != null && a.Hotel.Status == HotelStatus.Published
                    && a.RoomType != null && a.RoomType.Status == HotelStatus.Published);
            }

            if (idHotelRoomType is > 0)
                query = query.Where(a => a.IdHotelRoomType == idHotelRoomType.Value);

            var rows = await query
                .OrderBy(a => a.NightDate)
                .ThenBy(a => a.IdHotelRoomType)
                .ToListAsync(cancellationToken);

            var nights = rows.Select(HotelAllotmentMapper.ToAvailabilityNight).ToList();
            int? minDisponible = null;
            if (idHotelRoomType is > 0 && nights.Count > 0)
                minDisponible = nights.Min(n => n.QuantiteDisponible);

            return new HotelAvailabilityResponseDto
            {
                IdHotel = idHotel,
                From = fromDate,
                To = toDate,
                InventoryMode = nameof(HotelInventoryMode.ClassQuota),
                IdHotelRoomType = idHotelRoomType is > 0 ? idHotelRoomType : null,
                MinDisponible = minDisponible,
                Nights = nights
            };
        }

        private async Task<HotelAvailabilityResponseDto> GetGlobalAsync(
            int idHotel, DateTime fromDate, DateTime toDate,
            int? idSociete, bool publishedOnly, CancellationToken cancellationToken)
        {
            var query = _context.HotelNights.AsNoTracking()
                .Include(n => n.Hotel)
                .Where(n => n.IdHotel == idHotel
                    && n.NightDate >= fromDate
                    && n.NightDate < toDate);

            if (idSociete is > 0)
                query = query.Where(n => n.IdSociete == idSociete);

            if (publishedOnly)
            {
                query = query.Where(n =>
                    n.Status == HotelStatus.Published
                    && n.Hotel != null && n.Hotel.Status == HotelStatus.Published);
            }

            var rows = await query.OrderBy(n => n.NightDate).ToListAsync(cancellationToken);
            var nights = rows.Select(n => new HotelAvailabilityNightDto
            {
                NightDate = n.NightDate.Date,
                IdHotelNight = n.IdHotelNight,
                CapaciteTotale = n.CapaciteTotale,
                QuantiteHold = n.QuantiteHold,
                QuantiteVendue = n.QuantiteVendue,
                QuantiteDisponible = HotelNightMapper.QuantiteDisponible(n),
                PrixNuit = n.PrixNuit,
                CodeDevise = n.CodeDevise
            }).ToList();

            return new HotelAvailabilityResponseDto
            {
                IdHotel = idHotel,
                From = fromDate,
                To = toDate,
                InventoryMode = nameof(HotelInventoryMode.GlobalQuota),
                MinDisponible = nights.Count > 0 ? nights.Min(n => n.QuantiteDisponible) : null,
                Nights = nights
            };
        }
    }
}
