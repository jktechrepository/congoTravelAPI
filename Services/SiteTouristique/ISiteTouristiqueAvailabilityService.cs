using CongoTravel.Models.DTOs.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueAvailabilityService
    {
        Task<SiteTouristiqueAvailabilityResponseDto?> GetJourneeAvailabilityAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
