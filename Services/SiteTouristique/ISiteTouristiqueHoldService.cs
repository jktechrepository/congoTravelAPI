using CongoTravel.Models.DTOs.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueHoldService
    {
        Task<SiteTouristiqueHoldResponseDto> CreateHoldAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            SiteTouristiqueHoldRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
