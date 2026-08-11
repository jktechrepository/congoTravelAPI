using CongoTravel.Models.DTOs.Restaurant;

namespace CongoTravel.Services.Restaurant
{
    public interface IRestaurantHoldService
    {
        Task<RestaurantHoldResponseDto> CreateHoldAsync(
            int idRestaurantCreneau,
            int idSociete,
            RestaurantHoldRequestDto request,
            CancellationToken cancellationToken = default);
    }

    public interface IRestaurantPaymentService
    {
        Task<RestaurantConfirmPaymentResponseDto> ConfirmPaymentAsync(
            int idRestaurantReservation,
            int idSociete,
            RestaurantConfirmPaymentRequestDto request,
            CancellationToken cancellationToken = default);
    }

    public interface IRestaurantReservationConfirmationService
    {
        void EnsureHoldConfirmable(Models.Restaurant.RestaurantReservation reservation);

        Task ConfirmHoldAndMarkPaymentSucceededAsync(
            Models.Restaurant.RestaurantReservation reservation,
            Models.Restaurant.RestaurantPayment payment,
            int idSociete,
            CancellationToken cancellationToken = default);
    }

    public interface IRestaurantReservationService
    {
        Task<RestaurantReservationResponseDto?> GetByIdAsync(
            int idRestaurantReservation,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RestaurantReservationListItemDto>> ListAsync(
            int idSociete,
            RestaurantReservationListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<RestaurantCancelReservationResponseDto> CancelAsync(
            int idRestaurantReservation,
            int idSociete,
            CancellationToken cancellationToken = default);
    }

    public interface IRestaurantReservationWithPaiementService
    {
        Task<RestaurantReservationWithPaiementResponseDto> CreateCashAsync(
            RestaurantReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default);

        Task<RestaurantReservationWithPaiementResponseDto> InitiateElectronicAsync(
            RestaurantReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default);
    }

    public interface IRestaurantAvailabilityService
    {
        Task<RestaurantAvailabilityResponseDto?> GetAvailabilityAsync(
            int idRestaurantCreneau,
            int idSociete,
            CancellationToken cancellationToken = default);
    }

    public interface IRestaurantHoldExpirationRunner
    {
        Task ExpireHoldsAsync(
            Data.CongoTravelDbContext context,
            CancellationToken cancellationToken = default);
    }
}
