using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementHoldExpirationRunnerTests
    {
        private const string MessageHoldExpire =
            "Hold expiré. Le paiement n’a pas été confirmé à temps.";

        private static CongoTravelDbContext BuildInMemoryDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementHoldExpirationRunner CreateRunner(
            CongoTravelDbContext ctx,
            IFlexPayRealtimeNotifier? realtime = null) =>
            new(
                realtime ?? Mock.Of<IFlexPayRealtimeNotifier>(),
                EvenementTestFactories.CreateReservationService(ctx),
                EvenementTestFactories.CreateCommandeFlexPayService(ctx),
                NullLogger<EvenementHoldExpirationRunner>.Instance);

        [Fact]
        public async Task ExpireHoldsAsync_no_expired_holds_is_noop()
        {
            await using var ctx = BuildInMemoryDb(nameof(ExpireHoldsAsync_no_expired_holds_is_noop));
            var runner = CreateRunner(ctx);

            await runner.ExpireHoldsAsync(ctx);

            Assert.True(true);
        }

        [Fact]
        public async Task ExpireHoldsAsync_skips_when_hold_not_yet_expired()
        {
            await using var ctx = BuildInMemoryDb(nameof(ExpireHoldsAsync_skips_when_hold_not_yet_expired));
            var (_, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1, idUtilisateur: 11);
            var reservation = await ctx.EvenementReservations
                .FirstAsync(r => r.IdEvenementReservation == idReservation);
            reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30);
            await ctx.SaveChangesAsync();

            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            await CreateRunner(ctx, realtime.Object).ExpireHoldsAsync(ctx);

            Assert.Equal(EvenementReservationStatus.HOLD,
                await ctx.EvenementReservations.Select(r => r.Status).SingleAsync());
            Assert.Equal(EvenementPaymentStatus.PENDING,
                await ctx.EvenementPayments.Select(p => p.Status).SingleAsync());
            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
            Assert.Equal(orderNumber, await ctx.EvenementPayments.Select(p => p.ProviderTxRef).SingleAsync());
        }

        [Fact]
        public async Task ExpireHoldsAsync_expired_pending_flexpay_purges_reservation_and_notifies()
        {
            await using var ctx = BuildInMemoryDb(
                nameof(ExpireHoldsAsync_expired_pending_flexpay_purges_reservation_and_notifies));
            var (_, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(
                    ctx, quantity: 1, orderNumber: "FP-EXPIRE-001", idUtilisateur: 42);
            var reservation = await ctx.EvenementReservations
                .FirstAsync(r => r.IdEvenementReservation == idReservation);
            reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-2);
            await ctx.SaveChangesAsync();

            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            await CreateRunner(ctx, realtime.Object).ExpireHoldsAsync(ctx);

            Assert.Empty(await ctx.EvenementReservations.ToListAsync());
            Assert.Empty(await ctx.EvenementPayments.ToListAsync());
            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    42, orderNumber, MessageHoldExpire, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExpireHoldsAsync_expired_without_utilisateur_purges_without_notify()
        {
            await using var ctx = BuildInMemoryDb(
                nameof(ExpireHoldsAsync_expired_without_utilisateur_purges_without_notify));
            var (_, idReservation, _) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var reservation = await ctx.EvenementReservations
                .FirstAsync(r => r.IdEvenementReservation == idReservation);
            reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await ctx.SaveChangesAsync();

            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            await CreateRunner(ctx, realtime.Object).ExpireHoldsAsync(ctx);

            Assert.Empty(await ctx.EvenementPayments.ToListAsync());
            Assert.Empty(await ctx.EvenementReservations.ToListAsync());
            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
        [Fact]
        public async Task ExpireHoldsAsync_expired_plan_a_commande_fails_and_notifies()
        {
            await using var ctx = BuildInMemoryDb(
                nameof(ExpireHoldsAsync_expired_plan_a_commande_fails_and_notifies));

            var (idSociete, idSite, idSession) = await SeedSessionForCommandeAsync(ctx);
            var flexApi = EvenementTestFactories.CreateFlexPayApiMock("FP-CMD-EXPIRE-001");
            var commandeService = EvenementTestFactories.CreateCommandeFlexPayService(ctx, flexApi.Object);
            var initiated = await commandeService.InitiateElectronicAsync(
                new Models.DTOs.Evenement.EvenementReservationWithPaiementRequestDto
                {
                    IdEvenementSession = idSession,
                    Items = new List<Models.DTOs.Evenement.EvenementHoldItemRequestDto> { new() { Quantity = 1 } },
                    Paiement = new Models.DTOs.Evenement.EvenementReservationPaiementBlockDto
                    {
                        MethodePaiement = "MOBILE_MONEY",
                        Phone = "243900000001",
                        IdSite = idSite
                    }
                },
                idSociete,
                idSite);

            var commande = await ctx.EvenementCommandesEnAttente.SingleAsync();
            commande.DateExpiration = DateTime.UtcNow.AddMinutes(-2);
            commande.IdUtilisateur = 77;
            await ctx.SaveChangesAsync();

            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            await CreateRunner(ctx, realtime.Object).ExpireHoldsAsync(ctx);

            Assert.Empty(await ctx.EvenementCommandesEnAttente.ToListAsync());
            Assert.Empty(await ctx.EvenementReservations.ToListAsync());
            Assert.Equal(EvenementPaymentStatus.FAILED,
                await ctx.EvenementPayments.Select(p => p.Status).SingleAsync());
            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    77, initiated.OrderNumber!, MessageHoldExpire, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private static async Task<(int IdSociete, int IdSite, int IdSession)> SeedSessionForCommandeAsync(
            CongoTravelDbContext ctx)
        {
            var (idSociete, idSite, idReservation) =
                await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 1);
            var reservation = await ctx.EvenementReservations.SingleAsync();
            var idSession = reservation.IdEvenementSession;
            await EvenementTestFactories.CreateReservationService(ctx).CancelAsync(idReservation, idSociete);
            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            quota.QuantiteHold = 0;
            quota.QuantiteVendue = 0;
            await ctx.SaveChangesAsync();
            return (idSociete, idSite, idSession);
        }
    }
}
