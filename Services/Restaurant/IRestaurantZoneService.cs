using CongoTravel.Models.DTOs.Restaurant;

namespace CongoTravel.Services.Restaurant
{
    public interface IRestaurantZoneService
    {
        Task<RestaurantZoneResponseDto> CreateAsync(
            RestaurantCreateZoneRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<RestaurantZoneResponseDto?> GetByIdAsync(
            int idRestaurantZone,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<RestaurantZoneResponseDto?> GetPublishedByIdAsync(
            int idRestaurantZone,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RestaurantZoneResponseDto>> ListAsync(
            int idSociete,
            int? idRestaurant = null,
            bool actifsSeulement = false,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RestaurantZoneResponseDto>> ListPublishedGlobalAsync(
            int? idSociete = null,
            int? idRestaurant = null,
            bool actifsSeulement = false,
            CancellationToken cancellationToken = default);

        Task<RestaurantZoneResponseDto?> UpdateAsync(
            int idRestaurantZone,
            RestaurantUpdateZoneRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<RestaurantZoneResponseDto?> ToggleStatutAsync(
            int idRestaurantZone,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
