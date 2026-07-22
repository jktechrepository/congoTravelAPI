using CongoTravel.Models.DTOs.Evenement;

namespace CongoTravel.Services.Evenement
{
    public interface IEvenementAvailabilityService
    {
        Task<EvenementAvailabilityResponseDto?> GetSessionAvailabilityAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
