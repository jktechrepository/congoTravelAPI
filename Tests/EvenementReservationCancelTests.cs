using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Evenement.Strategies;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementReservationCancelTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementReservationService CreateCancelService(
            CongoTravelDbContext ctx,
            IFlexPayRealtimeNotifier? realtimeNotifier = null) =>
            new(
                ctx,
                new EvenementInventoryCancelStrategyFactory(
                    new EvenementGlobalQuotaCancelStrategy(ctx),
                    new EvenementClassQuotaCancelStrategy(ctx),
                    new EvenementSeatNumberedCancelStrategy(ctx)),
                realtimeNotifier ?? Mock.Of<IFlexPayRealtimeNotifier>(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementReservationService>.Instance);

        private static EvenementHoldService CreateHoldService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new EvenementInventoryHoldStrategyFactory(
                    new EvenementGlobalQuotaHoldStrategy(ctx),
                    new EvenementClassQuotaHoldStrategy(ctx),
                    new EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementHoldService>.Instance);

        private static EvenementPaymentService CreatePaymentService(CongoTravelDbContext ctx) =>
            EvenementTestFactories.CreatePaymentService(ctx);

        [Fact]
        public async Task CancelAsync_releases_hold_and_cancels_reservation()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_releases_hold_and_cancels_reservation));
            var (idSociete, idReservation) = await SeedHoldReservationAsync(ctx, quantity: 2);
            var service = CreateCancelService(ctx);

            var result = await service.CancelAsync(idReservation, idSociete);

            Assert.False(result.AlreadyCancelled);
            Assert.Equal("CANCELLED", result.Reservation.Status);
            Assert.Equal(0, result.TicketsVoided);

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(0, quota.QuantiteHold);
            Assert.Empty(await ctx.EvenementReservations.ToListAsync());
        }

        [Fact]
        public async Task CancelAsync_voids_tickets_and_releases_sold_on_confirmed_reservation()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_voids_tickets_and_releases_sold_on_confirmed_reservation));
            var (idSociete, idReservation) = await SeedConfirmedReservationAsync(ctx, quantity: 2);
            var service = CreateCancelService(ctx);

            var result = await service.CancelAsync(idReservation, idSociete);

            Assert.False(result.AlreadyCancelled);
            Assert.Equal("CANCELLED", result.Reservation.Status);
            Assert.Equal(2, result.TicketsVoided);
            Assert.All(result.Reservation.Tickets, t => Assert.Equal("VOID", t.Status));
            Assert.Equal("REFUNDED", result.Reservation.Payments.Single().Status);

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(0, quota.QuantiteVendue);
        }

        [Fact]
        public async Task CancelAsync_is_idempotent_when_already_cancelled()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_is_idempotent_when_already_cancelled));
            var (idSociete, idReservation) = await SeedConfirmedReservationAsync(ctx, quantity: 1);
            var service = CreateCancelService(ctx);

            await service.CancelAsync(idReservation, idSociete);
            var second = await service.CancelAsync(idReservation, idSociete);

            Assert.True(second.AlreadyCancelled);
            Assert.Equal(0, second.TicketsVoided);
            Assert.Equal("CANCELLED",
                await ctx.EvenementReservations.Select(r => r.Status.ToString()).SingleAsync());
        }

        [Fact]
        public async Task CancelAsync_rejects_confirmed_reservation_with_used_ticket()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_rejects_confirmed_reservation_with_used_ticket));
            var (idSociete, idReservation) = await SeedConfirmedReservationAsync(ctx, quantity: 1, markUsed: true);
            var service = CreateCancelService(ctx);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CancelAsync(idReservation, idSociete));
        }

        [Fact]
        public async Task CancelAsync_rejects_expired_reservation()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_rejects_expired_reservation));
            var (idSociete, idReservation) = await SeedExpiredReservationAsync(ctx);
            var service = CreateCancelService(ctx);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CancelAsync(idReservation, idSociete));
        }

        [Fact]
        public async Task CancelAsync_hold_with_pending_flexpay_purges_and_notifies()
        {
            await using var ctx = BuildDb(
                nameof(CancelAsync_hold_with_pending_flexpay_purges_and_notifies));
            var (idSociete, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1, idUtilisateur: 11);
            var realtime = new Mock<IFlexPayRealtimeNotifier>();
            var service = CreateCancelService(ctx, realtime.Object);

            var result = await service.CancelAsync(idReservation, idSociete);

            Assert.Equal("CANCELLED", result.Reservation.Status);
            Assert.Empty(await ctx.EvenementReservations.ToListAsync());
            Assert.Empty(await ctx.EvenementPayments.ToListAsync());
            Assert.Equal(0, await ctx.EvenementSessionGlobalQuotas.Select(q => q.QuantiteHold).SingleAsync());
            realtime.Verify(
                n => n.NotifyPaymentFailedAsync(
                    11, orderNumber, "Paiement annulé.", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private static async Task<(int IdSociete, int IdReservation)> SeedHoldReservationAsync(
            CongoTravelDbContext ctx,
            int quantity)
        {
            var (idSociete, idSession) = await SeedPublishedSessionAsync(ctx);
            var hold = await CreateHoldService(ctx).CreateHoldAsync(
                idSession,
                idSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = quantity } }
                });

            return (idSociete, hold.IdEvenementReservation);
        }

        private static async Task<(int IdSociete, int IdReservation)> SeedConfirmedReservationAsync(
            CongoTravelDbContext ctx,
            int quantity,
            bool markUsed = false)
        {
            var (idSociete, idReservation) = await SeedHoldReservationAsync(ctx, quantity);
            await CreatePaymentService(ctx).ConfirmPaymentAsync(
                idReservation,
                idSociete,
                new EvenementConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            if (markUsed)
            {
                var ticket = await ctx.EvenementTickets.SingleAsync();
                ticket.Status = EvenementTicketStatus.USED;
                ticket.UsedAtUtc = DateTime.UtcNow;
                await ctx.SaveChangesAsync();
            }

            return (idSociete, idReservation);
        }

        private static async Task<(int IdSociete, int IdReservation)> SeedExpiredReservationAsync(
            CongoTravelDbContext ctx)
        {
            var idSociete = await SeedSocieteAsync(ctx);
            var (idSociete2, idSession) = await SeedPublishedSessionAsync(ctx, idSociete);

            var reservation = new EvenementReservation
            {
                IdSociete = idSociete2,
                IdEvenementSession = idSession,
                ReferenceReservation = "EVT-RES-EXP",
                Status = EvenementReservationStatus.EXPIRED,
                MontantSousTotal = 10m,
                CodeDevise = "CDF",
                DateCreation = DateTime.UtcNow,
                Lines =
                {
                    new EvenementReservationLine
                    {
                        LineType = EvenementReservationLineType.GlobalQuota,
                        Quantite = 1,
                        PrixUnitaire = 10m,
                        CodeDevise = "CDF"
                    }
                }
            };
            ctx.EvenementReservations.Add(reservation);
            await ctx.SaveChangesAsync();
            return (idSociete2, reservation.IdEvenementReservation);
        }

        private static async Task<(int IdSociete, int IdSession)> SeedPublishedSessionAsync(
            CongoTravelDbContext ctx,
            int? idSociete = null)
        {
            var societeId = idSociete ?? await SeedSocieteAsync(ctx);
            var session = new EvenementSession
            {
                IdSociete = societeId,
                CodeSession = $"CANCEL-{Guid.NewGuid():N}"[..12],
                Libelle = "Cancel test",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            ctx.EvenementSessionGlobalQuotas.Add(new EvenementSessionGlobalQuota
            {
                IdEvenementSession = session.IdEvenementSession,
                CapaciteTotale = 50,
                QuantiteHold = 0,
                QuantiteVendue = 0,
                PrixUnitaire = 10m,
                CodeDevise = "CDF"
            });
            await ctx.SaveChangesAsync();

            return (societeId, session.IdEvenementSession);
        }

        private static async Task<int> SeedSocieteAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Cancel Societe", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();
            return societe.IdSociete;
        }
    }
}
