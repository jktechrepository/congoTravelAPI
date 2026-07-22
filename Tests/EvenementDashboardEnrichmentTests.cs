using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementDashboardEnrichmentTests
    {
        [Fact]
        public async Task TryLoadWidget_returns_null_when_permission_denied()
        {
            var eventDashboard = DashboardEnrichmentTestHelper.CreateEvenementDashboardMock();
            var permissions = DashboardEnrichmentTestHelper.CreatePermissionMock(granted: false);
            var user = DashboardEnrichmentTestHelper.CreateUserMock();

            var result = await EvenementDashboardEnrichmentHelper.TryLoadWidgetAsync(
                eventDashboard.Object,
                permissions.Object,
                user.Object,
                idSociete: 1);

            Assert.Null(result);
            eventDashboard.Verify(
                x => x.GetWidgetAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task TryLoadWidget_returns_widget_when_permission_granted()
        {
            var eventDashboard = new Mock<IEvenementDashboardService>();
            eventDashboard.Setup(x => x.GetWidgetAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EvenementDashboardWidgetDto
                {
                    Summary = new EvenementDashboardSummaryDto { HoldsEnCours = 5 }
                });

            var permissions = DashboardEnrichmentTestHelper.CreatePermissionMock(granted: true);
            var user = DashboardEnrichmentTestHelper.CreateUserMock();

            var result = await EvenementDashboardEnrichmentHelper.TryLoadWidgetAsync(
                eventDashboard.Object,
                permissions.Object,
                user.Object,
                idSociete: 1);

            Assert.NotNull(result);
            Assert.Equal(5, result!.Summary.HoldsEnCours);
        }

        [Fact]
        public async Task TryLoadSuperAdminWidget_always_returns_global_summary()
        {
            var eventDashboard = DashboardEnrichmentTestHelper.CreateEvenementDashboardMock();

            var result = await EvenementDashboardEnrichmentHelper.TryLoadSuperAdminWidgetAsync(
                eventDashboard.Object);

            Assert.NotNull(result);
            Assert.Equal(4, result!.SessionsPubliees);
        }
    }
}
