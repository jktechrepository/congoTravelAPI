using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Tests
{
    public static class DashboardEnrichmentTestHelper
    {
        public static Mock<IEvenementDashboardService> CreateEvenementDashboardMock()
        {
            var mock = new Mock<IEvenementDashboardService>();
            mock.Setup(x => x.GetWidgetAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EvenementDashboardWidgetDto
                {
                    Summary = new EvenementDashboardSummaryDto { SessionsPubliees = 2 }
                });

            mock.Setup(x => x.GetWidgetForSocietesAsync(
                    It.IsAny<IReadOnlyList<int>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EvenementDashboardWidgetDto
                {
                    Summary = new EvenementDashboardSummaryDto { SessionsPubliees = 3 }
                });

            mock.Setup(x => x.GetSuperAdminDashboardAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EvenementSuperAdminDashboardResponseDto
                {
                    Global = new EvenementDashboardGlobalSummaryDto
                    {
                        TotalSocietesActives = 1,
                        SessionsPubliees = 4
                    }
                });

            return mock;
        }

        public static Mock<IPermissionService> CreatePermissionMock(bool granted = false)
        {
            var mock = new Mock<IPermissionService>();
            mock.Setup(x => x.UserHasPermissionAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(granted);
            return mock;
        }

        public static Mock<ICurrentUserService> CreateUserMock(int userId = 10, bool isSuperAdmin = false)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.UserId).Returns(userId);
            mock.SetupGet(x => x.IsSuperAdmin).Returns(isSuperAdmin);
            return mock;
        }

        public static DashboardService CreateTransportDashboardService(
            CongoTravelDbContext ctx,
            ICurrentUserService? user = null,
            bool grantEvenementPermission = false) =>
            new(
                ctx,
                CreateEvenementDashboardMock().Object,
                CreatePermissionMock(grantEvenementPermission).Object,
                user ?? CreateUserMock().Object,
                NullLogger<DashboardService>.Instance);

        public static FinancierDashboardService CreateFinancierDashboardService(
            CongoTravelDbContext ctx,
            ICurrentUserService user,
            bool grantEvenementPermission = false) =>
            new(
                ctx,
                user,
                CreateEvenementDashboardMock().Object,
                CreatePermissionMock(grantEvenementPermission).Object,
                NullLogger<FinancierDashboardService>.Instance);
    }
}
