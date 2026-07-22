using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.PlanificationVoyage;
using CongoTravel.Models.DTOs.VoyageTarification;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class PlanificationVoyageService : IPlanificationVoyageService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<PlanificationVoyageService> _logger;

        public PlanificationVoyageService(
            CongoTravelDbContext context,
            ILogger<PlanificationVoyageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<PlanificationVoyageResponseDto>> GetBySocieteAsync(
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var items = await _context.PlanificationsVoyage.AsNoTracking()
                .Include(p => p.Etapes)
                .Include(p => p.Tarifs)
                .Where(p => p.IdSociete == idSociete)
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync(cancellationToken);

            var voyageCounts = await _context.Voyages.AsNoTracking()
                .Where(v => v.IdPlanificationVoyage.HasValue)
                .GroupBy(v => v.IdPlanificationVoyage!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

            return items.Select(p => MapToDto(p, voyageCounts.GetValueOrDefault(p.IdPlanificationVoyage))).ToList();
        }

        public async Task<PlanificationVoyageResponseDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await LoadWithNavigationsAsync(id, cancellationToken);
            if (entity == null)
                return null;

            var count = await _context.Voyages.AsNoTracking()
                .CountAsync(v => v.IdPlanificationVoyage == id, cancellationToken);

            return MapToDto(entity, count);
        }

        public async Task<PlanificationVoyageResponseDto> CreateAsync(
            CreatePlanificationVoyageDto dto,
            CancellationToken cancellationToken = default)
        {
            await ValidateReferencesAsync(dto, cancellationToken);

            var etapes = ResolveEtapes(dto);
            var entity = new PlanificationVoyage
            {
                Libelle = dto.Libelle.Trim(),
                IdSociete = dto.IdSociete,
                IdSite = dto.IdSite,
                IdVehicule = dto.IdVehicule,
                HeureDepart = dto.HeureDepart,
                Prix = dto.Prix,
                CodeDevisePrix = dto.CodeDevisePrix.ToUpperInvariant(),
                JoursSemaine = dto.JoursSemaine.Distinct().OrderBy(j => j).ToList(),
                Statut = dto.Statut,
                DateCreation = DateTime.UtcNow
            };

            _context.PlanificationsVoyage.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            await PersistEtapesAsync(entity.IdPlanificationVoyage, dto.IdSociete, etapes, cancellationToken);
            await PersistTarifsAsync(entity.IdPlanificationVoyage, dto.IdSociete, dto.Tarifs, cancellationToken);

            _logger.LogInformation("Planification voyage créée {Id} société {SocieteId}", entity.IdPlanificationVoyage, dto.IdSociete);

            return (await GetByIdAsync(entity.IdPlanificationVoyage, cancellationToken))!;
        }

        public async Task<PlanificationVoyageResponseDto?> UpdateAsync(
            UpdatePlanificationVoyageDto dto,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.PlanificationsVoyage
                .Include(p => p.Etapes)
                .Include(p => p.Tarifs)
                .FirstOrDefaultAsync(p => p.IdPlanificationVoyage == dto.IdPlanificationVoyage, cancellationToken);

            if (entity == null)
                return null;

            await ValidateReferencesAsync(dto, cancellationToken);

            var etapes = ResolveEtapes(dto);

            entity.Libelle = dto.Libelle.Trim();
            entity.IdSociete = dto.IdSociete;
            entity.IdSite = dto.IdSite;
            entity.IdVehicule = dto.IdVehicule;
            entity.HeureDepart = dto.HeureDepart;
            entity.Prix = dto.Prix;
            entity.CodeDevisePrix = dto.CodeDevisePrix.ToUpperInvariant();
            entity.JoursSemaine = dto.JoursSemaine.Distinct().OrderBy(j => j).ToList();
            entity.Statut = dto.Statut;
            entity.DateModification = DateTime.UtcNow;

            _context.PlanificationVoyageEtapes.RemoveRange(entity.Etapes ?? Array.Empty<PlanificationVoyageEtape>());
            _context.PlanificationVoyageTarifs.RemoveRange(entity.Tarifs ?? Array.Empty<PlanificationVoyageTarif>());

            await _context.SaveChangesAsync(cancellationToken);

            await PersistEtapesAsync(entity.IdPlanificationVoyage, dto.IdSociete, etapes, cancellationToken);
            await PersistTarifsAsync(entity.IdPlanificationVoyage, dto.IdSociete, dto.Tarifs, cancellationToken);

            return await GetByIdAsync(entity.IdPlanificationVoyage, cancellationToken);
        }

        public async Task<bool> ToggleStatutAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.PlanificationsVoyage.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null)
                return false;

            entity.Statut = !entity.Statut;
            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.PlanificationsVoyage.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null)
                return false;

            var hasLinkedVoyages = await _context.Voyages.AnyAsync(v => v.IdPlanificationVoyage == id, cancellationToken);
            if (hasLinkedVoyages)
            {
                var voyageIds = await _context.Voyages.AsNoTracking()
                    .Where(v => v.IdPlanificationVoyage == id)
                    .Select(v => v.Id)
                    .ToListAsync(cancellationToken);

                var hasReservations = await _context.Reservations.AnyAsync(
                    r => voyageIds.Contains(r.IdVoyage),
                    cancellationToken);

                if (hasReservations)
                    throw new InvalidOperationException(
                        "Impossible de supprimer : des voyages générés ont des réservations.");
            }

            if (hasLinkedVoyages)
            {
                entity.Statut = false;
                entity.DateModification = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }

            _context.PlanificationsVoyage.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<PlanificationVoyage?> LoadWithNavigationsAsync(int id, CancellationToken cancellationToken) =>
            await _context.PlanificationsVoyage.AsNoTracking()
                .Include(p => p.Etapes)
                .Include(p => p.Tarifs)
                .FirstOrDefaultAsync(p => p.IdPlanificationVoyage == id, cancellationToken);

        private static PlanificationVoyageResponseDto MapToDto(PlanificationVoyage entity, int nombreVoyagesGeneres)
        {
            var etapes = (entity.Etapes ?? Array.Empty<PlanificationVoyageEtape>())
                .OrderBy(e => e.Ordre)
                .Select(e => new PlanificationVoyageEtapeDto { Ordre = e.Ordre, IdDestination = e.IdDestination })
                .ToList();

            return new PlanificationVoyageResponseDto
            {
                IdPlanificationVoyage = entity.IdPlanificationVoyage,
                Libelle = entity.Libelle,
                IdSociete = entity.IdSociete,
                IdSite = entity.IdSite,
                IdVehicule = entity.IdVehicule,
                HeureDepart = entity.HeureDepart,
                Prix = entity.Prix,
                CodeDevisePrix = entity.CodeDevisePrix,
                JoursSemaine = entity.JoursSemaine,
                Statut = entity.Statut,
                IdDestination = etapes.FirstOrDefault()?.IdDestination,
                EtapesDestinations = etapes,
                Tarifs = (entity.Tarifs ?? Array.Empty<PlanificationVoyageTarif>())
                    .Select(t => new VoyageTarifCategorieSiegeItemDto { IdCategorieSiege = t.IdCategorieSiege, Prix = t.Prix })
                    .ToList(),
                NombreVoyagesGeneres = nombreVoyagesGeneres,
                DateCreation = entity.DateCreation,
                DateModification = entity.DateModification
            };
        }

        private static List<PlanificationVoyageEtapeDto> ResolveEtapes(CreatePlanificationVoyageDto dto)
        {
            var etapes = dto.EtapesDestinations?.Where(e => e != null).OrderBy(e => e.Ordre).ToList();
            if (etapes is { Count: > 0 })
                return etapes;

            return new List<PlanificationVoyageEtapeDto>
            {
                new() { Ordre = 1, IdDestination = dto.IdDestination!.Value }
            };
        }

        private async Task ValidateReferencesAsync(CreatePlanificationVoyageDto dto, CancellationToken cancellationToken)
        {
            var societeOk = await _context.Societes.AnyAsync(s => s.IdSociete == dto.IdSociete && s.Statut == true, cancellationToken);
            if (!societeOk)
                throw new ArgumentException($"Société {dto.IdSociete} introuvable ou inactive.");

            var vehicule = await _context.Vehicules.AsNoTracking()
                .FirstOrDefaultAsync(v => v.IdVehicule == dto.IdVehicule, cancellationToken);
            if (vehicule == null)
                throw new ArgumentException($"Véhicule {dto.IdVehicule} introuvable.");
            if (vehicule.IdSociete != dto.IdSociete)
                throw new ArgumentException($"Véhicule {dto.IdVehicule} n'appartient pas à la société {dto.IdSociete}.");

            var site = await _context.Sites.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSite == dto.IdSite, cancellationToken);
            if (site == null)
                throw new ArgumentException($"Site {dto.IdSite} introuvable.");
            if (site.IdSociete != dto.IdSociete)
                throw new ArgumentException($"Site {dto.IdSite} n'appartient pas à la société {dto.IdSociete}.");

            var etapes = ResolveEtapes(dto);
            var destinationIds = etapes.Select(e => e.IdDestination).Distinct().ToList();
            var destinations = await _context.Destinations.AsNoTracking()
                .Where(d => destinationIds.Contains(d.IdDestination))
                .Select(d => new { d.IdDestination, d.IdSociete })
                .ToListAsync(cancellationToken);

            if (destinations.Count != destinationIds.Count)
                throw new ArgumentException("Une ou plusieurs destinations sont introuvables.");

            if (destinations.Any(d => d.IdSociete != dto.IdSociete))
                throw new ArgumentException("Toutes les destinations doivent appartenir à la société du template.");
        }

        private async Task PersistEtapesAsync(
            int idPlanification,
            int idSociete,
            IReadOnlyList<PlanificationVoyageEtapeDto> etapes,
            CancellationToken cancellationToken)
        {
            foreach (var etape in etapes)
            {
                _context.PlanificationVoyageEtapes.Add(new PlanificationVoyageEtape
                {
                    IdPlanificationVoyage = idPlanification,
                    IdDestination = etape.IdDestination,
                    Ordre = etape.Ordre,
                    IdSociete = idSociete,
                    DateCreation = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task PersistTarifsAsync(
            int idPlanification,
            int idSociete,
            List<VoyageTarifCategorieSiegeItemDto>? tarifs,
            CancellationToken cancellationToken)
        {
            if (tarifs == null || tarifs.Count == 0)
                return;

            foreach (var tarif in tarifs)
            {
                _context.PlanificationVoyageTarifs.Add(new PlanificationVoyageTarif
                {
                    IdPlanificationVoyage = idPlanification,
                    IdCategorieSiege = tarif.IdCategorieSiege,
                    Prix = tarif.Prix,
                    IdSociete = idSociete,
                    DateCreation = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
