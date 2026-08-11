using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementFlexPayVerifierTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task VerifyAndFinalizeAsync_success_confirms_reservation()
        {
            await using var ctx = BuildDb(nameof(VerifyAndFinalizeAsync_success_confirms_reservation));
            var (idSociete, _, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 2);
            var service = EvenementTestFactories.CreateCallbackService(
                ctx,
                EvenementTestFactories.CreateFlexPayCheckMock("0"));

            var result = await service.VerifyAndFinalizeAsync(orderNumber, idSociete);

            Assert.True(result.IsConfirmSuccess);
            Assert.NotNull(result.ConfirmPayment);
            Assert.Equal("CONFIRMED", result.ConfirmPayment!.Reservation.Status);
            Assert.Equal(2, result.ConfirmPayment.Reservation.Tickets.Count);
            Assert.Equal(EvenementPaymentStatus.SUCCEEDED,
                Enum.Parse<EvenementPaymentStatus>(result.ConfirmPayment.Payment.Status));
        }

        [Fact]
        public async Task VerifyAndFinalizeAsync_pending_keeps_hold()
        {
            await using var ctx = BuildDb(nameof(VerifyAndFinalizeAsync_pending_keeps_hold));
            var (idSociete, _, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var service = EvenementTestFactories.CreateCallbackService(
                ctx,
                EvenementTestFactories.CreateFlexPayCheckMock("2"));

            var result = await service.VerifyAndFinalizeAsync(orderNumber, idSociete);

            Assert.False(result.IsConfirmSuccess);
            Assert.NotNull(result.StatusOnly);
            Assert.True(result.StatusOnly!.PaymentPending);
            Assert.Equal(EvenementReservationStatus.HOLD,
                await ctx.EvenementReservations.Select(r => r.Status).SingleAsync());
            Assert.Equal(EvenementPaymentStatus.PENDING,
                await ctx.EvenementPayments.Select(p => p.Status).SingleAsync());
        }

        [Fact]
        public async Task VerifyAndFinalizeAsync_is_idempotent_when_already_confirmed()
        {
            await using var ctx = BuildDb(nameof(VerifyAndFinalizeAsync_is_idempotent_when_already_confirmed));
            var (idSociete, _, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var service = EvenementTestFactories.CreateCallbackService(
                ctx,
                EvenementTestFactories.CreateFlexPayCheckMock("0"));

            var first = await service.VerifyAndFinalizeAsync(orderNumber, idSociete);
            var second = await service.VerifyAndFinalizeAsync(orderNumber, idSociete);

            Assert.True(first.IsConfirmSuccess);
            Assert.True(second.IsConfirmSuccess);
            Assert.True(second.ConfirmPayment!.AlreadyConfirmed);
            Assert.Equal(1, await ctx.EvenementTickets.CountAsync());
        }

        [Fact]
        public async Task VerifyAndFinalizeAsync_failure_marks_payment_failed()
        {
            await using var ctx = BuildDb(nameof(VerifyAndFinalizeAsync_failure_marks_payment_failed));
            var (idSociete, _, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var service = EvenementTestFactories.CreateCallbackService(
                ctx,
                EvenementTestFactories.CreateFlexPayCheckMock("1"));

            var result = await service.VerifyAndFinalizeAsync(orderNumber, idSociete);

            Assert.False(result.IsConfirmSuccess);
            Assert.NotNull(result.StatusOnly);
            Assert.True(result.StatusOnly!.Success);
            Assert.False(result.StatusOnly.PaymentPending);
            Assert.Equal(EvenementPaymentStatus.FAILED,
                await ctx.EvenementPayments.Select(p => p.Status).SingleAsync());
            Assert.Equal(EvenementReservationStatus.CANCELLED,
                await ctx.EvenementReservations.Select(r => r.Status).SingleAsync());
        }

        [Fact]
        public async Task VerifyAndFinalizeAsync_throws_when_wrong_societe()
        {
            await using var ctx = BuildDb(nameof(VerifyAndFinalizeAsync_throws_when_wrong_societe));
            var (_, _, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var service = EvenementTestFactories.CreateCallbackService(
                ctx,
                EvenementTestFactories.CreateFlexPayCheckMock("0"));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.VerifyAndFinalizeAsync(orderNumber, idSociete: 99999));
        }

        [Fact]
        public async Task VerifyAndFinalizeAsync_expired_hold_exits_pending()
        {
            await using var ctx = BuildDb(nameof(VerifyAndFinalizeAsync_expired_hold_exits_pending));
            var (idSociete, _, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1, idUtilisateur: 3);
            var reservation = await ctx.EvenementReservations.SingleAsync();
            reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10);
            await ctx.SaveChangesAsync();

            var realtime = new Mock<CongoTravel.Services.Repositories.IFlexPayRealtimeNotifier>();
            var service = EvenementTestFactories.CreateCallbackService(
                ctx,
                EvenementTestFactories.CreateFlexPayCheckMock("2"),
                realtime.Object);

            var result = await service.VerifyAndFinalizeAsync(orderNumber, idSociete);

            Assert.False(result.IsConfirmSuccess);
            Assert.NotNull(result.StatusOnly);
            Assert.False(result.StatusOnly!.PaymentPending);
            Assert.Contains("expiré", result.StatusOnly.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(EvenementPaymentStatus.FAILED,
                await ctx.EvenementPayments.Select(p => p.Status).SingleAsync());
            Assert.Equal(EvenementReservationStatus.CANCELLED,
                await ctx.EvenementReservations.Select(r => r.Status).SingleAsync());
            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    3, orderNumber, It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task VerifyAndFinalizeAsync_cancelled_reservation_pending_payment_exits_pending()
        {
            await using var ctx = BuildDb(
                nameof(VerifyAndFinalizeAsync_cancelled_reservation_pending_payment_exits_pending));
            var (idSociete, _, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var reservation = await ctx.EvenementReservations.SingleAsync();
            reservation.Status = EvenementReservationStatus.CANCELLED;
            reservation.ExpiresAtUtc = null;
            await ctx.SaveChangesAsync();

            var flexPay = EvenementTestFactories.CreateFlexPayCheckMockBuilder("2");
            var service = EvenementTestFactories.CreateCallbackService(ctx, flexPay.Object);

            var result = await service.VerifyAndFinalizeAsync(orderNumber, idSociete);

            Assert.False(result.IsConfirmSuccess);
            Assert.False(result.StatusOnly!.PaymentPending);
            Assert.Equal(EvenementPaymentStatus.FAILED,
                await ctx.EvenementPayments.Select(p => p.Status).SingleAsync());
            flexPay.Verify(
                f => f.VerifierStatutTransactionAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task VerifyAndFinalizeAsync_cancelled_reservation_failed_payment_exits_pending()
        {
            await using var ctx = BuildDb(
                nameof(VerifyAndFinalizeAsync_cancelled_reservation_failed_payment_exits_pending));
            var (idSociete, _, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var reservation = await ctx.EvenementReservations.SingleAsync();
            reservation.Status = EvenementReservationStatus.CANCELLED;
            reservation.ExpiresAtUtc = null;
            var payment = await ctx.EvenementPayments.SingleAsync();
            payment.Status = EvenementPaymentStatus.FAILED;
            await ctx.SaveChangesAsync();

            var flexPay = EvenementTestFactories.CreateFlexPayCheckMockBuilder("2");
            var service = EvenementTestFactories.CreateCallbackService(ctx, flexPay.Object);

            var result = await service.VerifyAndFinalizeAsync(orderNumber, idSociete);

            Assert.False(result.IsConfirmSuccess);
            Assert.False(result.StatusOnly!.PaymentPending);
            Assert.Contains("non confirmé", result.StatusOnly.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(EvenementPaymentStatus.FAILED,
                await ctx.EvenementPayments.Select(p => p.Status).SingleAsync());
            flexPay.Verify(
                f => f.VerifierStatutTransactionAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
