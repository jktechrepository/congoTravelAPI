using CongoTravel.Models.DTOs.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueJourneeGenerationService
    {
        Task<SiteTouristiquePlanificationGenerationResultDto> GenererAsync(
            int idPlanification,
            GenererSiteTouristiquePlanificationDto request,
            int? declencheParIdUtilisateur = null,
            CancellationToken cancellationToken = default);
    }
}
