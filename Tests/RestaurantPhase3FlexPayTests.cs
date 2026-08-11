using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class RestaurantPhase3FlexPayTests
    {
        private const string MessageHoldExpire =
            "Hold expiré. Le paiement n’a pas été confirmé à temps.";

        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockClientUser(int userId = 42, int jwtSocieteId = 999)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(u => u.IsStaff).Returns(false);
            mock.SetupGet(u => u.IsSuperAdmin).Returns(false);
            mock.SetupGet(u => u.SocieteId).Returns(jwtSocieteId);
            mock.SetupGet(u => u.UserId).Returns(userId);
            return mock;
        }

        private static RestaurantReservationWithPaiementService CreateWithPaiementService(
            CongoTravelDbContext ctx,
            IFlexPayService flexPay,
            ICurrentUserService? currentUser = null)
        {
            return new RestaurantReservationWithPaiementService(
                ctx,
                RestaurantTestFactories.CreateHoldService(ctx),
                RestaurantTestFactories.CreatePaymentService(ctx),
                RestaurantTestFactories.CreateFlexPayInitiationService(ctx, flexPay),
                RestaurantTestFactories.CreateReservationService(ctx),
                currentUser ?? MockClientUser().Object,
                NullLogger<RestaurantReservationWithPaiementService>.Instance);
        }

        [Fact]
        public void AddRestaurantReservations_registers_flexpay_services()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<CongoTravelDbContext>(options =>
                options.UseInMemoryDatabase(nameof(AddRestaurantReservations_registers_flexpay_services)));
            services.AddScoped<IConfigSocieteRepository, ConfigSocieteService>();
            services.AddSingleton(Mock.Of<ICurrentUserService>());
            services.AddSingleton(Mock.Of<IFlexPayService>());
            services.AddSingleton(Mock.Of<IFlexPayRealtimeNotifier>());
            services.AddSingleton(Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>());
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(
                new CongoTravel.Configuration.FlexPayOptions { Enabled = true }));
            services.AddScoped<IInfoPaiementResolutionService, InfoPaiementResolutionService>();
            services.AddScoped<IDeviseMontantConverter, DeviseMontantConverter>();
            services.AddRestaurantReservations();

            using var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IRestaurantFlexPayInitiationService>());
            Assert.NotNull(provider.GetService<IRestaurantFlexPayCallbackService>());
        }

        [Fact]
        public async Task With_paiement_electronique_creates_pending_payment_and_orderNumber()
        {
            await using var ctx = BuildDb(nameof(With_paiement_electronique_creates_pending_payment_and_orderNumber));
            var (idSociete, idSite, idCreneau) =
                await RestaurantTestFactories.SeedPublishedCreneauWithFlexPayAsync(ctx, "ELEC");
            var flexApi = RestaurantTestFactories.CreateFlexPayApiMock("FP-RST-ELEC-001");
            var service = CreateWithPaiementService(ctx, flexApi.Object);

            var result = await service.InitiateElectronicAsync(new RestaurantReservationWithPaiementRequestDto
            {
                IdRestaurantCreneau = idCreneau,
                Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 2 } },
                Paiement = new RestaurantReservationPaiementBlockDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "243900000001",
                    IdSite = idSite
                }
            });

            Assert.Equal("EnAttente", result.TransactionStatut);
            Assert.Equal("HOLD", result.Reservation.Status);
            Assert.Equal(idSociete, result.Reservation.IdSociete);
            Assert.Equal("PENDING", result.Payment!.Status);
            Assert.Equal(RestaurantFlexPayConstants.Provider, result.Payment.Provider);
            Assert.Equal("FP-RST-ELEC-001", result.OrderNumber);
            Assert.True(result.FlexPayAccepted);
            Assert.DoesNotContain("Phase 3", result.Message);

            var payment = await ctx.RestaurantPayments.SingleAsync();
            Assert.Equal(RestaurantPaymentStatus.PENDING, payment.Status);
            Assert.Equal("FP-RST-ELEC-001", payment.ProviderTxRef);

            var quota = await ctx.RestaurantCreneauGlobalQuotas
                .SingleAsync(q => q.IdRestaurantCreneau == idCreneau);
            Assert.Equal(2, quota.QuantiteHold);
            Assert.Equal(0, quota.QuantiteVendue);
        }

        [Fact]
        public async Task Callback_code_0_confirms_and_notifies_once()
        {
            await using var ctx = BuildDb(nameof(Callback_code_0_confirms_and_notifies_once));
            var (_, idReservation, orderNumber) =
                await RestaurantTestFactories.SeedPendingFlexPayPaymentAsync(
                    ctx, quantity: 2, orderNumber: "FP-RST-OK-001", idUtilisateur: 42);
            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            var service = RestaurantTestFactories.CreateCallbackService(ctx, realtimeNotifier: realtime.Object);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Amount = "20",
                Currency = "USD"
            });

            Assert.True(result.Success);
            Assert.False(result.AlreadyProcessed);
            Assert.Equal(idReservation, result.IdRestaurantReservation);

            Assert.Equal(RestaurantReservationStatus.CONFIRMED,
                await ctx.RestaurantReservations.Select(r => r.Status).SingleAsync());
            Assert.Equal(RestaurantPaymentStatus.SUCCEEDED,
                await ctx.RestaurantPayments.Select(p => p.Status).SingleAsync());

            var idPayment = result.IdRestaurantPayment!.Value;
            realtime.Verify(
                n => n.NotifyPaymentConfirmedAsync(
                    42, orderNumber, idReservation, idPayment, It.IsAny<CancellationToken>()),
                Times.Once);

            var second = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Amount = "20",
                Currency = "USD"
            });
            Assert.True(second.AlreadyProcessed);
            realtime.Verify(
                n => n.NotifyPaymentConfirmedAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Callback_code_nonzero_fails_releases_hold_and_notifies()
        {
            await using var ctx = BuildDb(nameof(Callback_code_nonzero_fails_releases_hold_and_notifies));
            var (_, idReservation, orderNumber) =
                await RestaurantTestFactories.SeedPendingFlexPayPaymentAsync(
                    ctx, quantity: 1, orderNumber: "FP-RST-FAIL-001", idUtilisateur: 7);
            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            var service = RestaurantTestFactories.CreateCallbackService(ctx, realtimeNotifier: realtime.Object);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "1",
                OrderNumber = orderNumber
            });

            Assert.True(result.Success);
            Assert.False(result.PaymentPending);
            Assert.Equal(idReservation, result.IdRestaurantReservation);

            Assert.Equal(RestaurantPaymentStatus.FAILED,
                await ctx.RestaurantPayments.Select(p => p.Status).SingleAsync());
            Assert.Equal(RestaurantReservationStatus.CANCELLED,
                await ctx.RestaurantReservations.Select(r => r.Status).SingleAsync());
            Assert.Equal(0, await ctx.RestaurantCreneauGlobalQuotas.Select(q => q.QuantiteHold).SingleAsync());

            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    7, orderNumber, It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExpireHoldsAsync_pending_flexpay_marks_failed_and_notifies()
        {
            await using var ctx = BuildDb(nameof(ExpireHoldsAsync_pending_flexpay_marks_failed_and_notifies));
            var (_, idReservation, orderNumber) =
                await RestaurantTestFactories.SeedPendingFlexPayPaymentAsync(
                    ctx, quantity: 1, orderNumber: "FP-RST-EXPIRE-001", idUtilisateur: 42);

            var reservation = await ctx.RestaurantReservations
                .FirstAsync(r => r.IdRestaurantReservation == idReservation);
            reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-2);
            await ctx.SaveChangesAsync();

            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            var runner = new RestaurantHoldExpirationRunner(
                realtime.Object,
                NullLogger<RestaurantHoldExpirationRunner>.Instance);
            await runner.ExpireHoldsAsync(ctx);

            Assert.Equal(RestaurantReservationStatus.EXPIRED,
                await ctx.RestaurantReservations.Select(r => r.Status).SingleAsync());
            Assert.Equal(RestaurantPaymentStatus.FAILED,
                await ctx.RestaurantPayments.Select(p => p.Status).SingleAsync());
            Assert.Equal(0, await ctx.RestaurantCreneauGlobalQuotas.Select(q => q.QuantiteHold).SingleAsync());

            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    42, orderNumber, MessageHoldExpire, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task With_paiement_electronique_no_longer_throws_phase3()
        {
            await using var ctx = BuildDb(nameof(With_paiement_electronique_no_longer_throws_phase3));
            var (_, idSite, idCreneau) =
                await RestaurantTestFactories.SeedPublishedCreneauWithFlexPayAsync(ctx, "NOPHASE");
            var flexApi = RestaurantTestFactories.CreateFlexPayApiMock("FP-RST-NOPHASE");
            var service = CreateWithPaiementService(ctx, flexApi.Object);

            var result = await service.InitiateElectronicAsync(new RestaurantReservationWithPaiementRequestDto
            {
                IdRestaurantCreneau = idCreneau,
                Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 1 } },
                Paiement = new RestaurantReservationPaiementBlockDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "0990000000",
                    IdSite = idSite
                }
            });

            Assert.Equal("EnAttente", result.TransactionStatut);
            Assert.DoesNotContain("Phase 3", result.Message ?? string.Empty);
            Assert.False(
                string.Equals(result.Message, "Paiement électronique restaurant non disponible (Phase 3).", StringComparison.Ordinal));
        }
    }
}
