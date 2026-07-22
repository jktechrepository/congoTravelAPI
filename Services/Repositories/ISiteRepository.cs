using CongoTravel.Models;
using CongoTravel.Models.DTOs.Site;

namespace CongoTravel.Services.Repositories
{
    public interface ISiteRepository
    {
        Task<IReadOnlyList<Site>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Site?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Site>> GetBySocieteAsync(int idSociete, CancellationToken cancellationToken = default);
        Task<Site> CreateAsync(Site site, CancellationToken cancellationToken = default);
        /// <summary>Crée un site et provisionne un Agent + Utilisateur Gérant (transactionnel).</summary>
        Task<SiteBootstrapCreationResult> CreateWithGerantAsync(SiteCreateDto dto, CancellationToken cancellationToken = default);
        Task<Site?> UpdateAsync(Site site, bool? isSitePrincipal = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> ToggleStatutAsync(int id, CancellationToken cancellationToken = default);
    }
}
