using CongoTravel.Models.DTOs.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    /// <summary>
    /// Façade Transport-like : hold + CASH confirm, ou hold + initiate FlexPay, en un appel.
    /// La société d’achat est résolue dans le service (JWT staff vs session Published pour Client).
    /// </summary>
    public interface ISiteTouristiqueReservationWithPaiementService
    {
        Task<SiteTouristiqueReservationWithPaiementResponseDto> CreateCashAsync(
            SiteTouristiqueReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueReservationWithPaiementResponseDto> InitiateElectronicAsync(
            SiteTouristiqueReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
