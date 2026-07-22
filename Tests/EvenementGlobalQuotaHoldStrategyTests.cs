using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement.Strategies;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementGlobalQuotaHoldStrategyTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public void ValidateAndSumItems_rejects_seat_or_class_on_global_mode()
        {
            Assert.Throws<InvalidOperationException>(() =>
                EvenementGlobalQuotaHoldStrategy.ValidateAndSumItems(new[]
                {
                    new EvenementHoldItemRequestDto { SeatId = 1, Quantity = 1 }
                }));

            Assert.Throws<InvalidOperationException>(() =>
                EvenementGlobalQuotaHoldStrategy.ValidateAndSumItems(new[]
                {
                    new EvenementHoldItemRequestDto { ClassId = 2, Quantity = 1 }
                }));
        }

        [Fact]
        public void ValidateAndSumItems_sums_quantities()
        {
            var total = EvenementGlobalQuotaHoldStrategy.ValidateAndSumItems(new[]
            {
                new EvenementHoldItemRequestDto { Quantity = 2 },
                new EvenementHoldItemRequestDto { Quantity = 3 }
            });

            Assert.Equal(5, total);
        }

        [Fact]
        public async Task ReserveHoldAsync_increments_quantite_hold_when_capacity_allows()
        {
            await using var ctx = BuildDb(nameof(ReserveHoldAsync_increments_quantite_hold_when_capacity_allows));
            var session = await SeedPublishedGlobalSessionAsync(ctx, capacity: 10, hold: 0, sold: 0);

            var strategy = new EvenementGlobalQuotaHoldStrategy(ctx);
            var result = await strategy.ReserveHoldAsync(new EvenementInventoryHoldRequest
            {
                Session = session,
                Items = new[] { new EvenementHoldItemRequestDto { Quantity = 4 } },
                PrixUnitaire = 25m,
                CodeDevise = "USD"
            });

            Assert.Equal(100m, result.MontantSousTotal);
            Assert.Single(result.Lines);
            Assert.Equal(EvenementReservationLineType.GlobalQuota, result.Lines[0].LineType);

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(4, quota.QuantiteHold);
        }

        [Fact]
        public async Task ReserveHoldAsync_throws_conflict_when_capacity_exceeded()
        {
            await using var ctx = BuildDb(nameof(ReserveHoldAsync_throws_conflict_when_capacity_exceeded));
            var session = await SeedPublishedGlobalSessionAsync(ctx, capacity: 10, hold: 8, sold: 0);

            var strategy = new EvenementGlobalQuotaHoldStrategy(ctx);

            await Assert.ThrowsAsync<EvenementHoldConflictException>(() =>
                strategy.ReserveHoldAsync(new EvenementInventoryHoldRequest
                {
                    Session = session,
                    Items = new[] { new EvenementHoldItemRequestDto { Quantity = 3 } },
                    PrixUnitaire = 10m
                }));
        }

        [Fact]
        public void Factory_returns_global_strategy_only_for_mode_c()
        {
            using var ctx = BuildDb(nameof(Factory_returns_global_strategy_only_for_mode_c));
            var factory = new EvenementInventoryHoldStrategyFactory(
                new EvenementGlobalQuotaHoldStrategy(ctx),
                new EvenementClassQuotaHoldStrategy(ctx),
                new EvenementSeatNumberedHoldStrategy(ctx));

            Assert.IsType<EvenementGlobalQuotaHoldStrategy>(
                factory.GetStrategy(EvenementInventoryMode.GlobalQuota));

            Assert.IsType<EvenementClassQuotaHoldStrategy>(
                factory.GetStrategy(EvenementInventoryMode.ClassQuota));
        }

        private static async Task<EvenementSession> SeedPublishedGlobalSessionAsync(
            CongoTravelDbContext ctx,
            int capacity,
            int hold,
            int sold)
        {
            var societe = new Societe { Nom = "Test EVT", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = "EVT-GLOBAL-TEST",
                Libelle = "Test",
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
                CapaciteTotale = capacity,
                QuantiteHold = hold,
                QuantiteVendue = sold,
                PrixUnitaire = 20m,
                CodeDevise = "USD"
            });
            await ctx.SaveChangesAsync();

            return session;
        }
    }
}
