using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Helpers.SiteTouristique
{
    public static class SiteTouristiqueDashboardEnrichmentHelper
    {
        public const string DashboardReadPermission = "SiteTouristique.Dashboard.Read";

        public static async Task<SiteTouristiqueDashboardWidgetDto?> TryLoadWidgetAsync(
            ISiteTouristiqueDashboardService eventDashboard,
            IPermissionService permissionService,
            ICurrentUserService currentUser,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (!await CanReadSiteTouristiqueDashboardAsync(permissionService, currentUser, cancellationToken))
                return null;

            var (_, monthStartUtc, _) = SocieteTransportMetricsHelper.GetUtcBoundaries();
            var monthEndUtc = monthStartUtc.AddMonths(1);

            return await eventDashboard.GetWidgetAsync(
                idSociete, monthStartUtc, monthEndUtc, cancellationToken);
        }

        public static async Task<SiteTouristiqueDashboardWidgetDto?> TryLoadWidgetForSocietesAsync(
            ISiteTouristiqueDashboardService eventDashboard,
            IPermissionService permissionService,
            ICurrentUserService currentUser,
            IReadOnlyList<int> idSocietes,
            CancellationToken cancellationToken = default)
        {
            if (idSocietes.Count == 0)
                return null;

            if (!await CanReadSiteTouristiqueDashboardAsync(permissionService, currentUser, cancellationToken))
                return null;

            var (_, monthStartUtc, _) = SocieteTransportMetricsHelper.GetUtcBoundaries();
            var monthEndUtc = monthStartUtc.AddMonths(1);

            return await eventDashboard.GetWidgetForSocietesAsync(
                idSocietes, monthStartUtc, monthEndUtc, cancellationToken);
        }

        public static async Task<SiteTouristiqueDashboardGlobalSummaryDto?> TryLoadSuperAdminWidgetAsync(
            ISiteTouristiqueDashboardService eventDashboard,
            CancellationToken cancellationToken = default)
        {
            var (_, monthStartUtc, _) = SocieteTransportMetricsHelper.GetUtcBoundaries();
            var monthEndUtc = monthStartUtc.AddMonths(1);

            var dashboard = await eventDashboard.GetSuperAdminDashboardAsync(
                monthStartUtc, monthEndUtc, cancellationToken);

            return dashboard.Global;
        }

        private static async Task<bool> CanReadSiteTouristiqueDashboardAsync(
            IPermissionService permissionService,
            ICurrentUserService currentUser,
            CancellationToken cancellationToken)
        {
            if (currentUser.IsSuperAdmin)
                return true;

            if (currentUser.UserId <= 0)
                return false;

            return await permissionService.UserHasPermissionAsync(
                currentUser.UserId, DashboardReadPermission);
        }
    }
}
