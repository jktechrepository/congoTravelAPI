using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
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
        public async Task ProcessCallbackAsync_marks_payment_failed_on_refusal()
        {
            await using var ctx = BuildDb(nameof(ProcessCallbackAsync_marks_payment_failed_on_refusal));
            var (_, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var service = CreateCallbackService(ctx);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "1",
                OrderNumber = orderNumber
            });

            Assert.True(result.Success);
            Assert.Equal(idReservation, result.IdEvenementReservation);

            var payment = await ctx.EvenementPayments.SingleAsync();
            Assert.Equal(EvenementPaymentStatus.FAILED, payment.Status);

            var reservation = await ctx.EvenementReservations.SingleAsync();
            Assert.Equal(EvenementReservationStatus.HOLD, reservation.Status);
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
            var payment = await ctx.EvenementPayments.SingleAsync();
            Assert.Equal(EvenementPaymentStatus.FAILED, payment.Status);
        }

        private static EvenementFlexPayCallbackService CreateCallbackService(CongoTravelDbContext ctx) =>
            EvenementTestFactories.CreateCallbackService(ctx);
    }
}
