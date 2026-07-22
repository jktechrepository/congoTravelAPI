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
    public class EvenementClassQuotaHoldStrategyTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public void ValidateAndAggregateItems_aggregates_same_class_and_rejects_seat()
        {
            var aggregated = EvenementClassQuotaHoldStrategy.ValidateAndAggregateItems(new[]
            {
                new EvenementHoldItemRequestDto { ClassId = 2, Quantity = 2 },
                new EvenementHoldItemRequestDto { ClassId = 2, Quantity = 1 },
                new EvenementHoldItemRequestDto { ClassId = 5, Quantity = 4 }
            });

            Assert.Equal(3, aggregated[2]);
            Assert.Equal(4, aggregated[5]);

            Assert.Throws<InvalidOperationException>(() =>
                EvenementClassQuotaHoldStrategy.ValidateAndAggregateItems(new[]
                {
                    new EvenementHoldItemRequestDto { SeatId = 1, ClassId = 2, Quantity = 1 }
                }));
        }

        [Fact]
        public async Task ReserveHoldAsync_increments_class_quota_for_single_class()
        {
            await using var ctx = BuildDb(nameof(ReserveHoldAsync_increments_class_quota_for_single_class));
            var session = await SeedPublishedClassSessionAsync(ctx, vipCapacity: 10, stdCapacity: 20);

            var strategy = new EvenementClassQuotaHoldStrategy(ctx);
            var result = await strategy.ReserveHoldAsync(new EvenementInventoryHoldRequest
            {
                Session = session,
                Items = new[] { new EvenementHoldItemRequestDto { ClassId = session.ClassQuotas.First().IdEvenementClasse, Quantity = 4 } }
            });

            Assert.Single(result.Lines);
            Assert.Equal(EvenementReservationLineType.ClassQuota, result.Lines[0].LineType);
            Assert.Equal(200m, result.MontantSousTotal);

            var vipQuota = session.ClassQuotas.First();
            var persisted = await ctx.EvenementSessionClassQuotas
                .SingleAsync(q => q.IdEvenementSessionClassQuota == vipQuota.IdEvenementSessionClassQuota);
            Assert.Equal(4, persisted.QuantiteHold);
        }

        [Fact]
        public async Task ReserveHoldAsync_supports_multi_class_items()
        {
            await using var ctx = BuildDb(nameof(ReserveHoldAsync_supports_multi_class_items));
            var session = await SeedPublishedClassSessionAsync(ctx, vipCapacity: 10, stdCapacity: 20);
            var vipClassId = session.ClassQuotas.First(q => q.PrixUnitaire == 50m).IdEvenementClasse;
            var stdClassId = session.ClassQuotas.First(q => q.PrixUnitaire == 15m).IdEvenementClasse;

            var strategy = new EvenementClassQuotaHoldStrategy(ctx);
            var result = await strategy.ReserveHoldAsync(new EvenementInventoryHoldRequest
            {
                Session = session,
                Items = new[]
                {
                    new EvenementHoldItemRequestDto { ClassId = vipClassId, Quantity = 2 },
                    new EvenementHoldItemRequestDto { ClassId = stdClassId, Quantity = 3 }
                }
            });

            Assert.Equal(2, result.Lines.Count);
            Assert.Equal(145m, result.MontantSousTotal);
            Assert.All(result.Lines, l => Assert.Equal(EvenementReservationLineType.ClassQuota, l.LineType));
        }

        [Fact]
        public async Task ReserveHoldAsync_throws_conflict_when_class_capacity_exceeded()
        {
            await using var ctx = BuildDb(nameof(ReserveHoldAsync_throws_conflict_when_class_capacity_exceeded));
            var session = await SeedPublishedClassSessionAsync(ctx, vipCapacity: 5, stdCapacity: 20, vipHold: 4);

            var strategy = new EvenementClassQuotaHoldStrategy(ctx);
            var vipClassId = session.ClassQuotas.First().IdEvenementClasse;

            await Assert.ThrowsAsync<EvenementHoldConflictException>(() =>
                strategy.ReserveHoldAsync(new EvenementInventoryHoldRequest
                {
                    Session = session,
                    Items = new[] { new EvenementHoldItemRequestDto { ClassId = vipClassId, Quantity = 2 } }
                }));
        }

        [Fact]
        public async Task CreateHoldAsync_creates_class_quota_hold_via_service()
        {
            await using var ctx = BuildDb(nameof(CreateHoldAsync_creates_class_quota_hold_via_service));
            var (idSociete, idSession, vipClassId, stdClassId) = await SeedPublishedClassSessionForHoldAsync(ctx);
            var holdService = new EvenementHoldService(
                ctx,
                new EvenementInventoryHoldStrategyFactory(
                    new EvenementGlobalQuotaHoldStrategy(ctx),
                    new EvenementClassQuotaHoldStrategy(ctx),
                    new EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementHoldService>.Instance);

            var hold = await holdService.CreateHoldAsync(idSession, idSociete, new EvenementHoldRequestDto
            {
                Items = new List<EvenementHoldItemRequestDto>
                {
                    new() { ClassId = vipClassId, Quantity = 2 },
                    new() { ClassId = stdClassId, Quantity = 1 }
                }
            });

            Assert.Equal(115m, hold.AmountPreview);
            Assert.Equal(2, await ctx.EvenementReservationLines.CountAsync());
            Assert.All(await ctx.EvenementReservationLines.ToListAsync(),
                l => Assert.Equal(EvenementReservationLineType.ClassQuota, l.LineType));
        }

        private static async Task<EvenementSession> SeedPublishedClassSessionAsync(
            CongoTravelDbContext ctx,
            int vipCapacity,
            int stdCapacity,
            int vipHold = 0,
            int stdHold = 0)
        {
            var societe = new Societe { Nom = "Class Hold", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var vip = new EvenementClasse
            {
                IdSociete = societe.IdSociete,
                CodeClasse = "VIP",
                Libelle = "VIP",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            var std = new EvenementClasse
            {
                IdSociete = societe.IdSociete,
                CodeClasse = "STD",
                Libelle = "Standard",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementClasses.AddRange(vip, std);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = "CLASS-HOLD",
                Libelle = "Class hold",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = EvenementInventoryMode.ClassQuota,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var vipQuota = new EvenementSessionClassQuota
            {
                IdEvenementSession = session.IdEvenementSession,
                IdEvenementClasse = vip.IdEvenementClasse,
                CapaciteTotale = vipCapacity,
                QuantiteHold = vipHold,
                QuantiteVendue = 0,
                PrixUnitaire = 50m,
                CodeDevise = "USD"
            };
            var stdQuota = new EvenementSessionClassQuota
            {
                IdEvenementSession = session.IdEvenementSession,
                IdEvenementClasse = std.IdEvenementClasse,
                CapaciteTotale = stdCapacity,
                QuantiteHold = stdHold,
                QuantiteVendue = 0,
                PrixUnitaire = 15m,
                CodeDevise = "USD"
            };
            ctx.EvenementSessionClassQuotas.AddRange(vipQuota, stdQuota);
            await ctx.SaveChangesAsync();

            session.ClassQuotas.Add(vipQuota);
            session.ClassQuotas.Add(stdQuota);
            return session;
        }

        private static async Task<(int IdSociete, int IdSession, int VipClassId, int StdClassId)> SeedPublishedClassSessionForHoldAsync(
            CongoTravelDbContext ctx)
        {
            var session = await SeedPublishedClassSessionAsync(ctx, vipCapacity: 20, stdCapacity: 30);
            var vipClassId = session.ClassQuotas.First(q => q.PrixUnitaire == 50m).IdEvenementClasse;
            var stdClassId = session.ClassQuotas.First(q => q.PrixUnitaire == 15m).IdEvenementClasse;
            return (session.IdSociete, session.IdEvenementSession, vipClassId, stdClassId);
        }
    }
}
