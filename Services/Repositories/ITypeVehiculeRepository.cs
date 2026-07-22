using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    public interface ITypeVehiculeRepository
    {
        Task<IEnumerable<TypeVehicule>> GetAllAsync();
        Task<IReadOnlyList<TypeVehicule>> GetBySocieteAsync(int idSociete);
        Task<TypeVehicule?> GetByIdAsync(int id);
        Task<TypeVehicule> CreateAsync(TypeVehicule typeVehicule);
        Task<TypeVehicule?> UpdateAsync(TypeVehicule typeVehicule);
        Task<bool> DeleteAsync(int id);

        Task<IEnumerable<TypeVehicule>> GetByStatutAsync(bool statut);
        Task<TypeVehicule?> GetByLibelleAsync(int idSociete, string libelle);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByLibelleAsync(int idSociete, string libelle, int? excludeId = null);
        Task<PagedResult<TypeVehicule>> GetPagedAsync(PagedRequest request, int? idSociete = null);
        Task<int> CountAsync();
        Task<int> CountByStatutAsync(bool statut);
    }
}
