using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class DestinationService : IDestinationRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<DestinationService> _logger;

        public DestinationService(
            CongoTravelDbContext context,
            ILogger<DestinationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Destination>> GetAllAsync()
        {
            try
            {
                return await _context.Destinations
                    .Include(d => d.Societe)
                    .Where(d => d.Statut == true)
                    .OrderByDescending(d => d.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de toutes les destinations");
                throw;
            }
        }

        public async Task<IEnumerable<Destination>> GetBySocieteAsync(int idSociete)
        {
            try
            {
                return await _context.Destinations
                    .Include(d => d.Societe)
                    .Where(d => d.IdSociete == idSociete && d.Statut == true)
                    .OrderByDescending(d => d.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des destinations de la société {IdSociete}", idSociete);
                throw;
            }
        }

        public async Task<PagedResult<Destination>> GetBySocietePagedAsync(int idSociete, PagedRequest request)
        {
            try
            {
                var query = _context.Destinations
                    .Include(d => d.Societe)
                    .Where(d => d.IdSociete == idSociete && d.Statut == true);

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(d =>
                        d.VilleDepart.Contains(request.SearchTerm) ||
                        d.VilleArrivee.Contains(request.SearchTerm));
                }

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(d => d.DateCreation)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Destination>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des destinations de la société {IdSociete}", idSociete);
                throw;
            }
        }

        public async Task<Destination> GetByIdAsync(int id)
        {
            try
            {
                var destination = await _context.Destinations
                    .Include(d => d.Societe)
                    .FirstOrDefaultAsync(d => d.IdDestination == id);

                if (destination == null)
                {
                    _logger.LogWarning("Destination avec l'ID {Id} non trouvée", id);
                }

                return destination;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la destination {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Destination>> GetByVillesAsync(int idSociete, string villeDepart, string villeArrivee)
        {
            try
            {
                var depart = NormalizeVille(villeDepart);
                var arrivee = NormalizeVille(villeArrivee);

                return await _context.Destinations
                    .Include(d => d.Societe)
                    .Where(d => d.IdSociete == idSociete
                        && d.VilleDepart.ToLower() == depart
                        && d.VilleArrivee.ToLower() == arrivee
                        && d.Statut == true)
                    .OrderByDescending(d => d.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche des destinations entre {VilleDepart} et {VilleArrivee}", villeDepart, villeArrivee);
                throw;
            }
        }

        public async Task<Destination> CreateAsync(Destination destination)
        {
            try
            {
                destination.VilleDepart = destination.VilleDepart.Trim();
                destination.VilleArrivee = destination.VilleArrivee.Trim();

                if (await ExistsByVillesAsync(destination.IdSociete, destination.VilleDepart, destination.VilleArrivee))
                {
                    throw new InvalidOperationException(
                        $"Une destination entre {destination.VilleDepart} et {destination.VilleArrivee} existe déjà pour cette société.");
                }

                _logger.LogInformation("Création d'une nouvelle destination: {VilleDepart} -> {VilleArrivee}",
                    destination.VilleDepart, destination.VilleArrivee);

                destination.DateCreation = DateTime.UtcNow;
                destination.DateModification = null;

                _context.Destinations.Add(destination);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Destination créée avec succès avec l'ID {Id}", destination.IdDestination);
                return destination;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la destination");
                throw;
            }
        }

        public async Task<Destination> UpdateAsync(Destination destination)
        {
            try
            {
                var existing = await _context.Destinations.FindAsync(destination.IdDestination);
                if (existing == null)
                {
                    _logger.LogWarning("Destination {Id} introuvable pour mise à jour", destination.IdDestination);
                    return destination;
                }

                destination.VilleDepart = destination.VilleDepart.Trim();
                destination.VilleArrivee = destination.VilleArrivee.Trim();

                if (await ExistsByVillesAsync(existing.IdSociete, destination.VilleDepart, destination.VilleArrivee, destination.IdDestination))
                {
                    throw new InvalidOperationException(
                        $"Une destination entre {destination.VilleDepart} et {destination.VilleArrivee} existe déjà pour cette société.");
                }

                _logger.LogInformation("Mise à jour de la destination {Id}: {VilleDepart} -> {VilleArrivee}",
                    destination.IdDestination, destination.VilleDepart, destination.VilleArrivee);

                existing.VilleDepart = destination.VilleDepart;
                existing.VilleArrivee = destination.VilleArrivee;
                existing.Montant = destination.Montant;
                existing.Statut = destination.Statut;
                existing.JourDepart = destination.JourDepart;
                existing.HeureDepart = destination.HeureDepart;
                existing.DateModification = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Destination {Id} mise à jour avec succès", destination.IdDestination);
                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la destination {Id}", destination.IdDestination);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var destination = await _context.Destinations.FindAsync(id);
                if (destination == null)
                {
                    _logger.LogWarning("Tentative de suppression d'une destination inexistante avec l'ID {Id}", id);
                    return false;
                }

                _logger.LogInformation("Suppression de la destination {Id}: {VilleDepart} -> {VilleArrivee}",
                    id, destination.VilleDepart, destination.VilleArrivee);

                _context.Destinations.Remove(destination);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Destination {Id} supprimée avec succès", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la destination {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.Destinations.AnyAsync(d => d.IdDestination == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de l'existence de la destination {Id}", id);
                throw;
            }
        }

        public async Task<bool> ToggleStatutAsync(int id)
        {
            try
            {
                var destination = await _context.Destinations.FindAsync(id);
                if (destination == null)
                {
                    _logger.LogWarning("Tentative de basculement de statut d'une destination inexistante avec l'ID {Id}", id);
                    return false;
                }

                destination.Statut = !destination.Statut;
                destination.DateModification = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Statut de la destination {Id} basculé vers {Statut}", id, destination.Statut);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du basculement du statut de la destination {Id}", id);
                throw;
            }
        }

        public async Task<bool> SetStatutAsync(int id, bool statut)
        {
            try
            {
                var destination = await _context.Destinations.FindAsync(id);
                if (destination == null)
                {
                    _logger.LogWarning("Tentative de modification de statut d'une destination inexistante avec l'ID {Id}", id);
                    return false;
                }

                destination.Statut = statut;
                destination.DateModification = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Statut de la destination {Id} défini sur {Statut}", id, statut);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du statut de la destination {Id}", id);
                throw;
            }
        }

        public async Task<PagedResult<Destination>> GetPagedAsync(PagedRequest request, int? idSociete = null)
        {
            try
            {
                var query = _context.Destinations
                    .Include(d => d.Societe)
                    .Where(d => d.Statut == true);

                if (idSociete.HasValue)
                    query = query.Where(d => d.IdSociete == idSociete.Value);

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(d =>
                        d.VilleDepart.Contains(request.SearchTerm) ||
                        d.VilleArrivee.Contains(request.SearchTerm) ||
                        (d.Societe != null && d.Societe.Nom.Contains(request.SearchTerm)));
                }

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(d => d.DateCreation)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Destination>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des destinations");
                throw;
            }
        }

        public async Task<bool> ExistsByVillesAsync(int idSociete, string villeDepart, string villeArrivee, int? excludeId = null)
        {
            try
            {
                var depart = NormalizeVille(villeDepart);
                var arrivee = NormalizeVille(villeArrivee);

                var query = _context.Destinations
                    .Where(d => d.IdSociete == idSociete
                        && d.VilleDepart.ToLower() == depart
                        && d.VilleArrivee.ToLower() == arrivee);

                if (excludeId.HasValue)
                    query = query.Where(d => d.IdDestination != excludeId.Value);

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de l'existence d'une destination entre {VilleDepart} et {VilleArrivee}",
                    villeDepart, villeArrivee);
                throw;
            }
        }

        private static string NormalizeVille(string ville) => ville.Trim().ToLowerInvariant();
    }
}
