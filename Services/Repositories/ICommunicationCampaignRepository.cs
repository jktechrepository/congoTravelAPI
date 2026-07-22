using CongoTravel.Models;
using CongoTravel.Models.DTOs.Communication;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le repository de gestion des campagnes de communication
    /// </summary>
    public interface ICommunicationCampaignRepository
    {
        Task<IEnumerable<CommunicationCampaign>> GetAllAsync();
        Task<PagedResult<CommunicationCampaign>> GetPagedAsync(PagedRequest request);
        Task<CommunicationCampaign?> GetByIdAsync(int id);
        Task<CommunicationCampaign> CreateAsync(CommunicationCampaign campaign);
        Task<CommunicationCampaign> UpdateAsync(CommunicationCampaign campaign);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}

