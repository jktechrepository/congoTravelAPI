using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Evenement.Strategies;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementHoldServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementHoldService CreateService(CongoTravelDbContext ctx)
        {
            var globalStrategy = new EvenementGlobalQuotaHoldStrategy(ctx);
            var factory = new EvenementInventoryHoldStrategyFactory(
                globalStrategy,
                new EvenementClassQuotaHoldStrategy(ctx),
                new EvenementSeatNumberedHoldStrategy(ctx));
            var configRepo = new ConfigSocieteService(ctx);
            return new EvenementHoldService(
                ctx,
                factory,
                configRepo,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementHoldService>.Instance);
        }

        [Fact]
        public async Task CreateHoldAsync_creates_hold_and_increments_quota()
        {
            await using var ctx = BuildDb(nameof(CreateHoldAsync_creates_hold_and_increments_quota));
            var (idSociete, idSession) = await SeedPublishedSessionAsync(ctx, capacity: 20, hold: 0, sold: 0);
            var service = CreateService(ctx);

            var result = await service.CreateHoldAsync(idSession, idSociete, new EvenementHoldRequestDto
            {
                CustomerRef = "CUST-001",
                Items = new List<EvenementHoldItemRequestDto>
                {
                    new() { Quantity = 3 }
                }
            });

            Assert.Equal("HOLD", result.Status);
            Assert.Equal(60m, result.AmountPreview);
            Assert.Equal("USD", result.CodeDevise);
            Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
            Assert.StartsWith("EVT-RES-", result.ReferenceReservation);

            var reservation = await ctx.EvenementReservations
                .Include(r => r.Lines)
                .SingleAsync();
            Assert.Equal(3, reservation.Lines.Single().Quantite);
            Assert.Equal("CUST-001", reservation.CustomerRef);

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(3, quota.QuantiteHold);
        }

        [Fact]
        public async Task CreateHoldAsync_returns_existing_on_idempotency_key()
        {
            await using var ctx = BuildDb(nameof(CreateHoldAsync_returns_existing_on_idempotency_key));
            var (idSociete, idSession) = await SeedPublishedSessionAsync(ctx, capacity: 20, hold: 0, sold: 0);
            var service = CreateService(ctx);
            var request = new EvenementHoldRequestDto
            {
                IdempotencyKey = "idem-key-001",
                Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 2 } }
            };

            var first = await service.CreateHoldAsync(idSession, idSociete, request);
            var second = await service.CreateHoldAsync(idSession, idSociete, request);

            Assert.Equal(first.IdEvenementReservation, second.IdEvenementReservation);
            Assert.Equal(1, await ctx.EvenementReservations.CountAsync());

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(2, quota.QuantiteHold);
        }

        [Fact]
        public async Task CreateHoldAsync_throws_conflict_when_capacity_exceeded()
        {
            await using var ctx = BuildDb(nameof(CreateHoldAsync_throws_conflict_when_capacity_exceeded));
            var (idSociete, idSession) = await SeedPublishedSessionAsync(ctx, capacity: 5, hold: 4, sold: 0);
            var service = CreateService(ctx);

            await Assert.ThrowsAsync<EvenementHoldConflictException>(() =>
                service.CreateHoldAsync(idSession, idSociete, new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 2 } }
                }));
        }

        [Fact]
        public async Task CreateHoldAsync_rejects_draft_session()
        {
            await using var ctx = BuildDb(nameof(CreateHoldAsync_rejects_draft_session));
            var (idSociete, idSession) = await SeedPublishedSessionAsync(
                ctx, capacity: 10, hold: 0, sold: 0, published: false);
            var service = CreateService(ctx);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateHoldAsync(idSession, idSociete, new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } }
                }));
        }

        [Fact]
        public async Task CreateHoldAsync_throws_when_session_not_found_for_societe()
        {
            await using var ctx = BuildDb(nameof(CreateHoldAsync_throws_when_session_not_found_for_societe));
            var (idSociete1, idSession) = await SeedPublishedSessionAsync(ctx, capacity: 10, hold: 0, sold: 0);
            var idSociete2 = await SeedOtherSocieteAsync(ctx);
            var service = CreateService(ctx);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateHoldAsync(idSession, idSociete2, new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } }
                }));
        }

        private static async Task<(int IdSociete, int IdSession)> SeedPublishedSessionAsync(
            CongoTravelDbContext ctx,
            int capacity,
            int hold,
            int sold,
            bool published = true)
        {
            var societe = new Societe { Nom = "EVT Hold Test", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = $"EVT-{Guid.NewGuid():N}"[..12],
                Libelle = "Hold test",
                StartAtUtc = DateTime.UtcNow.AddDays(2),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = published ? EvenementSessionStatus.Published : EvenementSessionStatus.Draft,
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

            return (societe.IdSociete, session.IdEvenementSession);
        }

        private static async Task<int> SeedOtherSocieteAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Other Societe", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();
            return societe.IdSociete;
        }
    }
}
