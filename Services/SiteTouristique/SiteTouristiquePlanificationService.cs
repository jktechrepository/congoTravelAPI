using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiquePlanificationService : ISiteTouristiquePlanificationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<SiteTouristiquePlanificationService> _logger;

        public SiteTouristiquePlanificationService(
            CongoTravelDbContext context,
            ILogger<SiteTouristiquePlanificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<SiteTouristiquePlanificationListItemDto>> ListAsync(
            int idSociete,
            int? idSiteTouristique = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.SiteTouristiquePlanifications.AsNoTracking()
                .Include(p => p.Lieu)
                .Where(p => p.IdSociete == idSociete);

            if (idSiteTouristique is > 0)
                query = query.Where(p => p.IdSiteTouristique == idSiteTouristique.Value);

            var items = await query
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync(cancellationToken);

            var counts = await _context.SiteTouristiqueJournees.AsNoTracking()
                .Where(j => j.IdSiteTouristiquePlanification.HasValue)
                .GroupBy(j => j.IdSiteTouristiquePlanification!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

            return items.Select(p => MapToListItem(p, counts.GetValueOrDefault(p.IdSiteTouristiquePlanification))).ToList();
        }

        public async Task<SiteTouristiquePlanificationResponseDto?> GetByIdAsync(
            int id,
            int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            var query = DetailQuery().Where(p => p.IdSiteTouristiquePlanification == id);
            if (idSociete is > 0)
                query = query.Where(p => p.IdSociete == idSociete.Value);

            var entity = await query.FirstOrDefaultAsync(cancellationToken);
            if (entity == null)
                return null;

            var count = await _context.SiteTouristiqueJournees.AsNoTracking()
                .CountAsync(j => j.IdSiteTouristiquePlanification == id, cancellationToken);

            return MapToDetail(entity, count);
        }

        public async Task<SiteTouristiquePlanificationResponseDto> CreateAsync(
            SiteTouristiqueCreatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            await ValidateRequestAsync(request, idSociete, cancellationToken);

            var entity = new SiteTouristiquePlanification
            {
                IdSociete = idSociete,
                IdSiteTouristique = request.IdSiteTouristique,
                Libelle = request.Libelle.Trim(),
                JoursSemaine = request.JoursSemaine.Distinct().OrderBy(j => j).ToList(),
                InventoryMode = request.InventoryMode,
                CodeDevise = NormalizeCodeDevise(request.CodeDevise),
                SalesOpenOffsetHours = request.SalesOpenOffsetHours,
                SalesCloseOffsetHours = request.SalesCloseOffsetHours,
                Statut = request.Statut,
                DateCreation = DateTime.UtcNow
            };

            AttachQuotas(entity, request);

            _context.SiteTouristiquePlanifications.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Planification site touristique créée {Id} société {SocieteId}",
                entity.IdSiteTouristiquePlanification,
                idSociete);

            return (await GetByIdAsync(entity.IdSiteTouristiquePlanification, idSociete, cancellationToken))!;
        }

        public async Task<SiteTouristiquePlanificationResponseDto?> UpdateAsync(
            SiteTouristiqueUpdatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.SiteTouristiquePlanifications
                .Include(p => p.GlobalQuota)
                .Include(p => p.ClassQuotas)
                .FirstOrDefaultAsync(
                    p => p.IdSiteTouristiquePlanification == request.IdSiteTouristiquePlanification
                         && p.IdSociete == idSociete,
                    cancellationToken);

            if (entity == null)
                return null;

            await ValidateRequestAsync(request, idSociete, cancellationToken);

            entity.Libelle = request.Libelle.Trim();
            entity.IdSiteTouristique = request.IdSiteTouristique;
            entity.JoursSemaine = request.JoursSemaine.Distinct().OrderBy(j => j).ToList();
            entity.InventoryMode = request.InventoryMode;
            entity.CodeDevise = NormalizeCodeDevise(request.CodeDevise);
            entity.SalesOpenOffsetHours = request.SalesOpenOffsetHours;
            entity.SalesCloseOffsetHours = request.SalesCloseOffsetHours;
            entity.Statut = request.Statut;
            entity.DateModification = DateTime.UtcNow;

            if (entity.GlobalQuota != null)
                _context.SiteTouristiquePlanifGlobalQuotas.Remove(entity.GlobalQuota);
            if (entity.ClassQuotas.Count > 0)
                _context.SiteTouristiquePlanifClassQuotas.RemoveRange(entity.ClassQuotas);

            entity.GlobalQuota = null;
            entity.ClassQuotas = new List<SiteTouristiquePlanifClassQuota>();
            AttachQuotas(entity, request);

            await _context.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(entity.IdSiteTouristiquePlanification, idSociete, cancellationToken);
        }

        public async Task<bool> ToggleStatutAsync(int id, int idSociete, CancellationToken cancellationToken = default)
        {
            var entity = await _context.SiteTouristiquePlanifications
                .FirstOrDefaultAsync(
                    p => p.IdSiteTouristiquePlanification == id && p.IdSociete == idSociete,
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
            var entity = await _context.SiteTouristiquePlanifications
                .Include(p => p.GlobalQuota)
                .Include(p => p.ClassQuotas)
                .FirstOrDefaultAsync(
                    p => p.IdSiteTouristiquePlanification == id && p.IdSociete == idSociete,
                    cancellationToken);
            if (entity == null)
                return false;

            var journeeIds = await _context.SiteTouristiqueJournees.AsNoTracking()
                .Where(j => j.IdSiteTouristiquePlanification == id)
                .Select(j => j.IdSiteTouristiqueJournee)
                .ToListAsync(cancellationToken);

            if (journeeIds.Count > 0)
            {
                var hasReservations = await _context.SiteTouristiqueReservations.AsNoTracking()
                    .AnyAsync(r => journeeIds.Contains(r.IdSiteTouristiqueJournee), cancellationToken);

                if (hasReservations)
                {
                    entity.Statut = false;
                    entity.DateModification = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    return true;
                }

                // Journées sans réservation : suppression des drafts vides puis du template
                var journees = await _context.SiteTouristiqueJournees
                    .Include(j => j.GlobalQuota)
                    .Include(j => j.ClassQuotas)
                    .Where(j => j.IdSiteTouristiquePlanification == id)
                    .ToListAsync(cancellationToken);
                _context.SiteTouristiqueJournees.RemoveRange(journees);
            }

            _context.SiteTouristiquePlanifications.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private IQueryable<SiteTouristiquePlanification> DetailQuery() =>
            _context.SiteTouristiquePlanifications.AsNoTracking()
                .Include(p => p.Lieu)
                .Include(p => p.GlobalQuota)
                .Include(p => p.ClassQuotas)
                    .ThenInclude(q => q.Classe);

        private async Task ValidateRequestAsync(
            SiteTouristiqueCreatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var lieu = await _context.SiteTouristiques.AsNoTracking()
                .FirstOrDefaultAsync(l => l.IdSiteTouristique == request.IdSiteTouristique, cancellationToken);
            if (lieu == null)
                throw new ArgumentException($"Lieu touristique {request.IdSiteTouristique} introuvable.");
            if (lieu.IdSociete != idSociete)
                throw new ArgumentException($"Le lieu {request.IdSiteTouristique} n'appartient pas à la société {idSociete}.");

            NormalizeCodeDevise(request.CodeDevise);

            if (request.InventoryMode == SiteTouristiqueInventoryMode.ClassQuota)
            {
                var classeIds = (request.ClassQuotas ?? new List<SiteTouristiqueCreatePlanificationClassQuotaDto>())
                    .Select(q => q.IdSiteTouristiqueClasse)
                    .Distinct()
                    .ToList();

                var classes = await _context.SiteTouristiqueClasses.AsNoTracking()
                    .Where(c => classeIds.Contains(c.IdSiteTouristiqueClasse))
                    .Select(c => new { c.IdSiteTouristiqueClasse, c.IdSociete, c.Actif })
                    .ToListAsync(cancellationToken);

                if (classes.Count != classeIds.Count)
                    throw new ArgumentException("Une ou plusieurs classes sont introuvables.");

                if (classes.Any(c => c.IdSociete != idSociete))
                    throw new ArgumentException("Toutes les classes doivent appartenir à la société du template.");

                if (classes.Any(c => !c.Actif))
                    throw new ArgumentException("Une ou plusieurs classes référencées sont inactives.");
            }
        }

        private static void AttachQuotas(
            SiteTouristiquePlanification entity,
            SiteTouristiqueCreatePlanificationRequestDto request)
        {
            switch (request.InventoryMode)
            {
                case SiteTouristiqueInventoryMode.GlobalQuota:
                    entity.GlobalQuota = new SiteTouristiquePlanifGlobalQuota
                    {
                        CapaciteTotale = request.GlobalQuota!.CapaciteTotale,
                        PrixUnitaire = request.GlobalQuota.PrixUnitaire
                    };
                    break;

                case SiteTouristiqueInventoryMode.ClassQuota:
                    foreach (var q in request.ClassQuotas!)
                    {
                        entity.ClassQuotas.Add(new SiteTouristiquePlanifClassQuota
                        {
                            IdSiteTouristiqueClasse = q.IdSiteTouristiqueClasse,
                            CapaciteTotale = q.CapaciteTotale,
                            PrixUnitaire = q.PrixUnitaire
                        });
                    }
                    break;
            }
        }

        private static SiteTouristiquePlanificationListItemDto MapToListItem(
            SiteTouristiquePlanification entity,
            int nombreJournees) =>
            new()
            {
                IdSiteTouristiquePlanification = entity.IdSiteTouristiquePlanification,
                IdSociete = entity.IdSociete,
                IdSiteTouristique = entity.IdSiteTouristique,
                LieuNom = entity.Lieu?.Nom,
                Libelle = entity.Libelle,
                JoursSemaine = entity.JoursSemaine,
                InventoryMode = entity.InventoryMode,
                CodeDevise = entity.CodeDevise,
                Statut = entity.Statut,
                NombreJourneesGenerees = nombreJournees,
                DateCreation = entity.DateCreation,
                DateModification = entity.DateModification
            };

        private static SiteTouristiquePlanificationResponseDto MapToDetail(
            SiteTouristiquePlanification entity,
            int nombreJournees)
        {
            var dto = new SiteTouristiquePlanificationResponseDto
            {
                IdSiteTouristiquePlanification = entity.IdSiteTouristiquePlanification,
                IdSociete = entity.IdSociete,
                IdSiteTouristique = entity.IdSiteTouristique,
                LieuNom = entity.Lieu?.Nom,
                Libelle = entity.Libelle,
                JoursSemaine = entity.JoursSemaine,
                InventoryMode = entity.InventoryMode,
                CodeDevise = entity.CodeDevise,
                SalesOpenOffsetHours = entity.SalesOpenOffsetHours,
                SalesCloseOffsetHours = entity.SalesCloseOffsetHours,
                Statut = entity.Statut,
                NombreJourneesGenerees = nombreJournees,
                DateCreation = entity.DateCreation,
                DateModification = entity.DateModification
            };

            if (entity.GlobalQuota != null)
            {
                dto.GlobalQuota = new SiteTouristiquePlanificationGlobalQuotaResponseDto
                {
                    CapaciteTotale = entity.GlobalQuota.CapaciteTotale,
                    PrixUnitaire = entity.GlobalQuota.PrixUnitaire
                };
            }

            dto.ClassQuotas = (entity.ClassQuotas ?? Array.Empty<SiteTouristiquePlanifClassQuota>())
                .Select(q => new SiteTouristiquePlanificationClassQuotaResponseDto
                {
                    IdSiteTouristiquePlanifClassQuota = q.IdSiteTouristiquePlanifClassQuota,
                    IdSiteTouristiqueClasse = q.IdSiteTouristiqueClasse,
                    ClasseLibelle = q.Classe?.Libelle,
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
