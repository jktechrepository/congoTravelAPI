using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class TypeVehiculeService : ITypeVehiculeRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<TypeVehiculeService> _logger;

        public TypeVehiculeService(CongoTravelDbContext context, ILogger<TypeVehiculeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<TypeVehicule>> GetAllAsync()
        {
            try
            {
                return await _context.TypeVehicules
                    .AsNoTracking()
                    .OrderByDescending(t => t.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les types de bus");
                throw;
            }
        }

        public async Task<IReadOnlyList<TypeVehicule>> GetBySocieteAsync(int idSociete)
        {
            try
            {
                return await _context.TypeVehicules
                    .AsNoTracking()
                    .Where(t => t.IdSociete == idSociete)
                    .OrderBy(t => t.Libelle)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des types de bus pour la société {IdSociete}", idSociete);
                throw;
            }
        }

        public async Task<TypeVehicule?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.TypeVehicules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.IdTypeVehicule == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du type de bus {TypeVehiculeId}", id);
                throw;
            }
        }

        public async Task<TypeVehicule> CreateAsync(TypeVehicule typeVehicule)
        {
            try
            {
                var libelle = typeVehicule.Libelle.Trim();
                if (await ExistsByLibelleAsync(typeVehicule.IdSociete, libelle))
                {
                    throw new InvalidOperationException(
                        $"Un type de véhicule avec le libellé '{libelle}' existe déjà pour cette société.");
                }

                typeVehicule.Libelle = libelle;
                typeVehicule.DateCreation = DateTime.UtcNow;
                typeVehicule.DateModification = null;

                _context.TypeVehicules.Add(typeVehicule);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Type de bus créé avec succès - ID: {TypeVehiculeId}, Libellé: {Libelle}, Société: {IdSociete}",
                    typeVehicule.IdTypeVehicule, typeVehicule.Libelle, typeVehicule.IdSociete);

                return typeVehicule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du type de bus");
                throw;
            }
        }

        public async Task<TypeVehicule?> UpdateAsync(TypeVehicule typeVehicule)
        {
            try
            {
                var existingTypeVehicule = await _context.TypeVehicules.FindAsync(typeVehicule.IdTypeVehicule);
                if (existingTypeVehicule == null)
                    return null;

                var libelle = typeVehicule.Libelle.Trim();
                if (await ExistsByLibelleAsync(existingTypeVehicule.IdSociete, libelle, typeVehicule.IdTypeVehicule))
                {
                    throw new InvalidOperationException(
                        $"Un type de véhicule avec le libellé '{libelle}' existe déjà pour cette société.");
                }

                existingTypeVehicule.Libelle = libelle;
                existingTypeVehicule.Statut = typeVehicule.Statut;
                existingTypeVehicule.DateModification = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Type de bus mis à jour avec succès - ID: {TypeVehiculeId}", typeVehicule.IdTypeVehicule);

                return existingTypeVehicule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du type de bus {TypeVehiculeId}", typeVehicule.IdTypeVehicule);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var typeVehicule = await _context.TypeVehicules.FindAsync(id);
                if (typeVehicule == null)
                    return false;

                _context.TypeVehicules.Remove(typeVehicule);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Type de bus supprimé avec succès - ID: {TypeVehiculeId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du type de bus {TypeVehiculeId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<TypeVehicule>> GetByStatutAsync(bool statut)
        {
            try
            {
                return await _context.TypeVehicules
                    .AsNoTracking()
                    .Where(t => t.Statut == statut)
                    .OrderByDescending(t => t.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des types de bus avec statut {Statut}", statut);
                throw;
            }
        }

        public async Task<TypeVehicule?> GetByLibelleAsync(int idSociete, string libelle)
        {
            try
            {
                var normalized = libelle.Trim();
                return await _context.TypeVehicules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.IdSociete == idSociete && t.Libelle == normalized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche du type de bus par libellé {Libelle}", libelle);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.TypeVehicules.AnyAsync(t => t.IdTypeVehicule == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence du type de bus {TypeVehiculeId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByLibelleAsync(int idSociete, string libelle, int? excludeId = null)
        {
            try
            {
                var normalized = libelle.Trim();
                return await _context.TypeVehicules.AnyAsync(t =>
                    t.IdSociete == idSociete
                    && t.Libelle == normalized
                    && (!excludeId.HasValue || t.IdTypeVehicule != excludeId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence du libellé {Libelle}", libelle);
                throw;
            }
        }

        public async Task<PagedResult<TypeVehicule>> GetPagedAsync(PagedRequest request, int? idSociete = null)
        {
            try
            {
                var query = _context.TypeVehicules.AsNoTracking().AsQueryable();

                if (idSociete.HasValue)
                    query = query.Where(t => t.IdSociete == idSociete.Value);

                if (!string.IsNullOrEmpty(request.SearchTerm))
                    query = query.Where(t => t.Libelle.Contains(request.SearchTerm));

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = request.SortBy.ToLower() switch
                    {
                        "libelle" => request.SortDescending
                            ? query.OrderByDescending(t => t.Libelle)
                            : query.OrderBy(t => t.Libelle),
                        "statut" => request.SortDescending
                            ? query.OrderByDescending(t => t.Statut)
                            : query.OrderBy(t => t.Statut),
                        _ => query.OrderByDescending(t => t.DateCreation)
                    };
                }
                else
                {
                    query = query.OrderByDescending(t => t.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<TypeVehicule>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des types de bus");
                throw;
            }
        }

        public async Task<int> CountAsync()
        {
            try
            {
                return await _context.TypeVehicules.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des types de bus");
                throw;
            }
        }

        public async Task<int> CountByStatutAsync(bool statut)
        {
            try
            {
                return await _context.TypeVehicules.CountAsync(t => t.Statut == statut);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des types de bus avec statut {Statut}", statut);
                throw;
            }
        }
    }
}
