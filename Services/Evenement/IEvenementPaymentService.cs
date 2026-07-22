using CongoTravel.Models.DTOs.Evenement;

namespace CongoTravel.Services.Evenement
{
    public interface IEvenementPaymentService
    {
        Task<EvenementConfirmPaymentResponseDto> ConfirmPaymentAsync(
            int idEvenementReservation,
            int idSociete,
            EvenementConfirmPaymentRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
