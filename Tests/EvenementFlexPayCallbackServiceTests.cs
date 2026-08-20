using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementFlexPayCallbackServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task ProcessCallbackAsync_confirms_hold_on_success()
        {
            await using var ctx = BuildDb(nameof(ProcessCallbackAsync_confirms_hold_on_success));
            var (idSociete, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 2);
            var service = CreateCallbackService(ctx);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Amount = "40",
                Currency = "USD"
            });

            Assert.True(result.Success);
            Assert.False(result.AlreadyProcessed);
            Assert.Equal(idReservation, result.IdEvenementReservation);

            var reservation = await ctx.EvenementReservations.SingleAsync();
            Assert.Equal(EvenementReservationStatus.CONFIRMED, reservation.Status);
            Assert.Equal(2, await ctx.EvenementTickets.CountAsync());

            var payment = await ctx.EvenementPayments.SingleAsync();
            Assert.Equal(EvenementPaymentStatus.SUCCEEDED, payment.Status);

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(0, quota.QuantiteHold);
            Assert.Equal(2, quota.QuantiteVendue);
        }

        [Fact]
        public async Task ProcessCallbackAsync_is_idempotent_on_second_success()
        {
            await using var ctx = BuildDb(nameof(ProcessCallbackAsync_is_idempotent_on_second_success));
            var (_, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var service = CreateCallbackService(ctx);
            var callback = new FlexPayCallbackDto { Code = "0", OrderNumber = orderNumber, Amount = "20" };

            var first = await service.ProcessCallbackAsync(callback);
            var second = await service.ProcessCallbackAsync(callback);

            Assert.False(first.AlreadyProcessed);
            Assert.True(second.AlreadyProcessed);
            Assert.Equal(1, await ctx.EvenementTickets.CountAsync());
        }

        [Fact]
        public async Task ProcessCallbackAsync_marks_payment_failed_and_releases_hold_on_refusal()
        {
            await using var ctx = BuildDb(nameof(ProcessCallbackAsync_marks_payment_failed_and_releases_hold_on_refusal));
            var (_, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var service = CreateCallbackService(ctx);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "1",
                OrderNumber = orderNumber
            });

            Assert.True(result.Success);
            Assert.False(result.PaymentPending);
            Assert.Equal(idReservation, result.IdEvenementReservation);

            Assert.Empty(await ctx.EvenementPayments.ToListAsync());
            Assert.Empty(await ctx.EvenementReservations.ToListAsync());
            Assert.Equal(0, await ctx.EvenementSessionGlobalQuotas.Select(q => q.QuantiteHold).SingleAsync());
        }

        [Fact]
        public async Task ProcessCallbackAsync_returns_failure_when_payment_not_found()
        {
            await using var ctx = BuildDb(nameof(ProcessCallbackAsync_returns_failure_when_payment_not_found));
            var service = CreateCallbackService(ctx);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = "UNKNOWN-ORDER"
            });

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ProcessCallbackAsync_rejects_currency_mismatch()
        {
            await using var ctx = BuildDb(nameof(ProcessCallbackAsync_rejects_currency_mismatch));
            var (_, _, orderNumber) = await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var service = CreateCallbackService(ctx);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Amount = "20",
                Currency = "CDF"
            });

            Assert.False(result.Success);
            Assert.Contains("devise callback", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessCallbackAsync_rejects_expired_hold_and_marks_payment_failed()
        {
            await using var ctx = BuildDb(nameof(ProcessCallbackAsync_rejects_expired_hold_and_marks_payment_failed));
            var (_, _, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var reservation = await ctx.EvenementReservations.SingleAsync();
            reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5);
            await ctx.SaveChangesAsync();

            var service = CreateCallbackService(ctx);
            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Amount = "20"
            });

            Assert.False(result.Success);
            Assert.Empty(await ctx.EvenementPayments.ToListAsync());
            Assert.Empty(await ctx.EvenementReservations.ToListAsync());
        }

        [Fact]
        public async Task ProcessCallbackAsync_notifies_signalr_on_success()
        {
            await using var ctx = BuildDb(nameof(ProcessCallbackAsync_notifies_signalr_on_success));
            var (_, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1, idUtilisateur: 42);
            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            var service = EvenementTestFactories.CreateCallbackService(ctx, realtimeNotifier: realtime.Object);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Amount = "20",
                Currency = "USD"
            });

            Assert.True(result.Success);
            var idPayment = result.IdEvenementPayment!.Value;
            realtime.Verify(
                n => n.NotifyPaymentConfirmedAsync(
                    42, orderNumber, idReservation, idPayment, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessCallbackAsync_notifies_signalr_on_refusal()
        {
            await using var ctx = BuildDb(nameof(ProcessCallbackAsync_notifies_signalr_on_refusal));
            var (_, _, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1, idUtilisateur: 7);
            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            var service = EvenementTestFactories.CreateCallbackService(ctx, realtimeNotifier: realtime.Object);

            await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "1",
                OrderNumber = orderNumber
            });

            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    7, orderNumber, It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task AbandonPendingPaymentAsync_marks_failed_and_releases_hold()
        {
            await using var ctx = BuildDb(nameof(AbandonPendingPaymentAsync_marks_failed_and_releases_hold));
            var (_, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 2, idUtilisateur: 9);
            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            var service = EvenementTestFactories.CreateCallbackService(ctx, realtimeNotifier: realtime.Object);

            var result = await service.AbandonPendingPaymentAsync(orderNumber, "Paiement annulé.");

            Assert.True(result.Success);
            Assert.False(result.PaymentPending);
            Assert.Empty(await ctx.EvenementPayments.ToListAsync());
            Assert.Empty(await ctx.EvenementReservations.ToListAsync());
            Assert.Equal(0, await ctx.EvenementSessionGlobalQuotas.Select(q => q.QuantiteHold).SingleAsync());
            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    9, orderNumber, "Paiement annulé.", It.IsAny<CancellationToken>()),
                Times.Once);
            Assert.Equal(idReservation, result.IdEvenementReservation);
        }

        /// <summary>
        /// Reproduit le bug prod : réservation déjà trackée sans Lines sur le DbContext partagé
        /// (CancelAsync voyait Lines vide et le catch avalait l’erreur → HOLD restait).
        /// </summary>
        [Fact]
        public async Task AbandonPendingPaymentAsync_releases_hold_when_reservation_pretracked_without_lines()
        {
            await using var ctx = BuildDb(
                nameof(AbandonPendingPaymentAsync_releases_hold_when_reservation_pretracked_without_lines));
            var (_, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);

            // Stub tracké sans Include(Lines) — comme l’ancien AbandonPendingPaymentAsync.
            _ = await ctx.EvenementReservations
                .FirstAsync(r => r.IdEvenementReservation == idReservation);

            var service = EvenementTestFactories.CreateCallbackService(ctx);
            var result = await service.AbandonPendingPaymentAsync(orderNumber, "Paiement annulé.");

            Assert.True(result.Success);
            Assert.False(result.PaymentPending);
            Assert.Empty(await ctx.EvenementReservations.AsNoTracking().ToListAsync());
            Assert.Empty(await ctx.EvenementPayments.AsNoTracking().ToListAsync());
            Assert.Equal(0, await ctx.EvenementSessionGlobalQuotas.Select(q => q.QuantiteHold).SingleAsync());
        }

        private static EvenementFlexPayCallbackService CreateCallbackService(CongoTravelDbContext ctx) =>
            EvenementTestFactories.CreateCallbackService(ctx);
    }
}
