using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le repository de gestion des plaintes clients
    /// </summary>
    public interface IPlainteClientRepository
    {
        Task<IEnumerable<PlainteClient>> GetAllAsync();
        Task<PagedResult<PlainteClient>> GetPagedAsync(PagedRequest request, string? statut = null, string? priorite = null, int? idAgent = null, int? idClient = null);
        Task<PlainteClient?> GetByIdAsync(int id);
        Task<IEnumerable<PlainteClient>> GetByClientAsync(int idClient);
        Task<IEnumerable<PlainteClient>> GetEnAttenteAsync();
        Task<IEnumerable<PlainteClient>> GetByAgentAsync(int idAgent);
        Task<PlainteClient> CreateAsync(PlainteClient plainte);
        Task<PlainteClient> UpdateAsync(PlainteClient plainte);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}

