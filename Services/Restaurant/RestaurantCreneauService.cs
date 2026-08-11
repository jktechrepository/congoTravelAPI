using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantCreneauService : IRestaurantCreneauService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<RestaurantCreneauService> _logger;

        public RestaurantCreneauService(
            CongoTravelDbContext context,
            ILogger<RestaurantCreneauService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RestaurantCreneauResponseDto> CreateDraftAsync(
            RestaurantCreateCreneauRequestDto request,
            int idSociete,
            int? idRestaurantPlanification = null,
            int? idRestaurantPlanificationPlage = null,
            CancellationToken cancellationToken = default)
        {
            var inventoryMode = ParseInventoryMode(request.InventoryMode);
            NormalizeUtcTimes(request);
            ValidateCreateRequest(request, inventoryMode);

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(
                    r => r.IdRestaurant == request.IdRestaurant && r.IdSociete == idSociete,
                    cancellationToken);

            if (restaurant == null)
                throw new KeyNotFoundException($"Établissement restaurant {request.IdRestaurant} introuvable.");

            var codeDevise = NormalizeCodeDevise(request.CodeDevise);
            var utcNow = DateTime.UtcNow;
            var creneau = new RestaurantCreneau
            {
                IdSociete = idSociete,
                IdRestaurant = request.IdRestaurant,
                DateService = request.DateService,
                StartAtUtc = request.StartAtUtc,
                EndAtUtc = request.EndAtUtc,
                InventoryMode = inventoryMode,
                Status = RestaurantStatus.Draft,
                CodeDevise = codeDevise,
                MontantAcompte = request.MontantAcompte,
                IdRestaurantPlanification = idRestaurantPlanification,
                IdRestaurantPlanificationPlage = idRestaurantPlanificationPlage,
                DateCreation = utcNow
            };

            switch (inventoryMode)
            {
                case RestaurantInventoryMode.GlobalQuota:
                    AttachGlobalQuota(creneau, request.GlobalQuota!);
                    break;
                case RestaurantInventoryMode.ClassQuota:
                    await AttachZoneQuotasAsync(
                        creneau,
                        request.ZoneQuotas!,
                        request.IdRestaurant,
                        cancellationToken);
                    break;
            }

            _context.RestaurantCreneaux.Add(creneau);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Créneau restaurant Draft créé — Id={Id}, Restaurant={IdRestaurant}, Date={Date}, Mode={Mode}",
                creneau.IdRestaurantCreneau,
                request.IdRestaurant,
                request.DateService,
                inventoryMode);

            return await LoadCreneauResponseAsync(creneau.IdRestaurantCreneau, idSociete, cancellationToken);
        }

        public async Task<RestaurantCreneauResponseDto?> GetByIdAsync(
            int idRestaurantCreneau,
            int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            var query = CreneauDetailQuery()
                .Where(c => c.IdRestaurantCreneau == idRestaurantCreneau);

            if (idSociete.HasValue && idSociete.Value > 0)
                query = query.Where(c => c.IdSociete == idSociete.Value);

            var creneau = await query.FirstOrDefaultAsync(cancellationToken);
            return creneau == null ? null : RestaurantCreneauMapper.ToResponseDto(creneau);
        }

        public async Task<RestaurantCreneauResponseDto?> GetPublishedByIdAsync(
            int idRestaurantCreneau,
            CancellationToken cancellationToken = default)
        {
            var creneau = await CreneauDetailQuery()
                .FirstOrDefaultAsync(
                    c => c.IdRestaurantCreneau == idRestaurantCreneau
                         && c.Status == RestaurantStatus.Published,
                    cancellationToken);

            return creneau == null ? null : RestaurantCreneauMapper.ToResponseDto(creneau);
        }

        public async Task<IReadOnlyList<RestaurantCreneauListItemDto>> ListAsync(
            int idSociete,
            RestaurantCreneauListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var creneaux = await BuildListQuery(idSociete, filter)
                .ToListAsync(cancellationToken);

            return creneaux
                .Select(RestaurantCreneauMapper.ToListItemDto)
                .ToList();
        }

        public async Task<IReadOnlyList<RestaurantCreneauListItemDto>> ListPublishedGlobalAsync(
            RestaurantCreneauListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var query = CreneauListQuery()
                .Where(c => c.Status == RestaurantStatus.Published
                            && c.DateService >= today);

            if (filter?.IdSociete.HasValue == true && filter.IdSociete.Value > 0)
                query = query.Where(c => c.IdSociete == filter.IdSociete.Value);

            if (filter?.IdRestaurant.HasValue == true && filter.IdRestaurant.Value > 0)
                query = query.Where(c => c.IdRestaurant == filter.IdRestaurant.Value);

            if (filter?.DateService.HasValue == true)
                query = query.Where(c => c.DateService == filter.DateService.Value);

            if (filter?.InventoryMode.HasValue == true)
                query = query.Where(c => c.InventoryMode == filter.InventoryMode.Value);

            var creneaux = await query
                .OrderBy(c => c.StartAtUtc)
                .ToListAsync(cancellationToken);

            return creneaux
                .Select(RestaurantCreneauMapper.ToListItemDto)
                .ToList();
        }

        public async Task<RestaurantCreneauResponseDto> PublishAsync(
            int idRestaurantCreneau,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var creneau = await _context.RestaurantCreneaux
                .Include(c => c.GlobalQuota)
                .Include(c => c.ZoneQuotas)
                .Include(c => c.Restaurant)
                .FirstOrDefaultAsync(
                    c => c.IdRestaurantCreneau == idRestaurantCreneau && c.IdSociete == idSociete,
                    cancellationToken);

            if (creneau == null)
                throw new KeyNotFoundException($"Créneau restaurant {idRestaurantCreneau} introuvable.");

            if (creneau.Status != RestaurantStatus.Draft)
            {
                throw new InvalidOperationException(
                    $"Seul un créneau Draft peut être publié (statut actuel : {creneau.Status}).");
            }

            if (creneau.Restaurant == null || creneau.Restaurant.Status != RestaurantStatus.Published)
            {
                throw new InvalidOperationException(
                    "L'établissement associé doit être Published avant de publier un créneau.");
            }

            ValidateInventoryForPublish(creneau);
            await EnsureNoPublishedOverlapAsync(creneau, cancellationToken);

            creneau.Status = RestaurantStatus.Published;
            creneau.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Créneau restaurant publié — Id={Id}, Societe={IdSociete}, Mode={Mode}",
                creneau.IdRestaurantCreneau,
                idSociete,
                creneau.InventoryMode);

            return await LoadCreneauResponseAsync(creneau.IdRestaurantCreneau, idSociete, cancellationToken);
        }

        private async Task EnsureNoPublishedOverlapAsync(
            RestaurantCreneau creneau,
            CancellationToken cancellationToken)
        {
            // Chevauchement demi-ouvert [StartAtUtc, EndAtUtc)
            var overlaps = await _context.RestaurantCreneaux
                .AsNoTracking()
                .AnyAsync(
                    c => c.IdRestaurant == creneau.IdRestaurant
                         && c.IdRestaurantCreneau != creneau.IdRestaurantCreneau
                         && c.Status == RestaurantStatus.Published
                         && c.StartAtUtc < creneau.EndAtUtc
                         && creneau.StartAtUtc < c.EndAtUtc,
                    cancellationToken);

            if (overlaps)
            {
                throw new RestaurantCreneauConflictException(
                    "Un créneau Published chevauche déjà cette plage horaire pour cet établissement.");
            }
        }

        private IQueryable<RestaurantCreneau> BuildListQuery(
            int idSociete,
            RestaurantCreneauListFilter? filter)
        {
            var query = CreneauListQuery()
                .Where(c => c.IdSociete == idSociete);

            if (filter?.Status.HasValue == true)
                query = query.Where(c => c.Status == filter.Status.Value);

            if (filter?.InventoryMode.HasValue == true)
                query = query.Where(c => c.InventoryMode == filter.InventoryMode.Value);

            if (filter?.IdRestaurant.HasValue == true && filter.IdRestaurant.Value > 0)
                query = query.Where(c => c.IdRestaurant == filter.IdRestaurant.Value);

            if (filter?.DateService.HasValue == true)
                query = query.Where(c => c.DateService == filter.DateService.Value);

            return query.OrderBy(c => c.StartAtUtc);
        }

        private IQueryable<RestaurantCreneau> CreneauListQuery() =>
            _context.RestaurantCreneaux
                .AsNoTracking()
                .Include(c => c.Societe)
                .Include(c => c.Restaurant!)
                    .ThenInclude(r => r.Site)
                .Include(c => c.GlobalQuota)
                .Include(c => c.ZoneQuotas)
                    .ThenInclude(q => q.Zone);

        private IQueryable<RestaurantCreneau> CreneauDetailQuery() =>
            _context.RestaurantCreneaux
                .AsNoTracking()
                .Include(c => c.Societe)
                .Include(c => c.Restaurant!)
                    .ThenInclude(r => r.Site)
                .Include(c => c.GlobalQuota)
                .Include(c => c.ZoneQuotas)
                    .ThenInclude(q => q.Zone);

        private async Task<RestaurantCreneauResponseDto> LoadCreneauResponseAsync(
            int idRestaurantCreneau,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var creneau = await CreneauDetailQuery()
                .FirstAsync(
                    c => c.IdRestaurantCreneau == idRestaurantCreneau && c.IdSociete == idSociete,
                    cancellationToken);

            return RestaurantCreneauMapper.ToResponseDto(creneau);
        }

        private static void AttachGlobalQuota(
            RestaurantCreneau creneau,
            RestaurantCreateCreneauGlobalQuotaDto global)
        {
            creneau.GlobalQuota = new RestaurantCreneauGlobalQuota
            {
                CapaciteTotale = global.CapaciteTotale,
                QuantiteHold = 0,
                QuantiteVendue = 0,
                PrixUnitaire = global.PrixUnitaire
            };
        }

        private async Task AttachZoneQuotasAsync(
            RestaurantCreneau creneau,
            IReadOnlyList<RestaurantCreateCreneauZoneQuotaDto> zoneQuotas,
            int idRestaurant,
            CancellationToken cancellationToken)
        {
            var zoneIds = zoneQuotas.Select(q => q.IdRestaurantZone).Distinct().ToList();
            var zones = await _context.RestaurantZones
                .AsNoTracking()
                .Where(z => z.IdRestaurant == idRestaurant && zoneIds.Contains(z.IdRestaurantZone))
                .ToListAsync(cancellationToken);

            if (zones.Count != zoneIds.Count)
            {
                throw new InvalidOperationException(
                    "Une ou plusieurs zones référencées sont introuvables pour cet établissement.");
            }

            if (zones.Any(z => !z.Actif))
            {
                throw new InvalidOperationException(
                    "Une ou plusieurs zones référencées sont inactives.");
            }

            foreach (var item in zoneQuotas)
            {
                creneau.ZoneQuotas.Add(new RestaurantCreneauZoneQuota
                {
                    IdRestaurantZone = item.IdRestaurantZone,
                    CapaciteTotale = item.CapaciteTotale,
                    QuantiteHold = 0,
                    QuantiteVendue = 0,
                    PrixUnitaire = item.PrixUnitaire
                });
            }
        }

        private static void NormalizeUtcTimes(RestaurantCreateCreneauRequestDto request)
        {
            if (request.StartAtUtc.Kind == DateTimeKind.Unspecified)
                request.StartAtUtc = DateTime.SpecifyKind(request.StartAtUtc, DateTimeKind.Utc);
            else if (request.StartAtUtc.Kind == DateTimeKind.Local)
                request.StartAtUtc = request.StartAtUtc.ToUniversalTime();

            if (request.EndAtUtc.Kind == DateTimeKind.Unspecified)
                request.EndAtUtc = DateTime.SpecifyKind(request.EndAtUtc, DateTimeKind.Utc);
            else if (request.EndAtUtc.Kind == DateTimeKind.Local)
                request.EndAtUtc = request.EndAtUtc.ToUniversalTime();
        }

        private static void ValidateCreateRequest(
            RestaurantCreateCreneauRequestDto request,
            RestaurantInventoryMode inventoryMode)
        {
            if (request.EndAtUtc <= request.StartAtUtc)
                throw new InvalidOperationException("EndAtUtc doit être strictement postérieur à StartAtUtc.");

            if (request.MontantAcompte.HasValue && request.MontantAcompte.Value < 0)
                throw new InvalidOperationException("MontantAcompte ne peut pas être négatif.");

            switch (inventoryMode)
            {
                case RestaurantInventoryMode.GlobalQuota:
                    ValidateGlobalQuotaCreate(request.GlobalQuota);
                    break;
                case RestaurantInventoryMode.ClassQuota:
                    ValidateZoneQuotasCreate(request.ZoneQuotas);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"InventoryMode {inventoryMode} non supporté pour la création.");
            }
        }

        private static void ValidateGlobalQuotaCreate(RestaurantCreateCreneauGlobalQuotaDto? global)
        {
            if (global == null)
                throw new InvalidOperationException("GlobalQuota est obligatoire pour InventoryMode GlobalQuota.");

            if (global.CapaciteTotale <= 0)
                throw new InvalidOperationException("CapaciteTotale doit être supérieure à 0.");

            if (global.PrixUnitaire < 0)
                throw new InvalidOperationException("PrixUnitaire ne peut pas être négatif.");
        }

        private static void ValidateZoneQuotasCreate(List<RestaurantCreateCreneauZoneQuotaDto>? zoneQuotas)
        {
            if (zoneQuotas == null || zoneQuotas.Count == 0)
            {
                throw new InvalidOperationException(
                    "ZoneQuotas est obligatoire pour InventoryMode ClassQuota (au moins une zone).");
            }

            var duplicateZone = zoneQuotas
                .GroupBy(q => q.IdRestaurantZone)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateZone != null)
            {
                throw new InvalidOperationException(
                    $"ZoneQuotas contient un doublon pour IdRestaurantZone={duplicateZone.Key}.");
            }

            foreach (var quota in zoneQuotas)
            {
                if (quota.CapaciteTotale <= 0)
                {
                    throw new InvalidOperationException(
                        $"CapaciteTotale invalide pour IdRestaurantZone={quota.IdRestaurantZone}.");
                }

                if (quota.PrixUnitaire < 0)
                {
                    throw new InvalidOperationException(
                        $"PrixUnitaire invalide pour IdRestaurantZone={quota.IdRestaurantZone}.");
                }
            }
        }

        private static void ValidateInventoryForPublish(RestaurantCreneau creneau)
        {
            switch (creneau.InventoryMode)
            {
                case RestaurantInventoryMode.GlobalQuota:
                    if (creneau.GlobalQuota == null)
                        throw new InvalidOperationException("GlobalQuota manquant pour la publication.");

                    if (creneau.GlobalQuota.CapaciteTotale <= 0)
                        throw new InvalidOperationException("CapaciteTotale doit être supérieure à 0.");
                    return;

                case RestaurantInventoryMode.ClassQuota:
                    if (creneau.ZoneQuotas.Count == 0
                        || creneau.ZoneQuotas.All(q => q.CapaciteTotale <= 0))
                    {
                        throw new InvalidOperationException(
                            "Publication impossible : au moins un zoneQuota valide est requis.");
                    }
                    return;

                default:
                    throw new InvalidOperationException(
                        $"Publication Mode {creneau.InventoryMode} : non implémentée.");
            }
        }

        private static RestaurantInventoryMode ParseInventoryMode(string? inventoryMode)
        {
            if (string.IsNullOrWhiteSpace(inventoryMode))
                throw new InvalidOperationException("InventoryMode est obligatoire.");

            if (!Enum.TryParse<RestaurantInventoryMode>(inventoryMode.Trim(), ignoreCase: true, out var mode))
            {
                throw new InvalidOperationException(
                    $"InventoryMode invalide '{inventoryMode}'. Valeurs : GlobalQuota, ClassQuota.");
            }

            return mode;
        }

        private static string NormalizeCodeDevise(string? codeDevise)
        {
            var normalized = (codeDevise ?? "CDF").Trim().ToUpperInvariant();
            if (normalized.Length != 3)
                throw new InvalidOperationException("CodeDevise doit contenir exactement 3 caractères.");
            return normalized;
        }
    }
}
