using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models.Evenement.Enums;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementPurgeNeverConfirmedTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task PurgeNeverConfirmedAsync_refuses_when_payment_succeeded()
        {
            await using var ctx = BuildDb(nameof(PurgeNeverConfirmedAsync_refuses_when_payment_succeeded));
            var (idSociete, idReservation, _) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);

            var payment = await ctx.EvenementPayments.SingleAsync();
            payment.Status = EvenementPaymentStatus.SUCCEEDED;
            var reservation = await ctx.EvenementReservations.SingleAsync();
            reservation.Status = EvenementReservationStatus.CONFIRMED;
            await ctx.SaveChangesAsync();

            var service = EvenementTestFactories.CreateReservationService(ctx);
            var purged = await service.PurgeNeverConfirmedAsync(idReservation, idSociete);

            Assert.False(purged);
            Assert.Single(await ctx.EvenementReservations.ToListAsync());
            Assert.Single(await ctx.EvenementPayments.ToListAsync());
        }

        [Fact]
        public async Task PurgeNeverConfirmedAsync_deletes_cancelled_without_succeeded_payment()
        {
            await using var ctx = BuildDb(nameof(PurgeNeverConfirmedAsync_deletes_cancelled_without_succeeded_payment));
            var (idSociete, idReservation, _) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);

            var payment = await ctx.EvenementPayments.SingleAsync();
            payment.Status = EvenementPaymentStatus.FAILED;
            var reservation = await ctx.EvenementReservations.SingleAsync();
            reservation.Status = EvenementReservationStatus.CANCELLED;
            await ctx.SaveChangesAsync();

            var service = EvenementTestFactories.CreateReservationService(ctx);
            var purged = await service.PurgeNeverConfirmedAsync(idReservation, idSociete);

            Assert.True(purged);
            Assert.Empty(await ctx.EvenementReservations.ToListAsync());
            Assert.Empty(await ctx.EvenementPayments.ToListAsync());
        }
    }
}
