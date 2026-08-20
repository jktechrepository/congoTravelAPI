using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Services;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class RestaurantPhase5DashboardTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockClientUser(int jwtSocieteId = 999)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(u => u.IsStaff).Returns(false);
            mock.SetupGet(u => u.IsSuperAdmin).Returns(false);
            mock.SetupGet(u => u.SocieteId).Returns(jwtSocieteId);
            mock.SetupGet(u => u.UserId).Returns(0);
            return mock;
        }

        private static RestaurantReservationWithPaiementService CreateWithPaiementService(
            CongoTravelDbContext ctx,
            ICurrentUserService? currentUser = null)
        {
            var hold = RestaurantTestFactories.CreateHoldService(ctx);
            var confirmation = RestaurantTestFactories.CreateConfirmationService(ctx);
            var payment = new RestaurantPaymentService(
                ctx,
                confirmation,
                NullLogger<RestaurantPaymentService>.Instance);
            var reservation = RestaurantTestFactories.CreateReservationService(ctx);
            var commandeFlexPay = RestaurantTestFactories.CreateCommandeFlexPayService(
                ctx,
                Mock.Of<IFlexPayService>());

            return new RestaurantReservationWithPaiementService(
                ctx,
                hold,
                payment,
                commandeFlexPay,
                reservation,
                currentUser ?? MockClientUser().Object,
                NullLogger<RestaurantReservationWithPaiementService>.Instance);
        }

        private static (DateTime MonthStart, DateTime MonthEnd) GetCurrentMonthRange()
        {
            var (_, monthStart, _) = SocieteTransportMetricsHelper.GetUtcBoundaries(DateTime.UtcNow);
            return (monthStart, monthStart.AddMonths(1));
        }

        [Fact]
        public void AddRestaurantReservations_registers_dashboard_service()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<CongoTravelDbContext>(options =>
                options.UseInMemoryDatabase(nameof(AddRestaurantReservations_registers_dashboard_service)));
            services.AddScoped<IConfigSocieteRepository, ConfigSocieteService>();
            services.AddSingleton(Mock.Of<ICurrentUserService>());
            services.AddSingleton(Mock.Of<IFlexPayRealtimeNotifier>());
            services.AddSingleton(Mock.Of<IFlexPayService>());
            services.AddSingleton(Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>());
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(
                new CongoTravel.Configuration.FlexPayOptions { Enabled = true }));
            services.AddScoped<IInfoPaiementResolutionService, InfoPaiementResolutionService>();
            services.AddScoped<IDeviseMontantConverter, DeviseMontantConverter>();
            services.AddRestaurantReservations();

            using var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IRestaurantDashboardService>());
        }

        [Fact]
        public async Task GetSocieteDashboardAsync_returns_metrics_after_cash_confirm()
        {
            await using var ctx = BuildDb(nameof(GetSocieteDashboardAsync_returns_metrics_after_cash_confirm));
            var (idSociete, _, idCreneau) = await RestaurantTestFactories.SeedPublishedCreneauAsync(
                ctx, "DASH", capacite: 20, prixUnitaire: 50m, acomptePourcent: 20m);

            var withPaiement = CreateWithPaiementService(ctx);
            var cash = await withPaiement.CreateCashAsync(new RestaurantReservationWithPaiementRequestDto
            {
                IdRestaurantCreneau = idCreneau,
                CustomerRef = "DASH-TABLE",
                Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 2 } },
                Paiement = new RestaurantReservationPaiementBlockDto
                {
                    MethodePaiement = "CASH",
                    ReferenceTransaction = "DASH-CAISSE-1"
                }
            });

            Assert.Equal("CONFIRMED", cash.Reservation.Status);
            Assert.Equal("SUCCEEDED", cash.Payment!.Status);

            var service = new RestaurantDashboardService(ctx, NullLogger<RestaurantDashboardService>.Instance);
            var (monthStart, monthEnd) = GetCurrentMonthRange();

            var dashboard = await service.GetSocieteDashboardAsync(idSociete, monthStart, monthEnd);

            Assert.Equal(idSociete, dashboard.IdSociete);
            Assert.True(dashboard.Summary.EtablissementsPublies >= 1);
            Assert.True(dashboard.Summary.CreneauxPublies >= 1);
            Assert.True(dashboard.Summary.ReservationsConfirmeesMois >= 1);
            Assert.Equal(1, dashboard.Reservations.Confirmed);
            Assert.Contains(dashboard.RevenuParProvider, r => r.Provider == "CASH");
            Assert.True(dashboard.Summary.MontantAcomptesSuccesMois > 0);
            Assert.NotEmpty(dashboard.Top5CreneauxCa);
            Assert.Equal(2, dashboard.Top5CreneauxCa[0].CouvertsConfirmes);
            Assert.NotEmpty(dashboard.ReservationsRecentes);
            Assert.NotEmpty(dashboard.PaiementsRecents);
        }

        [Fact]
        public async Task GetWidgetAsync_returns_compact_summary()
        {
            await using var ctx = BuildDb(nameof(GetWidgetAsync_returns_compact_summary));
            var (idSociete, _, idCreneau) = await RestaurantTestFactories.SeedPublishedCreneauAsync(
                ctx, "WGT", capacite: 10, prixUnitaire: 40m, acomptePourcent: 25m);

            var withPaiement = CreateWithPaiementService(ctx);
            await withPaiement.CreateCashAsync(new RestaurantReservationWithPaiementRequestDto
            {
                IdRestaurantCreneau = idCreneau,
                Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 1 } },
                Paiement = new RestaurantReservationPaiementBlockDto { MethodePaiement = "CASH" }
            });

            var service = new RestaurantDashboardService(ctx, NullLogger<RestaurantDashboardService>.Instance);
            var (monthStart, monthEnd) = GetCurrentMonthRange();

            var widget = await service.GetWidgetAsync(idSociete, monthStart, monthEnd);

            Assert.True(widget.Summary.ReservationsConfirmeesMois >= 1);
            Assert.Contains(widget.RevenuParProvider, r => r.Provider == "CASH");
            Assert.NotEmpty(widget.TopCreneauxCa);
        }
    }
}
