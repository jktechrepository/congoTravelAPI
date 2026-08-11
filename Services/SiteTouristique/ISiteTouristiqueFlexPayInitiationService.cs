using CongoTravel.Models.DTOs.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    /// <summary>
    /// Initiation paiement FlexPay sur une réservation site touristique <c>HOLD</c> existante (Phase 5).
    /// </summary>
    public interface ISiteTouristiqueFlexPayInitiationService
    {
        /// <summary>
        /// Crée un <c>SiteTouristiquePayment</c> <c>PENDING</c> et appelle l'API FlexPay.
        /// Ne confirme pas la réservation — finalisation via callback ou verify.
        /// </summary>
        Task<SiteTouristiqueInitiateFlexPayResponseDto> InitiateAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            SiteTouristiqueInitiateFlexPayRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
