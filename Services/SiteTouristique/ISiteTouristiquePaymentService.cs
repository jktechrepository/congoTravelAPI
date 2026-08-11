using CongoTravel.Models.DTOs.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiquePaymentService
    {
        Task<SiteTouristiqueConfirmPaymentResponseDto> ConfirmPaymentAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            SiteTouristiqueConfirmPaymentRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
