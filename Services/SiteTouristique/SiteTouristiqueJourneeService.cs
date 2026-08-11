using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueJourneeService : ISiteTouristiqueJourneeService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<SiteTouristiqueJourneeService> _logger;

        public SiteTouristiqueJourneeService(
            CongoTravelDbContext context,
            ILogger<SiteTouristiqueJourneeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SiteTouristiqueJourneeResponseDto> CreateDraftAsync(
            SiteTouristiqueCreateJourneeRequestDto request,
            int idSociete,
            int? idSiteTouristiquePlanification = null,
            CancellationToken cancellationToken = default)
        {
            var inventoryMode = ParseInventoryMode(request.InventoryMode);
            request.SalesOpenAtUtc = SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(request.SalesOpenAtUtc);
            request.SalesCloseAtUtc = SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(request.SalesCloseAtUtc);
            ValidateCreateRequest(request, inventoryMode);

            var lieu = await _context.SiteTouristiques
                .FirstOrDefaultAsync(
                    l => l.IdSiteTouristique == request.IdSiteTouristique && l.IdSociete == idSociete,
                    cancellationToken);

            if (lieu == null)
                throw new KeyNotFoundException($"Lieu touristique {request.IdSiteTouristique} introuvable.");

            var exists = await _context.SiteTouristiqueJournees
                .AsNoTracking()
                .AnyAsync(
                    j => j.IdSiteTouristique == request.IdSiteTouristique
                         && j.DateVisite == request.DateVisite,
                    cancellationToken);

            if (exists)
            {
                throw new SiteTouristiqueJourneeConflictException(
                    $"Une journée existe déjà pour le lieu {request.IdSiteTouristique} à la date {request.DateVisite:yyyy-MM-dd}.");
            }

            var codeDevise = NormalizeCodeDevise(request.CodeDevise);
            var utcNow = DateTime.UtcNow;
            var journee = new SiteTouristiqueJournee
            {
                IdSociete = idSociete,
                IdSiteTouristique = request.IdSiteTouristique,
                DateVisite = request.DateVisite,
                InventoryMode = inventoryMode,
                Status = SiteTouristiqueStatus.Draft,
                CodeDevise = codeDevise,
                SalesOpenAtUtc = request.SalesOpenAtUtc,
                SalesCloseAtUtc = request.SalesCloseAtUtc,
                IdSiteTouristiquePlanification = idSiteTouristiquePlanification,
                DateCreation = utcNow
            };

            switch (inventoryMode)
            {
                case SiteTouristiqueInventoryMode.GlobalQuota:
                    AttachGlobalQuota(journee, request.GlobalQuota!);
                    break;

                case SiteTouristiqueInventoryMode.ClassQuota:
                    await AttachClassQuotasAsync(
                        journee, request.ClassQuotas!, idSociete, cancellationToken);
                    break;
            }

            _context.SiteTouristiqueJournees.Add(journee);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Journée site touristique Draft créée — Id={Id}, Lieu={IdLieu}, Date={Date}, Mode={Mode}",
                journee.IdSiteTouristiqueJournee,
                request.IdSiteTouristique,
                request.DateVisite,
                inventoryMode);

            return await LoadJourneeResponseAsync(journee.IdSiteTouristiqueJournee, idSociete, cancellationToken);
        }

        public async Task<SiteTouristiqueJourneeResponseDto?> GetByIdAsync(
            int idSiteTouristiqueJournee,
            int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            var query = JourneeDetailQuery()
                .Where(j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee);

            if (idSociete.HasValue && idSociete.Value > 0)
                query = query.Where(j => j.IdSociete == idSociete.Value);

            var journee = await query.FirstOrDefaultAsync(cancellationToken);
            return journee == null ? null : SiteTouristiqueJourneeMapper.ToResponseDto(journee);
        }

        public async Task<SiteTouristiqueJourneeResponseDto?> GetPublishedByIdAsync(
            int idSiteTouristiqueJournee,
            CancellationToken cancellationToken = default)
        {
            var journee = await JourneeDetailQuery()
                .FirstOrDefaultAsync(
                    j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee
                         && j.Status == SiteTouristiqueStatus.Published,
                    cancellationToken);

            return journee == null ? null : SiteTouristiqueJourneeMapper.ToResponseDto(journee);
        }

        public async Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListAsync(
            int idSociete,
            SiteTouristiqueJourneeListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var journees = await BuildListQuery(idSociete, filter)
                .ToListAsync(cancellationToken);

            return journees
                .Select(SiteTouristiqueJourneeMapper.ToListItemDto)
                .ToList();
        }

        public async Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListPublishedGlobalAsync(
            SiteTouristiqueJourneeListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var query = JourneeListQuery()
                .Where(j => j.Status == SiteTouristiqueStatus.Published
                            && j.DateVisite >= today);

            if (filter?.IdSociete.HasValue == true && filter.IdSociete.Value > 0)
                query = query.Where(j => j.IdSociete == filter.IdSociete.Value);

            if (filter?.IdSiteTouristique.HasValue == true && filter.IdSiteTouristique.Value > 0)
                query = query.Where(j => j.IdSiteTouristique == filter.IdSiteTouristique.Value);

            if (filter?.InventoryMode.HasValue == true)
                query = query.Where(j => j.InventoryMode == filter.InventoryMode.Value);

            var journees = await query
                .OrderBy(j => j.DateVisite)
                .ToListAsync(cancellationToken);

            return journees
                .Select(SiteTouristiqueJourneeMapper.ToListItemDto)
                .ToList();
        }

        public Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByStatusAsync(
            SiteTouristiqueStatus status,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new SiteTouristiqueJourneeListFilter { Status = status },
                cancellationToken);

        public Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByInventoryModeAsync(
            SiteTouristiqueInventoryMode inventoryMode,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new SiteTouristiqueJourneeListFilter { InventoryMode = inventoryMode },
                cancellationToken);

        public async Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByDateAsync(
            DateOnly dateVisite,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var journees = await JourneeListQuery()
                .Where(j => j.IdSociete == idSociete && j.DateVisite == dateVisite)
                .OrderBy(j => j.DateVisite)
                .ToListAsync(cancellationToken);

            return journees
                .Select(SiteTouristiqueJourneeMapper.ToListItemDto)
                .ToList();
        }

        public async Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByDateRangeAsync(
            DateOnly dateDebut,
            DateOnly dateFin,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var journees = await JourneeListQuery()
                .Where(j => j.IdSociete == idSociete
                            && j.DateVisite >= dateDebut
                            && j.DateVisite <= dateFin)
                .OrderBy(j => j.DateVisite)
                .ToListAsync(cancellationToken);

            return journees
                .Select(SiteTouristiqueJourneeMapper.ToListItemDto)
                .ToList();
        }

        public async Task<SiteTouristiqueJourneeResponseDto> PublishAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var journee = await _context.SiteTouristiqueJournees
                .Include(j => j.GlobalQuota)
                .Include(j => j.ClassQuotas)
                    .ThenInclude(q => q.Classe)
                .Include(j => j.Lieu)
                .FirstOrDefaultAsync(
                    j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && j.IdSociete == idSociete,
                    cancellationToken);

            if (journee == null)
                throw new KeyNotFoundException($"Journée site touristique {idSiteTouristiqueJournee} introuvable.");

            if (journee.Status != SiteTouristiqueStatus.Draft)
            {
                throw new InvalidOperationException(
                    $"Seule une journée Draft peut être publiée (statut actuel : {journee.Status}).");
            }

            if (journee.Lieu == null || journee.Lieu.Status != SiteTouristiqueStatus.Published)
            {
                throw new InvalidOperationException(
                    "Le lieu associé doit être Published avant de publier une journée.");
            }

            ValidateInventoryForPublish(journee);

            journee.Status = SiteTouristiqueStatus.Published;
            journee.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Journée site touristique publiée — Id={Id}, Societe={IdSociete}, Mode={Mode}",
                journee.IdSiteTouristiqueJournee,
                idSociete,
                journee.InventoryMode);

            return await LoadJourneeResponseAsync(journee.IdSiteTouristiqueJournee, idSociete, cancellationToken);
        }

        private IQueryable<SiteTouristiqueJournee> BuildListQuery(
            int idSociete,
            SiteTouristiqueJourneeListFilter? filter)
        {
            var query = JourneeListQuery()
                .Where(j => j.IdSociete == idSociete);

            if (filter?.Status.HasValue == true)
                query = query.Where(j => j.Status == filter.Status.Value);

            if (filter?.InventoryMode.HasValue == true)
                query = query.Where(j => j.InventoryMode == filter.InventoryMode.Value);

            if (filter?.IdSiteTouristique.HasValue == true && filter.IdSiteTouristique.Value > 0)
                query = query.Where(j => j.IdSiteTouristique == filter.IdSiteTouristique.Value);

            if (filter?.DateVisite.HasValue == true)
                query = query.Where(j => j.DateVisite == filter.DateVisite.Value);

            if (filter?.DateVisiteFrom.HasValue == true)
                query = query.Where(j => j.DateVisite >= filter.DateVisiteFrom.Value);

            if (filter?.DateVisiteTo.HasValue == true)
                query = query.Where(j => j.DateVisite <= filter.DateVisiteTo.Value);

            return query.OrderByDescending(j => j.DateVisite);
        }

        private IQueryable<SiteTouristiqueJournee> JourneeListQuery() =>
            _context.SiteTouristiqueJournees
                .AsNoTracking()
                .Include(j => j.Societe)
                .Include(j => j.Lieu!)
                    .ThenInclude(l => l.Site)
                .Include(j => j.GlobalQuota)
                .Include(j => j.ClassQuotas);

        private IQueryable<SiteTouristiqueJournee> JourneeDetailQuery() =>
            _context.SiteTouristiqueJournees
                .AsNoTracking()
                .Include(j => j.Societe)
                .Include(j => j.Lieu!)
                    .ThenInclude(l => l.Site)
                .Include(j => j.GlobalQuota)
                .Include(j => j.ClassQuotas)
                    .ThenInclude(q => q.Classe);

        private async Task<SiteTouristiqueJourneeResponseDto> LoadJourneeResponseAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var journee = await JourneeDetailQuery()
                .FirstAsync(
                    j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && j.IdSociete == idSociete,
                    cancellationToken);

            return SiteTouristiqueJourneeMapper.ToResponseDto(journee);
        }

        private static void AttachGlobalQuota(
            SiteTouristiqueJournee journee,
            SiteTouristiqueCreateJourneeGlobalQuotaDto global)
        {
            journee.GlobalQuota = new SiteTouristiqueGlobalQuota
            {
                CapaciteTotale = global.CapaciteTotale,
                QuantiteHold = 0,
                QuantiteVendue = 0,
                PrixUnitaire = global.PrixUnitaire
            };
        }

        private async Task AttachClassQuotasAsync(
            SiteTouristiqueJournee journee,
            IReadOnlyList<SiteTouristiqueCreateJourneeClassQuotaDto> classQuotas,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var classeIds = classQuotas.Select(q => q.IdSiteTouristiqueClasse).Distinct().ToList();
            var classes = await _context.SiteTouristiqueClasses
                .AsNoTracking()
                .Where(c => c.IdSociete == idSociete && classeIds.Contains(c.IdSiteTouristiqueClasse))
                .ToListAsync(cancellationToken);

            if (classes.Count != classeIds.Count)
            {
                throw new InvalidOperationException(
                    "Une ou plusieurs classes référencées sont introuvables pour cette société.");
            }

            if (classes.Any(c => !c.Actif))
            {
                throw new InvalidOperationException(
                    "Une ou plusieurs classes référencées sont inactives.");
            }

            foreach (var item in classQuotas)
            {
                journee.ClassQuotas.Add(new SiteTouristiqueClassQuota
                {
                    IdSiteTouristiqueClasse = item.IdSiteTouristiqueClasse,
                    CapaciteTotale = item.CapaciteTotale,
                    QuantiteHold = 0,
                    QuantiteVendue = 0,
                    PrixUnitaire = item.PrixUnitaire
                });
            }
        }

        private static void ValidateCreateRequest(
            SiteTouristiqueCreateJourneeRequestDto request,
            SiteTouristiqueInventoryMode inventoryMode)
        {
            if (request.IdSiteTouristique <= 0)
                throw new InvalidOperationException("IdSiteTouristique est obligatoire.");

            if (request.SalesCloseAtUtc.HasValue
                && request.SalesOpenAtUtc.HasValue
                && request.SalesCloseAtUtc.Value < request.SalesOpenAtUtc.Value)
            {
                throw new InvalidOperationException(
                    "SalesCloseAtUtc doit être postérieur ou égal à SalesOpenAtUtc.");
            }

            switch (inventoryMode)
            {
                case SiteTouristiqueInventoryMode.GlobalQuota:
                    ValidateGlobalQuotaCreate(request.GlobalQuota);
                    break;

                case SiteTouristiqueInventoryMode.ClassQuota:
                    ValidateClassQuotasCreate(request.ClassQuotas);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"InventoryMode {inventoryMode} non supporté pour la création.");
            }
        }

        private static void ValidateGlobalQuotaCreate(SiteTouristiqueCreateJourneeGlobalQuotaDto? global)
        {
            if (global == null)
                throw new InvalidOperationException("GlobalQuota est obligatoire pour InventoryMode GlobalQuota.");

            if (global.CapaciteTotale <= 0)
                throw new InvalidOperationException("CapaciteTotale doit être strictement positive.");

            if (global.PrixUnitaire < 0)
                throw new InvalidOperationException("PrixUnitaire ne peut pas être négatif.");
        }

        private static void ValidateClassQuotasCreate(List<SiteTouristiqueCreateJourneeClassQuotaDto>? classQuotas)
        {
            if (classQuotas == null || classQuotas.Count == 0)
            {
                throw new InvalidOperationException(
                    "ClassQuotas est obligatoire pour InventoryMode ClassQuota (au moins une classe).");
            }

            var duplicateClasse = classQuotas
                .GroupBy(q => q.IdSiteTouristiqueClasse)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateClasse != null)
            {
                throw new InvalidOperationException(
                    $"ClassQuotas contient un doublon pour IdSiteTouristiqueClasse={duplicateClasse.Key}.");
            }

            foreach (var quota in classQuotas)
            {
                if (quota.CapaciteTotale <= 0)
                {
                    throw new InvalidOperationException(
                        $"CapaciteTotale invalide pour IdSiteTouristiqueClasse={quota.IdSiteTouristiqueClasse}.");
                }

                if (quota.PrixUnitaire < 0)
                {
                    throw new InvalidOperationException(
                        $"PrixUnitaire invalide pour IdSiteTouristiqueClasse={quota.IdSiteTouristiqueClasse}.");
                }
            }
        }

        private static void ValidateInventoryForPublish(SiteTouristiqueJournee journee)
        {
            switch (journee.InventoryMode)
            {
                case SiteTouristiqueInventoryMode.GlobalQuota:
                    if (journee.GlobalQuota == null || journee.GlobalQuota.CapaciteTotale <= 0)
                    {
                        throw new InvalidOperationException(
                            "Publication impossible : quota global manquant ou capacité invalide.");
                    }

                    return;

                case SiteTouristiqueInventoryMode.ClassQuota:
                    if (journee.ClassQuotas.Count == 0
                        || journee.ClassQuotas.All(q => q.CapaciteTotale <= 0))
                    {
                        throw new InvalidOperationException(
                            "Publication impossible : au moins un quota classe valide est requis.");
                    }

                    return;

                default:
                    throw new InvalidOperationException(
                        $"Publication Mode {journee.InventoryMode} : non implémentée.");
            }
        }

        private static SiteTouristiqueInventoryMode ParseInventoryMode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return SiteTouristiqueInventoryMode.GlobalQuota;

            if (!Enum.TryParse<SiteTouristiqueInventoryMode>(value.Trim(), ignoreCase: true, out var mode))
            {
                throw new InvalidOperationException(
                    $"InventoryMode invalide : '{value}'. Valeurs : ClassQuota, GlobalQuota.");
            }

            return mode;
        }

        private static string NormalizeCodeDevise(string codeDevise)
        {
            var normalized = string.IsNullOrWhiteSpace(codeDevise)
                ? "CDF"
                : codeDevise.Trim().ToUpperInvariant();

            if (normalized is not ("CDF" or "USD"))
            {
                throw new InvalidOperationException(
                    "CodeDevise invalide. Valeurs acceptées : CDF, USD.");
            }

            return normalized;
        }
    }
}
