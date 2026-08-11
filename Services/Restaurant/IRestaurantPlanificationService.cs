using CongoTravel.Models.DTOs.Restaurant;

namespace CongoTravel.Services.Restaurant
{
    public interface IRestaurantPlanificationService
    {
        Task<IReadOnlyList<RestaurantPlanificationListItemDto>> ListAsync(
            int idSociete,
            int? idRestaurant = null,
            CancellationToken cancellationToken = default);

        Task<RestaurantPlanificationResponseDto?> GetByIdAsync(
            int id,
            int? idSociete = null,
            CancellationToken cancellationToken = default);

        Task<RestaurantPlanificationResponseDto> CreateAsync(
            RestaurantCreatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<RestaurantPlanificationResponseDto?> UpdateAsync(
            RestaurantUpdatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<bool> ToggleStatutAsync(int id, int idSociete, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, int idSociete, CancellationToken cancellationToken = default);
    }
}
