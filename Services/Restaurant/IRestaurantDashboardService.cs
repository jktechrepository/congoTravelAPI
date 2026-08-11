using CongoTravel.Models.DTOs.Restaurant;

namespace CongoTravel.Services.Restaurant
{
    public interface IRestaurantDashboardService
    {
        Task<RestaurantDashboardResponseDto> GetSocieteDashboardAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);

        Task<RestaurantSuperAdminDashboardResponseDto> GetSuperAdminDashboardAsync(
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);

        Task<RestaurantDashboardWidgetDto> GetWidgetAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);

        Task<RestaurantDashboardWidgetDto> GetWidgetForSocietesAsync(
            IReadOnlyList<int> idSocietes,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);
    }
}
