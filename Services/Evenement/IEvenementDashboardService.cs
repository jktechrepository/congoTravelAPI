using CongoTravel.Models.DTOs.Evenement;

namespace CongoTravel.Services.Evenement
{
    public interface IEvenementDashboardService
    {
        Task<EvenementDashboardResponseDto> GetSocieteDashboardAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);

        Task<EvenementSuperAdminDashboardResponseDto> GetSuperAdminDashboardAsync(
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);

        Task<EvenementDashboardWidgetDto> GetWidgetAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);

        Task<EvenementDashboardWidgetDto> GetWidgetForSocietesAsync(
            IReadOnlyList<int> idSocietes,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default);
    }
}
