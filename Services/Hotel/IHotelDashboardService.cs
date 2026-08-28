using CongoTravel.Models.DTOs.Hotel;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelDashboardService
    {
        Task<HotelDashboardResponseDto> GetSocieteDashboardAsync(
            int idSociete, DateTime monthStartUtc, DateTime monthEndUtc,
            CancellationToken cancellationToken = default);

        Task<HotelSuperAdminDashboardResponseDto> GetSuperAdminDashboardAsync(
            DateTime monthStartUtc, DateTime monthEndUtc,
            CancellationToken cancellationToken = default);

        Task<HotelDashboardWidgetDto> GetWidgetAsync(
            int idSociete, DateTime monthStartUtc, DateTime monthEndUtc,
            CancellationToken cancellationToken = default);
    }
}
