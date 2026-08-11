using CongoTravel.Models.DTOs.Restaurant;

namespace CongoTravel.Services.Restaurant
{
    /// <summary>
    /// Initiation paiement FlexPay sur une réservation restaurant <c>HOLD</c> existante (Phase 3).
    /// </summary>
    public interface IRestaurantFlexPayInitiationService
    {
        /// <summary>
        /// Crée un <c>RestaurantPayment</c> <c>PENDING</c> et appelle l'API FlexPay.
        /// Ne confirme pas la réservation — finalisation via callback ou verify.
        /// </summary>
        Task<RestaurantInitiateFlexPayResponseDto> InitiateAsync(
            int idRestaurantReservation,
            int idSociete,
            RestaurantInitiateFlexPayRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
