using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementSessionReadTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementSessionService CreateService(CongoTravelDbContext ctx) =>
            new(ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementSessionService>.Instance);

        [Fact]
        public async Task ListAsync_filters_by_status_and_inventory_mode()
        {
            await using var ctx = BuildDb(nameof(ListAsync_filters_by_status_and_inventory_mode));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var draft = await service.CreateDraftAsync(BuildValidCreateRequest("DRAFT-1"), idSociete);
            await service.PublishAsync(draft.IdEvenementSession, idSociete);

            var classSession = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "CLASS-1",
                Libelle = "Session classe",
                StartAtUtc = DateTime.UtcNow.AddDays(20),
                InventoryMode = "ClassQuota",
                ClassQuotas = new List<EvenementCreateSessionClassQuotaDto>
                {
                    new()
                    {
                        IdEvenementClasse = await SeedClasseAsync(ctx, idSociete),
                        CapaciteTotale = 30,
                        PrixUnitaire = 15m,
                        CodeDevise = "CDF"
                    }
                }
            }, idSociete);

            var published = await service.ListAsync(
                idSociete,
                new EvenementSessionListFilter
                {
                    Status = EvenementSessionStatus.Published,
                    InventoryMode = EvenementInventoryMode.GlobalQuota
                });

            Assert.Single(published);
            Assert.Equal("DRAFT-1", published[0].CodeSession);

            var drafts = await service.ListAsync(
                idSociete,
                new EvenementSessionListFilter { Status = EvenementSessionStatus.Draft });

            Assert.Single(drafts);
            Assert.Equal(classSession.IdEvenementSession, drafts[0].IdEvenementSession);
        }

        [Fact]
        public async Task GetByCodeAsync_returns_session_for_own_societe()
        {
            await using var ctx = BuildDb(nameof(GetByCodeAsync_returns_session_for_own_societe));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            await service.CreateDraftAsync(BuildValidCreateRequest("CODE-READ"), idSociete);
            var found = await service.GetByCodeAsync("CODE-READ", idSociete);
            var missing = await service.GetByCodeAsync("CODE-READ", idSociete + 999);

            Assert.NotNull(found);
            Assert.Equal("CODE-READ", found!.CodeSession);
            Assert.Null(missing);
        }

        [Fact]
        public async Task ListByDateRangeAsync_returns_sessions_in_range()
        {
            await using var ctx = BuildDb(nameof(ListByDateRangeAsync_returns_sessions_in_range));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            await service.CreateDraftAsync(BuildValidCreateRequest("FUTURE"), idSociete);

            var oldSession = new EvenementSession
            {
                IdSociete = idSociete,
                CodeSession = "OLD-SESSION",
                Libelle = "Ancienne session",
                StartAtUtc = new DateTime(2026, 1, 15, 18, 0, 0, DateTimeKind.Utc),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Closed,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(oldSession);
            await ctx.SaveChangesAsync();

            var recent = await service.ListByDateRangeAsync(
                DateTime.UtcNow.Date.AddDays(-1),
                DateTime.UtcNow.Date.AddDays(30),
                idSociete);
            var oldOnly = await service.ListByDateRangeAsync(
                new DateTime(2026, 1, 15),
                new DateTime(2026, 1, 15),
                idSociete);

            Assert.Single(recent);
            Assert.Equal("FUTURE", recent[0].CodeSession);
            Assert.Single(oldOnly);
            Assert.Equal("OLD-SESSION", oldOnly[0].CodeSession);
        }

        [Fact]
        public async Task GetByIdAsync_still_returns_null_for_other_societe()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_still_returns_null_for_other_societe));
            var idSociete1 = await SeedSocieteAsync(ctx, "Societe A");
            var idSociete2 = await SeedSocieteAsync(ctx, "Societe B");
            var service = CreateService(ctx);

            var created = await service.CreateDraftAsync(BuildValidCreateRequest("ISO-READ"), idSociete1);
            var other = await service.GetByIdAsync(created.IdEvenementSession, idSociete2);

            Assert.Null(other);
        }

        private static EvenementCreateSessionRequestDto BuildValidCreateRequest(string code) => new()
        {
            CodeSession = code,
            Libelle = "Test session",
            StartAtUtc = DateTime.UtcNow.AddDays(5),
            InventoryMode = "GlobalQuota",
            GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
            {
                CapaciteTotale = 50,
                PrixUnitaire = 10m,
                CodeDevise = "CDF"
            }
        };

        private static async Task<int> SeedSocieteAsync(CongoTravelDbContext ctx, string nom = "Read Societe")
        {
            var societe = new Societe { Nom = nom, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();
            return societe.IdSociete;
        }

        private static async Task<int> SeedClasseAsync(CongoTravelDbContext ctx, int idSociete)
        {
            var classe = new EvenementClasse
            {
                IdSociete = idSociete,
                CodeClasse = "VIP",
                Libelle = "VIP",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementClasses.Add(classe);
            await ctx.SaveChangesAsync();
            return classe.IdEvenementClasse;
        }
    }
}
