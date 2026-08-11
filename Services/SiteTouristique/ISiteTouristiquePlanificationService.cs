using CongoTravel.Models.DTOs.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiquePlanificationService
    {
        Task<IReadOnlyList<SiteTouristiquePlanificationListItemDto>> ListAsync(
            int idSociete,
            int? idSiteTouristique = null,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiquePlanificationResponseDto?> GetByIdAsync(
            int id,
            int? idSociete = null,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiquePlanificationResponseDto> CreateAsync(
            SiteTouristiqueCreatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiquePlanificationResponseDto?> UpdateAsync(
            SiteTouristiqueUpdatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<bool> ToggleStatutAsync(int id, int idSociete, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, int idSociete, CancellationToken cancellationToken = default);
    }
}
