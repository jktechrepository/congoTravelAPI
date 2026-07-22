using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class VehiculeService : IVehiculeRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<VehiculeService> _logger;
        private readonly ISiegeService _siegeService;

        public VehiculeService(CongoTravelDbContext context, ILogger<VehiculeService> logger, ISiegeService siegeService)
        {
            _context = context;
            _logger = logger;
            _siegeService = siegeService;
        }

        private static IQueryable<Vehicule> WithDetails(IQueryable<Vehicule> query) =>
            query
                .Include(vh => vh.Societe)
                .Include(vh => vh.TypeVehicule)
                .Include(vh => vh.Photos);

        // CRUD de base
        public async Task<IEnumerable<Vehicule>> GetAllAsync()
        {
            try
            {
                return await WithDetails(_context.Vehicules)
                    .OrderByDescending(vh => vh.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les véhicules");
                throw;
            }
        }

        public async Task<Vehicule?> GetByIdAsync(int id)
        {
            try
            {
                return await WithDetails(_context.Vehicules)
                    .FirstOrDefaultAsync(vh => vh.IdVehicule == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du véhicule {VehiculeId}", id);
                throw;
            }
        }

        public async Task<Vehicule> CreateAsync(Vehicule vehicule)
        {
            try
            {
                // ✅ VALIDATION ID SOCIETE: Vérifier que l'IdSociete est valide
                if (vehicule.IdSociete <= 0)
                {
                    throw new ArgumentException("L'IdSociete est obligatoire et doit être supérieur à 0");
                }

                if (string.IsNullOrWhiteSpace(vehicule.AliasVehicule))
                {
                    throw new ArgumentException("L'alias du véhicule est obligatoire");
                }

                vehicule.AliasVehicule = vehicule.AliasVehicule.Trim();

                // ✅ VALIDATION EXISTENCE SOCIETE: Vérifier que la société existe
                var societeExists = await _context.Societes.AnyAsync(s => s.IdSociete == vehicule.IdSociete);
                if (!societeExists)
                {
                    throw new ArgumentException($"La société avec l'ID {vehicule.IdSociete} n'existe pas");
                }

                // ✅ VALIDATION NUMERO BUS UNIQUE: Vérifier que le alias du véhicule n'existe pas déjà pour cette société
                var aliasExists = await ExistsByAliasVehiculeAsync(vehicule.AliasVehicule, vehicule.IdSociete);
                if (aliasExists)
                {
                    throw new InvalidOperationException($"Un véhicule avec le alias '{vehicule.AliasVehicule}' existe déjà pour cette société");
                }

                // ✅ VALIDATION TYPE BUS: Vérifier que le type de véhicule existe
                var typeVehiculeExists = await _context.TypeVehicules.AnyAsync(t => t.IdTypeVehicule == vehicule.IdTypeVehicule);
                if (!typeVehiculeExists)
                {
                    throw new ArgumentException($"Le type de véhicule avec l'ID {vehicule.IdTypeVehicule} n'existe pas");
                }

                // ✅ VALIDATION NOMBRE SIEGE: Vérifier que le nombre de sièges est positif
                if (vehicule.NombreSiege <= 0)
                {
                    throw new ArgumentException("Le nombre de sièges doit être supérieur à 0");
                }

                vehicule.DateCreation = DateTime.Now;
                _context.Vehicules.Add(vehicule);
                await _context.SaveChangesAsync();

                await _siegeService.EnsureSeatsForVehiculeAsync(vehicule.IdVehicule);

                _logger.LogInformation("Vehicule créé avec succès - ID: {VehiculeId}, Alias: {AliasVehicule}, Société: {SocieteId}", 
                    vehicule.IdVehicule, vehicule.AliasVehicule, vehicule.IdSociete);

                return vehicule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du véhicule");
                throw;
            }
        }

        public async Task<Vehicule?> UpdateAsync(Vehicule vehicule)
        {
            try
            {
                var existingVehicule = await _context.Vehicules.FindAsync(vehicule.IdVehicule);
                if (existingVehicule == null)
                    return null;

                // ✅ VALIDATION ID SOCIETE: Vérifier que l'IdSociete est valide
                if (vehicule.IdSociete <= 0)
                {
                    throw new ArgumentException("L'IdSociete est obligatoire et doit être supérieur à 0");
                }

                if (string.IsNullOrWhiteSpace(vehicule.AliasVehicule))
                {
                    throw new ArgumentException("L'alias du véhicule est obligatoire");
                }

                vehicule.AliasVehicule = vehicule.AliasVehicule.Trim();

                // ✅ VALIDATION EXISTENCE SOCIETE: Vérifier que la société existe
                var societeExists = await _context.Societes.AnyAsync(s => s.IdSociete == vehicule.IdSociete);
                if (!societeExists)
                {
                    throw new ArgumentException($"La société avec l'ID {vehicule.IdSociete} n'existe pas");
                }

                // ✅ VALIDATION NUMERO BUS UNIQUE: Vérifier que le alias du véhicule n'existe pas déjà pour cette société (sauf pour le même vehicule)
                var aliasTaken = await _context.Vehicules
                    .AnyAsync(vh => vh.AliasVehicule == vehicule.AliasVehicule && vh.IdSociete == vehicule.IdSociete && vh.IdVehicule != vehicule.IdVehicule);
                
                if (aliasTaken)
                {
                    throw new InvalidOperationException($"Un véhicule avec le alias '{vehicule.AliasVehicule}' existe déjà pour cette société");
                }

                // ✅ VALIDATION TYPE BUS: Vérifier que le type de véhicule existe
                var typeVehiculeExists = await _context.TypeVehicules.AnyAsync(t => t.IdTypeVehicule == vehicule.IdTypeVehicule);
                if (!typeVehiculeExists)
                {
                    throw new ArgumentException($"Le type de véhicule avec l'ID {vehicule.IdTypeVehicule} n'existe pas");
                }

                // ✅ VALIDATION NOMBRE SIEGE: Vérifier que le nombre de sièges est positif
                if (vehicule.NombreSiege <= 0)
                {
                    throw new ArgumentException("Le nombre de sièges doit être supérieur à 0");
                }

                existingVehicule.Marques = vehicule.Marques;
                existingVehicule.AliasVehicule = vehicule.AliasVehicule;
                existingVehicule.IdTypeVehicule = vehicule.IdTypeVehicule;
                existingVehicule.NombreSiege = vehicule.NombreSiege;
                existingVehicule.IdSociete = vehicule.IdSociete;
                existingVehicule.NumeroDePlaque = vehicule.NumeroDePlaque;
                existingVehicule.Statut = vehicule.Statut;
                existingVehicule.DateModification = DateTime.Now;

                await _context.SaveChangesAsync();

                await _siegeService.EnsureSeatsForVehiculeAsync(existingVehicule.IdVehicule);

                _logger.LogInformation("Vehicule mis à jour avec succès - ID: {VehiculeId}", vehicule.IdVehicule);

                return await WithDetails(_context.Vehicules)
                    .FirstOrDefaultAsync(vh => vh.IdVehicule == existingVehicule.IdVehicule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du véhicule {VehiculeId}", vehicule.IdVehicule);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var vehicule = await _context.Vehicules.FindAsync(id);
                if (vehicule == null)
                    return false;

                _context.Vehicules.Remove(vehicule);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Vehicule supprimé avec succès - ID: {VehiculeId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du véhicule {VehiculeId}", id);
                throw;
            }
        }

        // Méthodes de recherche
        public async Task<IEnumerable<Vehicule>> GetBySocieteAsync(int idSociete)
        {
            try
            {
                return await WithDetails(_context.Vehicules)
                    .Where(vh => vh.IdSociete == idSociete)
                    .OrderBy(vh => vh.AliasVehicule)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des véhicules pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        public async Task<IEnumerable<Vehicule>> GetByTypeVehiculeAsync(int idTypeVehicule)
        {
            try
            {
                return await WithDetails(_context.Vehicules)
                    .Where(vh => vh.IdTypeVehicule == idTypeVehicule)
                    .OrderByDescending(vh => vh.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des véhicules de type {TypeVehicule}", idTypeVehicule);
                throw;
            }
        }

        public async Task<IEnumerable<Vehicule>> GetBySocieteAndTypeAsync(int idSociete, int idTypeVehicule)
        {
            try
            {
                return await WithDetails(_context.Vehicules)
                    .Where(vh => vh.IdSociete == idSociete && vh.IdTypeVehicule == idTypeVehicule)
                    .OrderBy(vh => vh.AliasVehicule)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des véhicules pour la société {SocieteId} et type {TypeVehicule}", idSociete, idTypeVehicule);
                throw;
            }
        }

        public async Task<Vehicule?> GetByAliasVehiculeAsync(string aliasVehicule, int idSociete)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(aliasVehicule))
                    return null;

                var trimmed = aliasVehicule.Trim();

                return await WithDetails(_context.Vehicules)
                    .FirstOrDefaultAsync(vh => vh.AliasVehicule == trimmed && vh.IdSociete == idSociete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche du véhicule alias {AliasVehicule} pour la société {SocieteId}", aliasVehicule, idSociete);
                throw;
            }
        }

        // Méthodes de filtrage
        public async Task<IEnumerable<Vehicule>> GetByStatutAsync(bool statut)
        {
            try
            {
                return await WithDetails(_context.Vehicules)
                    .Where(vh => vh.Statut == statut)
                    .OrderByDescending(vh => vh.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des véhicules avec statut {Statut}", statut);
                throw;
            }
        }

        public async Task<IEnumerable<Vehicule>> GetByMarqueAsync(string marque)
        {
            try
            {
                return await WithDetails(_context.Vehicules)
                    .Where(vh => vh.Marques != null && vh.Marques.Contains(marque))
                    .OrderByDescending(vh => vh.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des véhicules de marque {Marque}", marque);
                throw;
            }
        }

        // Méthodes d'existence
        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.Vehicules.AnyAsync(vh => vh.IdVehicule == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence du véhicule {VehiculeId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByAliasVehiculeAsync(string aliasVehicule, int idSociete)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(aliasVehicule))
                    return false;

                var trimmed = aliasVehicule.Trim();

                return await _context.Vehicules.AnyAsync(vh => vh.AliasVehicule == trimmed && vh.IdSociete == idSociete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence du véhicule alias {AliasVehicule} pour la société {SocieteId}", aliasVehicule, idSociete);
                throw;
            }
        }

        // Pagination
        public async Task<PagedResult<Vehicule>> GetPagedAsync(PagedRequest request)
        {
            try
            {
                var baseQuery = _context.Vehicules.AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    baseQuery = baseQuery.Where(vh =>
                        (vh.Marques != null && vh.Marques.Contains(request.SearchTerm)) ||
                        vh.AliasVehicule.Contains(request.SearchTerm) ||
                        vh.IdTypeVehicule.ToString().Contains(request.SearchTerm));
                }

                var totalCount = await baseQuery.CountAsync();
                var query = WithDetails(baseQuery);

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "alias":
                            query = request.SortDescending 
                                ? query.OrderByDescending(vh => vh.AliasVehicule)
                                : query.OrderBy(vh => vh.AliasVehicule);
                            break;
                        case "marque":
                            query = request.SortDescending 
                                ? query.OrderByDescending(vh => vh.Marques)
                                : query.OrderBy(vh => vh.Marques);
                            break;
                        case "type":
                            query = request.SortDescending 
                                ? query.OrderByDescending(vh => vh.IdTypeVehicule)
                                : query.OrderBy(vh => vh.IdTypeVehicule);
                            break;
                        default:
                            query = query.OrderByDescending(vh => vh.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(vh => vh.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Vehicule>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des véhicules");
                throw;
            }
        }

        public async Task<PagedResult<Vehicule>> GetBySocietePagedAsync(int idSociete, PagedRequest request)
        {
            try
            {
                var baseQuery = _context.Vehicules
                    .Where(vh => vh.IdSociete == idSociete);

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    baseQuery = baseQuery.Where(vh =>
                        (vh.Marques != null && vh.Marques.Contains(request.SearchTerm)) ||
                        vh.AliasVehicule.Contains(request.SearchTerm) ||
                        vh.IdTypeVehicule.ToString().Contains(request.SearchTerm));
                }

                var totalCount = await baseQuery.CountAsync();
                var query = WithDetails(baseQuery);

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "alias":
                            query = request.SortDescending 
                                ? query.OrderByDescending(vh => vh.AliasVehicule)
                                : query.OrderBy(vh => vh.AliasVehicule);
                            break;
                        case "marque":
                            query = request.SortDescending 
                                ? query.OrderByDescending(vh => vh.Marques)
                                : query.OrderBy(vh => vh.Marques);
                            break;
                        case "type":
                            query = request.SortDescending 
                                ? query.OrderByDescending(vh => vh.IdTypeVehicule)
                                : query.OrderBy(vh => vh.IdTypeVehicule);
                            break;
                        default:
                            query = query.OrderBy(vh => vh.AliasVehicule);
                            break;
                    }
                }
                else
                {
                    query = query.OrderBy(vh => vh.AliasVehicule);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Vehicule>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des véhicules pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        // Compteurs
        public async Task<int> CountAsync()
        {
            try
            {
                return await _context.Vehicules.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des véhicules");
                throw;
            }
        }

        public async Task<int> CountBySocieteAsync(int idSociete)
        {
            try
            {
                return await _context.Vehicules.CountAsync(vh => vh.IdSociete == idSociete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des véhicules pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        public async Task<int> CountByTypeVehiculeAsync(int idTypeVehicule)
        {
            try
            {
                return await _context.Vehicules.CountAsync(vh => vh.IdTypeVehicule == idTypeVehicule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des véhicules de type {TypeVehicule}", idTypeVehicule);
                throw;
            }
        }
    }
}
