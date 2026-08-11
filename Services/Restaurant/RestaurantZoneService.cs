using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantZoneService : IRestaurantZoneService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<RestaurantZoneService> _logger;

        public RestaurantZoneService(
            CongoTravelDbContext context,
            ILogger<RestaurantZoneService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RestaurantZoneResponseDto> CreateAsync(
            RestaurantCreateZoneRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Libelle))
                throw new InvalidOperationException("Libelle est obligatoire.");

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdRestaurant == request.IdRestaurant && r.IdSociete == idSociete,
                    cancellationToken);

            if (restaurant == null)
                throw new KeyNotFoundException($"Établissement restaurant {request.IdRestaurant} introuvable.");

            string? code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
            if (code != null)
            {
                var exists = await _context.RestaurantZones
                    .AsNoTracking()
                    .AnyAsync(
                        z => z.IdRestaurant == request.IdRestaurant && z.Code == code,
                        cancellationToken);

                if (exists)
                {
                    throw new RestaurantZoneConflictException(
                        $"Une zone avec le code '{code}' existe déjà pour cet établissement.");
                }
            }

            var zone = new RestaurantZone
            {
                IdSociete = idSociete,
                IdRestaurant = request.IdRestaurant,
                Code = code,
                Libelle = request.Libelle.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Actif = true,
                DateCreation = DateTime.UtcNow
            };

            _context.RestaurantZones.Add(zone);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Zone restaurant créée — Id={Id}, Restaurant={IdRestaurant}, Code={Code}",
                zone.IdRestaurantZone,
                request.IdRestaurant,
                code);

            return RestaurantZoneMapper.ToResponseDto(zone);
        }

        public async Task<RestaurantZoneResponseDto?> GetByIdAsync(
            int idRestaurantZone,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var zone = await _context.RestaurantZones
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    z => z.IdRestaurantZone == idRestaurantZone && z.IdSociete == idSociete,
                    cancellationToken);

            return zone == null ? null : RestaurantZoneMapper.ToResponseDto(zone);
        }

        public async Task<IReadOnlyList<RestaurantZoneResponseDto>> ListAsync(
            int idSociete,
            int? idRestaurant = null,
            bool actifsSeulement = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.RestaurantZones
                .AsNoTracking()
                .Where(z => z.IdSociete == idSociete);

            if (idRestaurant is > 0)
                query = query.Where(z => z.IdRestaurant == idRestaurant.Value);

            if (actifsSeulement)
                query = query.Where(z => z.Actif);

            var zones = await query
                .OrderBy(z => z.Libelle)
                .ToListAsync(cancellationToken);

            return zones.Select(RestaurantZoneMapper.ToResponseDto).ToList();
        }

        public async Task<RestaurantZoneResponseDto?> UpdateAsync(
            int idRestaurantZone,
            RestaurantUpdateZoneRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Libelle))
                throw new InvalidOperationException("Libelle est obligatoire.");

            var zone = await _context.RestaurantZones
                .FirstOrDefaultAsync(
                    z => z.IdRestaurantZone == idRestaurantZone && z.IdSociete == idSociete,
                    cancellationToken);

            if (zone == null)
                return null;

            zone.Libelle = request.Libelle.Trim();
            zone.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            zone.Actif = request.Actif;
            zone.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return RestaurantZoneMapper.ToResponseDto(zone);
        }

        public async Task<RestaurantZoneResponseDto?> ToggleStatutAsync(
            int idRestaurantZone,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var zone = await _context.RestaurantZones
                .FirstOrDefaultAsync(
                    z => z.IdRestaurantZone == idRestaurantZone && z.IdSociete == idSociete,
                    cancellationToken);

            if (zone == null)
                return null;

            zone.Actif = !zone.Actif;
            zone.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return RestaurantZoneMapper.ToResponseDto(zone);
        }
    }
}
