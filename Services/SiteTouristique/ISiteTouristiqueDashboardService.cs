using CongoTravel.Models.DTOs.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueDashboardService
    {
        Task<SiteTouristiqueDashboardResponseDto> GetSocieteDashboardAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueSuperAdminDashboardResponseDto> GetSuperAdminDashboardAsync(
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueDashboardWidgetDto> GetWidgetAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueDashboardWidgetDto> GetWidgetForSocietesAsync(
            IReadOnlyList<int> idSocietes,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);
    }
}
