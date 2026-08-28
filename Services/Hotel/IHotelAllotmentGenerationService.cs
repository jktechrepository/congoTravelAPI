using CongoTravel.Models.DTOs.Hotel;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelAllotmentGenerationService
    {
        Task<HotelPlanificationGenerationResultDto> GenererAsync(
            int idPlanification,
            GenererHotelPlanificationDto request,
            int? declencheParIdUtilisateur = null,
            CancellationToken cancellationToken = default);
    }
}
