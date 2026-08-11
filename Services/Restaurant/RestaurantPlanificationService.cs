using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantPlanificationService : IRestaurantPlanificationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<RestaurantPlanificationService> _logger;

        public RestaurantPlanificationService(
            CongoTravelDbContext context,
            ILogger<RestaurantPlanificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<RestaurantPlanificationListItemDto>> ListAsync(
            int idSociete,
            int? idRestaurant = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.RestaurantPlanifications.AsNoTracking()
                .Include(p => p.Restaurant)
                .Include(p => p.Plages)
                .Where(p => p.IdSociete == idSociete);

            if (idRestaurant is > 0)
                query = query.Where(p => p.IdRestaurant == idRestaurant.Value);

            var items = await query
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync(cancellationToken);

            var counts = await _context.RestaurantCreneaux.AsNoTracking()
                .Where(c => c.IdRestaurantPlanification.HasValue)
                .GroupBy(c => c.IdRestaurantPlanification!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

            return items.Select(p => MapToListItem(p, counts.GetValueOrDefault(p.IdRestaurantPlanification))).ToList();
        }

        public async Task<RestaurantPlanificationResponseDto?> GetByIdAsync(
            int id,
            int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            var query = DetailQuery().Where(p => p.IdRestaurantPlanification == id);
            if (idSociete is > 0)
                query = query.Where(p => p.IdSociete == idSociete.Value);

            var entity = await query.FirstOrDefaultAsync(cancellationToken);
            if (entity == null)
                return null;

            var count = await _context.RestaurantCreneaux.AsNoTracking()
                .CountAsync(c => c.IdRestaurantPlanification == id, cancellationToken);

            return MapToDetail(entity, count);
        }

        public async Task<RestaurantPlanificationResponseDto> CreateAsync(
            RestaurantCreatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            await ValidateRequestAsync(request, idSociete, cancellationToken);
            RestaurantPlanificationTimeHelper.ValidateNoOverlappingPlages(
                request.Plages.Select(p => (p.StartTime, p.EndTime)).ToList());

            var entity = new RestaurantPlanification
            {
                IdSociete = idSociete,
                IdRestaurant = request.IdRestaurant,
                Libelle = request.Libelle.Trim(),
                JoursSemaine = request.JoursSemaine.Distinct().OrderBy(j => j).ToList(),
                InventoryMode = request.InventoryMode,
                CodeDevise = NormalizeCodeDevise(request.CodeDevise),
                MontantAcompte = request.MontantAcompte,
                Statut = request.Statut,
                DateCreation = DateTime.UtcNow
            };

            AttachPlages(entity, request);

            _context.RestaurantPlanifications.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Planification restaurant créée {Id} société {SocieteId}",
                entity.IdRestaurantPlanification,
                idSociete);

            return (await GetByIdAsync(entity.IdRestaurantPlanification, idSociete, cancellationToken))!;
        }

        public async Task<RestaurantPlanificationResponseDto?> UpdateAsync(
            RestaurantUpdatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.RestaurantPlanifications
                .Include(p => p.Plages)
                    .ThenInclude(pl => pl.GlobalQuota)
                .Include(p => p.Plages)
                    .ThenInclude(pl => pl.ZoneQuotas)
                .FirstOrDefaultAsync(
                    p => p.IdRestaurantPlanification == request.IdRestaurantPlanification
                         && p.IdSociete == idSociete,
                    cancellationToken);

            if (entity == null)
                return null;

            await ValidateRequestAsync(request, idSociete, cancellationToken);
            RestaurantPlanificationTimeHelper.ValidateNoOverlappingPlages(
                request.Plages.Select(p => (p.StartTime, p.EndTime)).ToList());

            entity.Libelle = request.Libelle.Trim();
            entity.IdRestaurant = request.IdRestaurant;
            entity.JoursSemaine = request.JoursSemaine.Distinct().OrderBy(j => j).ToList();
            entity.InventoryMode = request.InventoryMode;
            entity.CodeDevise = NormalizeCodeDevise(request.CodeDevise);
            entity.MontantAcompte = request.MontantAcompte;
            entity.Statut = request.Statut;
            entity.DateModification = DateTime.UtcNow;

            if (entity.Plages.Count > 0)
                _context.RestaurantPlanificationPlages.RemoveRange(entity.Plages);

            entity.Plages = new List<RestaurantPlanificationPlage>();
            AttachPlages(entity, request);

            await _context.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(entity.IdRestaurantPlanification, idSociete, cancellationToken);
        }

        public async Task<bool> ToggleStatutAsync(int id, int idSociete, CancellationToken cancellationToken = default)
        {
            var entity = await _context.RestaurantPlanifications
                .FirstOrDefaultAsync(
                    p => p.IdRestaurantPlanification == id && p.IdSociete == idSociete,
                    cancellationToken);
            if (entity == null)
                return false;

            entity.Statut = !entity.Statut;
            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int idSociete, CancellationToken cancellationToken = default)
        {
            var entity = await _context.RestaurantPlanifications
                .Include(p => p.Plages)
                    .ThenInclude(pl => pl.GlobalQuota)
                .Include(p => p.Plages)
                    .ThenInclude(pl => pl.ZoneQuotas)
                .FirstOrDefaultAsync(
                    p => p.IdRestaurantPlanification == id && p.IdSociete == idSociete,
                    cancellationToken);
            if (entity == null)
                return false;

            var creneauIds = await _context.RestaurantCreneaux.AsNoTracking()
                .Where(c => c.IdRestaurantPlanification == id)
                .Select(c => c.IdRestaurantCreneau)
                .ToListAsync(cancellationToken);

            if (creneauIds.Count > 0)
            {
                var hasReservations = await _context.RestaurantReservations.AsNoTracking()
                    .AnyAsync(r => creneauIds.Contains(r.IdRestaurantCreneau), cancellationToken);

                if (hasReservations)
                {
                    entity.Statut = false;
                    entity.DateModification = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    return true;
                }

                var creneaux = await _context.RestaurantCreneaux
                    .Include(c => c.GlobalQuota)
                    .Include(c => c.ZoneQuotas)
                    .Where(c => c.IdRestaurantPlanification == id)
                    .ToListAsync(cancellationToken);
                _context.RestaurantCreneaux.RemoveRange(creneaux);
            }

            _context.RestaurantPlanifications.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private IQueryable<RestaurantPlanification> DetailQuery() =>
            _context.RestaurantPlanifications.AsNoTracking()
                .Include(p => p.Restaurant)
                .Include(p => p.Plages)
                    .ThenInclude(pl => pl.GlobalQuota)
                .Include(p => p.Plages)
                    .ThenInclude(pl => pl.ZoneQuotas)
                        .ThenInclude(q => q.Zone);

        private async Task ValidateRequestAsync(
            RestaurantCreatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var restaurant = await _context.Restaurants.AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdRestaurant == request.IdRestaurant, cancellationToken);
            if (restaurant == null)
                throw new ArgumentException($"Établissement restaurant {request.IdRestaurant} introuvable.");
            if (restaurant.IdSociete != idSociete)
                throw new ArgumentException($"Le restaurant {request.IdRestaurant} n'appartient pas à la société {idSociete}.");

            NormalizeCodeDevise(request.CodeDevise);

            if (request.MontantAcompte.HasValue && request.MontantAcompte.Value < 0)
                throw new ArgumentException("MontantAcompte ne peut pas être négatif.");

            if (request.InventoryMode == RestaurantInventoryMode.ClassQuota)
            {
                var zoneIds = request.Plages
                    .SelectMany(p => p.ZoneQuotas ?? new List<RestaurantCreatePlanificationZoneQuotaDto>())
                    .Select(q => q.IdRestaurantZone)
                    .Distinct()
                    .ToList();

                var zones = await _context.RestaurantZones.AsNoTracking()
                    .Where(z => zoneIds.Contains(z.IdRestaurantZone))
                    .Select(z => new { z.IdRestaurantZone, z.IdRestaurant, z.Actif })
                    .ToListAsync(cancellationToken);

                if (zones.Count != zoneIds.Count)
                    throw new ArgumentException("Une ou plusieurs zones sont introuvables.");

                if (zones.Any(z => z.IdRestaurant != request.IdRestaurant))
                    throw new ArgumentException("Toutes les zones doivent appartenir à l'établissement du template.");

                if (zones.Any(z => !z.Actif))
                    throw new ArgumentException("Une ou plusieurs zones référencées sont inactives.");
            }
        }

        private static void AttachPlages(
            RestaurantPlanification entity,
            RestaurantCreatePlanificationRequestDto request)
        {
            var ordered = request.Plages
                .Select((p, index) => (Plage: p, Index: index))
                .OrderBy(x => x.Plage.Ordre)
                .ThenBy(x => x.Index)
                .ToList();

            var ordre = 0;
            foreach (var item in ordered)
            {
                var dto = item.Plage;
                var plage = new RestaurantPlanificationPlage
                {
                    Ordre = ordre++,
                    Libelle = string.IsNullOrWhiteSpace(dto.Libelle) ? null : dto.Libelle.Trim(),
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime
                };

                switch (request.InventoryMode)
                {
                    case RestaurantInventoryMode.GlobalQuota:
                        plage.GlobalQuota = new RestaurantPlanifPlageGlobalQuota
                        {
                            CapaciteTotale = dto.GlobalQuota!.CapaciteTotale,
                            PrixUnitaire = dto.GlobalQuota.PrixUnitaire
                        };
                        break;

                    case RestaurantInventoryMode.ClassQuota:
                        foreach (var q in dto.ZoneQuotas!)
                        {
                            plage.ZoneQuotas.Add(new RestaurantPlanifPlageZoneQuota
                            {
                                IdRestaurantZone = q.IdRestaurantZone,
                                CapaciteTotale = q.CapaciteTotale,
                                PrixUnitaire = q.PrixUnitaire
                            });
                        }
                        break;
                }

                entity.Plages.Add(plage);
            }
        }

        private static RestaurantPlanificationListItemDto MapToListItem(
            RestaurantPlanification entity,
            int nombreCreneaux) =>
            new()
            {
                IdRestaurantPlanification = entity.IdRestaurantPlanification,
                IdSociete = entity.IdSociete,
                IdRestaurant = entity.IdRestaurant,
                RestaurantNom = entity.Restaurant?.Nom,
                Libelle = entity.Libelle,
                JoursSemaine = entity.JoursSemaine,
                InventoryMode = entity.InventoryMode,
                CodeDevise = entity.CodeDevise,
                Statut = entity.Statut,
                NombrePlages = entity.Plages?.Count ?? 0,
                NombreCreneauxGeneres = nombreCreneaux,
                DateCreation = entity.DateCreation,
                DateModification = entity.DateModification
            };

        private static RestaurantPlanificationResponseDto MapToDetail(
            RestaurantPlanification entity,
            int nombreCreneaux)
        {
            var dto = new RestaurantPlanificationResponseDto
            {
                IdRestaurantPlanification = entity.IdRestaurantPlanification,
                IdSociete = entity.IdSociete,
                IdRestaurant = entity.IdRestaurant,
                RestaurantNom = entity.Restaurant?.Nom,
                Libelle = entity.Libelle,
                JoursSemaine = entity.JoursSemaine,
                InventoryMode = entity.InventoryMode,
                CodeDevise = entity.CodeDevise,
                MontantAcompte = entity.MontantAcompte,
                Statut = entity.Statut,
                NombrePlages = entity.Plages?.Count ?? 0,
                NombreCreneauxGeneres = nombreCreneaux,
                DateCreation = entity.DateCreation,
                DateModification = entity.DateModification,
                Plages = (entity.Plages ?? Array.Empty<RestaurantPlanificationPlage>())
                    .OrderBy(p => p.Ordre)
                    .ThenBy(p => p.StartTime)
                    .Select(MapPlage)
                    .ToList()
            };

            return dto;
        }

        private static RestaurantPlanificationPlageResponseDto MapPlage(RestaurantPlanificationPlage plage)
        {
            var dto = new RestaurantPlanificationPlageResponseDto
            {
                IdRestaurantPlanificationPlage = plage.IdRestaurantPlanificationPlage,
                Ordre = plage.Ordre,
                Libelle = plage.Libelle,
                StartTime = plage.StartTime,
                EndTime = plage.EndTime
            };

            if (plage.GlobalQuota != null)
            {
                dto.GlobalQuota = new RestaurantPlanificationGlobalQuotaResponseDto
                {
                    CapaciteTotale = plage.GlobalQuota.CapaciteTotale,
                    PrixUnitaire = plage.GlobalQuota.PrixUnitaire
                };
            }

            dto.ZoneQuotas = (plage.ZoneQuotas ?? Array.Empty<RestaurantPlanifPlageZoneQuota>())
                .Select(q => new RestaurantPlanificationZoneQuotaResponseDto
                {
                    IdRestaurantPlanifPlageZoneQuota = q.IdRestaurantPlanifPlageZoneQuota,
                    IdRestaurantZone = q.IdRestaurantZone,
                    ZoneLibelle = q.Zone?.Libelle,
                    CapaciteTotale = q.CapaciteTotale,
                    PrixUnitaire = q.PrixUnitaire
                })
                .ToList();

            return dto;
        }

        private static string NormalizeCodeDevise(string codeDevise)
        {
            var normalized = string.IsNullOrWhiteSpace(codeDevise)
                ? "CDF"
                : codeDevise.Trim().ToUpperInvariant();

            if (normalized is not ("CDF" or "USD"))
                throw new ArgumentException("CodeDevise invalide. Valeurs acceptées : CDF, USD.");

            return normalized;
        }
    }
}
