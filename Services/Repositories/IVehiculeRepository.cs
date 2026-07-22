using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    public interface IVehiculeRepository
    {
        Task<IEnumerable<Vehicule>> GetAllAsync();
        Task<Vehicule?> GetByIdAsync(int id);
        Task<Vehicule> CreateAsync(Vehicule vehicule);
        Task<Vehicule?> UpdateAsync(Vehicule vehicule);
        Task<bool> DeleteAsync(int id);

        Task<IEnumerable<Vehicule>> GetBySocieteAsync(int idSociete);
        Task<IEnumerable<Vehicule>> GetByTypeVehiculeAsync(int idTypeVehicule);
        Task<IEnumerable<Vehicule>> GetBySocieteAndTypeAsync(int idSociete, int idTypeVehicule);
        Task<Vehicule?> GetByAliasVehiculeAsync(string aliasVehicule, int idSociete);

        Task<IEnumerable<Vehicule>> GetByStatutAsync(bool statut);
        Task<IEnumerable<Vehicule>> GetByMarqueAsync(string marque);

        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByAliasVehiculeAsync(string aliasVehicule, int idSociete);

        Task<PagedResult<Vehicule>> GetPagedAsync(PagedRequest request);
        Task<PagedResult<Vehicule>> GetBySocietePagedAsync(int idSociete, PagedRequest request);

        Task<int> CountAsync();
        Task<int> CountBySocieteAsync(int idSociete);
        Task<int> CountByTypeVehiculeAsync(int idTypeVehicule);
    }
}
