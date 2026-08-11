using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.Repositories;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiqueHoldExpirationRunnerTests
    {
        private const string MessageHoldExpire =
            "Hold expiré. Le paiement n’a pas été confirmé à temps.";

        private static CongoTravelDbContext BuildInMemoryDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static SiteTouristiqueHoldExpirationRunner CreateRunner(IFlexPayRealtimeNotifier? realtime = null) =>
            new(
                realtime ?? Mock.Of<IFlexPayRealtimeNotifier>(),
                NullLogger<SiteTouristiqueHoldExpirationRunner>.Instance);

        [Fact]
        public async Task ExpireHoldsAsync_no_expired_holds_is_noop()
        {
            await using var ctx = BuildInMemoryDb(nameof(ExpireHoldsAsync_no_expired_holds_is_noop));
            var runner = CreateRunner();

            await runner.ExpireHoldsAsync(ctx);

            Assert.True(true);
        }

        [Fact]
        public async Task ExpireHoldsAsync_skips_when_hold_not_yet_expired()
        {
            await using var ctx = BuildInMemoryDb(nameof(ExpireHoldsAsync_skips_when_hold_not_yet_expired));
            var (_, idReservation, orderNumber) =
                await SiteTouristiqueTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1, idUtilisateur: 11);
            var reservation = await ctx.SiteTouristiqueReservations
                .FirstAsync(r => r.IdSiteTouristiqueReservation == idReservation);
            reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30);
            await ctx.SaveChangesAsync();

            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            await CreateRunner(realtime.Object).ExpireHoldsAsync(ctx);

            Assert.Equal(SiteTouristiqueReservationStatus.HOLD,
                await ctx.SiteTouristiqueReservations.Select(r => r.Status).SingleAsync());
            Assert.Equal(SiteTouristiquePaymentStatus.PENDING,
                await ctx.SiteTouristiquePayments.Select(p => p.Status).SingleAsync());
            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
            Assert.Equal(orderNumber, await ctx.SiteTouristiquePayments.Select(p => p.ProviderTxRef).SingleAsync());
        }

        [Fact]
        public async Task ExpireHoldsAsync_expired_pending_flexpay_marks_failed_and_notifies()
        {
            await using var ctx = BuildInMemoryDb(
                nameof(ExpireHoldsAsync_expired_pending_flexpay_marks_failed_and_notifies));
            var (_, idReservation, orderNumber) =
                await SiteTouristiqueTestFactories.SeedPendingFlexPayPaymentAsync(
                    ctx, quantity: 1, orderNumber: "ST-EXPIRE-001", idUtilisateur: 42);
            var reservation = await ctx.SiteTouristiqueReservations
                .FirstAsync(r => r.IdSiteTouristiqueReservation == idReservation);
            reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-2);
            await ctx.SaveChangesAsync();

            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            await CreateRunner(realtime.Object).ExpireHoldsAsync(ctx);

            Assert.Equal(SiteTouristiqueReservationStatus.EXPIRED,
                await ctx.SiteTouristiqueReservations.Select(r => r.Status).SingleAsync());
            Assert.Equal(SiteTouristiquePaymentStatus.FAILED,
                await ctx.SiteTouristiquePayments.Select(p => p.Status).SingleAsync());
            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    42, orderNumber, MessageHoldExpire, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExpireHoldsAsync_expired_without_utilisateur_fails_payment_without_notify()
        {
            await using var ctx = BuildInMemoryDb(
                nameof(ExpireHoldsAsync_expired_without_utilisateur_fails_payment_without_notify));
            var (_, idReservation, _) =
                await SiteTouristiqueTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var reservation = await ctx.SiteTouristiqueReservations
                .FirstAsync(r => r.IdSiteTouristiqueReservation == idReservation);
            reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await ctx.SaveChangesAsync();

            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            await CreateRunner(realtime.Object).ExpireHoldsAsync(ctx);

            Assert.Equal(SiteTouristiquePaymentStatus.FAILED,
                await ctx.SiteTouristiquePayments.Select(p => p.Status).SingleAsync());
            Assert.Equal(SiteTouristiqueReservationStatus.EXPIRED,
                await ctx.SiteTouristiqueReservations.Select(r => r.Status).SingleAsync());
            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
