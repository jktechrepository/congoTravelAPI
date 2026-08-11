using CongoTravel.Models.DTOs.Restaurant;

namespace CongoTravel.Services.Restaurant
{
    public interface IRestaurantCreneauService
    {
        Task<RestaurantCreneauResponseDto> CreateDraftAsync(
            RestaurantCreateCreneauRequestDto request,
            int idSociete,
            int? idRestaurantPlanification = null,
            int? idRestaurantPlanificationPlage = null,
            CancellationToken cancellationToken = default);

        Task<RestaurantCreneauResponseDto?> GetByIdAsync(
            int idRestaurantCreneau,
            int? idSociete = null,
            CancellationToken cancellationToken = default);

        Task<RestaurantCreneauResponseDto?> GetPublishedByIdAsync(
            int idRestaurantCreneau,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RestaurantCreneauListItemDto>> ListAsync(
            int idSociete,
            RestaurantCreneauListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RestaurantCreneauListItemDto>> ListPublishedGlobalAsync(
            RestaurantCreneauListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<RestaurantCreneauResponseDto> PublishAsync(
            int idRestaurantCreneau,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
