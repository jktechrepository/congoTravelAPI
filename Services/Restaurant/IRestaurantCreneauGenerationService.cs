using CongoTravel.Models.DTOs.Restaurant;

namespace CongoTravel.Services.Restaurant
{
    public interface IRestaurantCreneauGenerationService
    {
        Task<RestaurantPlanificationGenerationResultDto> GenererAsync(
            int idPlanification,
            GenererRestaurantPlanificationDto request,
            int? declencheParIdUtilisateur = null,
            CancellationToken cancellationToken = default);
    }
}
