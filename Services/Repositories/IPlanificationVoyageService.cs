using CongoTravel.Models.DTOs.PlanificationVoyage;

namespace CongoTravel.Services.Repositories
{
    public interface IPlanificationVoyageService
    {
        Task<IReadOnlyList<PlanificationVoyageResponseDto>> GetBySocieteAsync(int idSociete, CancellationToken cancellationToken = default);
        Task<PlanificationVoyageResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PlanificationVoyageResponseDto> CreateAsync(CreatePlanificationVoyageDto dto, CancellationToken cancellationToken = default);
        Task<PlanificationVoyageResponseDto?> UpdateAsync(UpdatePlanificationVoyageDto dto, CancellationToken cancellationToken = default);
        Task<bool> ToggleStatutAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
