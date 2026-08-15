using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    public interface ISocieteRepository
    {
        Task<IEnumerable<Societe>> GetAllAsync();
        Task<Societe> GetByIdAsync(int id);
        Task<Societe> GetByNomAsync(string nom);
       // Task<Societe> GetByCodeAsync(string code);
       // Task<IEnumerable<Societe>> GetByStatutAsync(bool statut);
        Task<Societe> CreateAsync(Societe societe);

        /// <summary>Crée une société avec site initial et compte gérant (transactionnel). L’Admin automatique est également provisionné.</summary>
        Task<SocieteBootstrapCreationResult> CreateWithBootstrapAsync(CreateSocieteWithBootstrapDto dto);
        Task<Societe> UpdateAsync(Societe societe);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByNomAsync(string nom);
     //   Task<bool> ExistsByCodeAsync(string code);
        Task<IEnumerable<Utilisateur>> GetUtilisateursAsync(int idSociete);
        Task<IEnumerable<Agent>> GetAgentsAsync(int idSociete);
        Task<PagedResult<Agent>> GetAgentsByRoleAsync(int idSociete, string roleNom, PagedRequest request);
        
        // ✅ SOFT DELETE
        Task<bool> ToggleStatutAsync(int id);
        Task<bool> SetStatutAsync(int id, bool statut);
    }
}
