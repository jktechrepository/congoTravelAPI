using CongoTravel.Models.DTOs.PlanificationVoyage;

namespace CongoTravel.Services.Repositories
{
    public interface IVoyageGenerationService
    {
        Task<PlanificationGenerationResultDto> GenererAsync(
            int idPlanificationVoyage,
            GenererPlanificationVoyageDto request,
            int? declencheParIdUtilisateur = null,
            CancellationToken cancellationToken = default);
    }
}
