using CongoTravel.Models.DTOs.Evenement;

namespace CongoTravel.Services.Evenement
{
    /// <summary>
    /// Façade Transport-like : hold + CASH confirm, ou hold + initiate FlexPay, en un appel.
    /// La société d’achat est résolue dans le service (JWT staff vs session Published pour Client).
    /// </summary>
    public interface IEvenementReservationWithPaiementService
    {
        Task<EvenementReservationWithPaiementResponseDto> CreateCashAsync(
            EvenementReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default);

        Task<EvenementReservationWithPaiementResponseDto> InitiateElectronicAsync(
            EvenementReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
