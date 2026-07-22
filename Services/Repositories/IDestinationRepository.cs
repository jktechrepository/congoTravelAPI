using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    public interface IDestinationRepository
    {
        Task<IEnumerable<Destination>> GetAllAsync();
        Task<IEnumerable<Destination>> GetBySocieteAsync(int idSociete);
        Task<PagedResult<Destination>> GetBySocietePagedAsync(int idSociete, PagedRequest request);
        Task<Destination> GetByIdAsync(int id);
        Task<IEnumerable<Destination>> GetByVillesAsync(int idSociete, string villeDepart, string villeArrivee);
        Task<Destination> CreateAsync(Destination destination);
        Task<Destination> UpdateAsync(Destination destination);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ToggleStatutAsync(int id);
        Task<bool> SetStatutAsync(int id, bool statut);
        Task<PagedResult<Destination>> GetPagedAsync(PagedRequest request, int? idSociete = null);
        Task<bool> ExistsByVillesAsync(int idSociete, string villeDepart, string villeArrivee, int? excludeId = null);
    }
}
