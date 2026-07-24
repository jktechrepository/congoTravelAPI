using CongoTravel.Models.DTOs.Evenement;

namespace CongoTravel.Services.Evenement
{
    /// <summary>
    /// Façade Transport-like : hold + CASH confirm, ou hold + initiate FlexPay, en un appel.
    /// </summary>
    public interface IEvenementReservationWithPaiementService
    {
        Task<EvenementReservationWithPaiementResponseDto> CreateCashAsync(
            int idSociete,
            EvenementReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default);

        Task<EvenementReservationWithPaiementResponseDto> InitiateElectronicAsync(
            int idSociete,
            EvenementReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
