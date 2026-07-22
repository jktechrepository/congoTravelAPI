using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Helpers.Evenement
{
    public static class EvenementDashboardEnrichmentHelper
    {
        public const string DashboardReadPermission = "Evenement.Dashboard.Read";

        public static async Task<EvenementDashboardWidgetDto?> TryLoadWidgetAsync(
            IEvenementDashboardService eventDashboard,
            IPermissionService permissionService,
            ICurrentUserService currentUser,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (!await CanReadEvenementDashboardAsync(permissionService, currentUser, cancellationToken))
                return null;

            var (_, monthStartUtc, _) = SocieteTransportMetricsHelper.GetUtcBoundaries();
            var monthEndUtc = monthStartUtc.AddMonths(1);

            return await eventDashboard.GetWidgetAsync(
                idSociete, monthStartUtc, monthEndUtc, cancellationToken);
        }

        public static async Task<EvenementDashboardWidgetDto?> TryLoadWidgetForSocietesAsync(
            IEvenementDashboardService eventDashboard,
            IPermissionService permissionService,
            ICurrentUserService currentUser,
            IReadOnlyList<int> idSocietes,
            CancellationToken cancellationToken = default)
        {
            if (idSocietes.Count == 0)
                return null;

            if (!await CanReadEvenementDashboardAsync(permissionService, currentUser, cancellationToken))
                return null;

            var (_, monthStartUtc, _) = SocieteTransportMetricsHelper.GetUtcBoundaries();
            var monthEndUtc = monthStartUtc.AddMonths(1);

            return await eventDashboard.GetWidgetForSocietesAsync(
                idSocietes, monthStartUtc, monthEndUtc, cancellationToken);
        }

        public static async Task<EvenementDashboardGlobalSummaryDto?> TryLoadSuperAdminWidgetAsync(
            IEvenementDashboardService eventDashboard,
            CancellationToken cancellationToken = default)
        {
            var (_, monthStartUtc, _) = SocieteTransportMetricsHelper.GetUtcBoundaries();
            var monthEndUtc = monthStartUtc.AddMonths(1);

            var dashboard = await eventDashboard.GetSuperAdminDashboardAsync(
                monthStartUtc, monthEndUtc, cancellationToken);

            return dashboard.Global;
        }

        private static async Task<bool> CanReadEvenementDashboardAsync(
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
