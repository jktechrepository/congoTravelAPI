using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Services.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementClasseServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementClasseService CreateService(CongoTravelDbContext ctx) =>
            new(ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementClasseService>.Instance);

        [Fact]
        public async Task CreateAsync_creates_classe()
        {
            await using var ctx = BuildDb(nameof(CreateAsync_creates_classe));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var result = await service.CreateAsync(new EvenementCreateClasseRequestDto
            {
                CodeClasse = "VIP",
                Libelle = "VIP",
                Description = "Zone VIP"
            }, idSociete);

            Assert.Equal("VIP", result.CodeClasse);
            Assert.True(result.Statut);
        }

        [Fact]
        public async Task CreateAsync_throws_conflict_on_duplicate_code()
        {
            await using var ctx = BuildDb(nameof(CreateAsync_throws_conflict_on_duplicate_code));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);
            var request = new EvenementCreateClasseRequestDto { CodeClasse = "STD", Libelle = "Standard" };

            await service.CreateAsync(request, idSociete);

            await Assert.ThrowsAsync<EvenementClasseConflictException>(() =>
                service.CreateAsync(request, idSociete));
        }

        [Fact]
        public async Task ListAsync_returns_societe_classes_ordered_by_code()
        {
            await using var ctx = BuildDb(nameof(ListAsync_returns_societe_classes_ordered_by_code));
            var idSociete = await SeedSocieteAsync(ctx);
            var otherSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            await service.CreateAsync(new EvenementCreateClasseRequestDto { CodeClasse = "VIP", Libelle = "VIP" }, idSociete);
            await service.CreateAsync(new EvenementCreateClasseRequestDto { CodeClasse = "STD", Libelle = "Standard" }, idSociete);
            await service.CreateAsync(new EvenementCreateClasseRequestDto { CodeClasse = "EXT", Libelle = "Externe" }, otherSociete);

            var list = await service.ListAsync(idSociete);

            Assert.Equal(2, list.Count);
            Assert.Equal("STD", list[0].CodeClasse);
            Assert.Equal("VIP", list[1].CodeClasse);
        }

        [Fact]
        public async Task ListAsync_actifsSeulement_filters_inactive()
        {
            await using var ctx = BuildDb(nameof(ListAsync_actifsSeulement_filters_inactive));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var active = await service.CreateAsync(
                new EvenementCreateClasseRequestDto { CodeClasse = "VIP", Libelle = "VIP" }, idSociete);
            var inactive = await service.CreateAsync(
                new EvenementCreateClasseRequestDto { CodeClasse = "STD", Libelle = "Standard" }, idSociete);
            await service.ToggleStatutAsync(inactive.IdEvenementClasse, idSociete);

            var all = await service.ListAsync(idSociete, actifsSeulement: false);
            var actifs = await service.ListAsync(idSociete, actifsSeulement: true);

            Assert.Equal(2, all.Count);
            Assert.Single(actifs);
            Assert.Equal(active.IdEvenementClasse, actifs[0].IdEvenementClasse);
        }

        [Fact]
        public async Task UpdateAsync_updates_fields()
        {
            await using var ctx = BuildDb(nameof(UpdateAsync_updates_fields));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var created = await service.CreateAsync(
                new EvenementCreateClasseRequestDto { CodeClasse = "VIP", Libelle = "VIP" }, idSociete);

            var updated = await service.UpdateAsync(created.IdEvenementClasse, new EvenementUpdateClasseRequestDto
            {
                Libelle = "VIP Premium",
                Description = "Nouvelle zone",
                Statut = false
            }, idSociete);

            Assert.NotNull(updated);
            Assert.Equal("VIP Premium", updated!.Libelle);
            Assert.Equal("Nouvelle zone", updated.Description);
            Assert.False(updated.Statut);
            Assert.Equal("VIP", updated.CodeClasse);
        }

        [Fact]
        public async Task UpdateAsync_returns_null_for_other_societe()
        {
            await using var ctx = BuildDb(nameof(UpdateAsync_returns_null_for_other_societe));
            var idSociete = await SeedSocieteAsync(ctx);
            var otherSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var created = await service.CreateAsync(
                new EvenementCreateClasseRequestDto { CodeClasse = "VIP", Libelle = "VIP" }, idSociete);

            var result = await service.UpdateAsync(created.IdEvenementClasse, new EvenementUpdateClasseRequestDto
            {
                Libelle = "Hack"
            }, otherSociete);

            Assert.Null(result);
        }

        [Fact]
        public async Task ToggleStatutAsync_flips_statut()
        {
            await using var ctx = BuildDb(nameof(ToggleStatutAsync_flips_statut));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var created = await service.CreateAsync(
                new EvenementCreateClasseRequestDto { CodeClasse = "VIP", Libelle = "VIP" }, idSociete);

            var toggled = await service.ToggleStatutAsync(created.IdEvenementClasse, idSociete);

            Assert.NotNull(toggled);
            Assert.False(toggled!.Statut);

            toggled = await service.ToggleStatutAsync(created.IdEvenementClasse, idSociete);
            Assert.True(toggled!.Statut);
        }

        [Fact]
        public async Task GetByLibelleAsync_returns_match_case_insensitive()
        {
            await using var ctx = BuildDb(nameof(GetByLibelleAsync_returns_match_case_insensitive));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            await service.CreateAsync(new EvenementCreateClasseRequestDto
            {
                CodeClasse = "VIP",
                Libelle = "Zone VIP"
            }, idSociete);

            var found = await service.GetByLibelleAsync("zone vip", idSociete);

            Assert.NotNull(found);
            Assert.Equal("VIP", found!.CodeClasse);
        }

        [Fact]
        public async Task GetByLibelleAsync_returns_null_when_not_found()
        {
            await using var ctx = BuildDb(nameof(GetByLibelleAsync_returns_null_when_not_found));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var found = await service.GetByLibelleAsync("Inexistant", idSociete);

            Assert.Null(found);
        }

        private static async Task<int> SeedSocieteAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Classe Societe", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();
            return societe.IdSociete;
        }
    }

    public class EvenementSessionClassQuotaCreateTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task CreateDraftAsync_creates_class_quota_session()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_creates_class_quota_session));
            var (idSociete, idClasseVip, idClasseStd) = await SeedClassesAsync(ctx);
            var service = new EvenementSessionService(
                ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementSessionService>.Instance);

            var created = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "CONCERT-B",
                Libelle = "Concert mode B",
                StartAtUtc = DateTime.UtcNow.AddDays(7),
                InventoryMode = "ClassQuota",
                ClassQuotas = new List<EvenementCreateSessionClassQuotaDto>
                {
                    new() { IdEvenementClasse = idClasseVip, CapaciteTotale = 50, PrixUnitaire = 100m, CodeDevise = "USD" },
                    new() { IdEvenementClasse = idClasseStd, CapaciteTotale = 200, PrixUnitaire = 30m, CodeDevise = "USD" }
                }
            }, idSociete);

            Assert.Equal("ClassQuota", created.InventoryMode);
            Assert.Equal(2, created.ClassQuotas.Count);
            Assert.Equal("VIP", created.ClassQuotas[0].CodeClasse);
            Assert.Equal(50, created.ClassQuotas[0].CapaciteTotale);
            Assert.Equal(200, created.ClassQuotas[1].QuantiteDisponible);
        }

        [Fact]
        public async Task PublishAsync_publishes_class_quota_session()
        {
            await using var ctx = BuildDb(nameof(PublishAsync_publishes_class_quota_session));
            var (idSociete, idClasse, _) = await SeedClassesAsync(ctx);
            var service = new EvenementSessionService(
                ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementSessionService>.Instance);

            var draft = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "PUB-B",
                Libelle = "Publish B",
                StartAtUtc = DateTime.UtcNow.AddDays(3),
                InventoryMode = "ClassQuota",
                ClassQuotas = new List<EvenementCreateSessionClassQuotaDto>
                {
                    new() { IdEvenementClasse = idClasse, CapaciteTotale = 80, PrixUnitaire = 15m, CodeDevise = "CDF" }
                }
            }, idSociete);

            var published = await service.PublishAsync(draft.IdEvenementSession, idSociete);
            Assert.Equal("Published", published.Status);
        }

        [Fact]
        public async Task CreateDraftAsync_rejects_duplicate_classe_in_class_quotas()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_rejects_duplicate_classe_in_class_quotas));
            var (idSociete, idClasse, _) = await SeedClassesAsync(ctx);
            var service = new EvenementSessionService(
                ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementSessionService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateDraftAsync(new EvenementCreateSessionRequestDto
                {
                    CodeSession = "DUPE-B",
                    Libelle = "Dupe",
                    StartAtUtc = DateTime.UtcNow.AddDays(1),
                    InventoryMode = "ClassQuota",
                    ClassQuotas = new List<EvenementCreateSessionClassQuotaDto>
                    {
                        new() { IdEvenementClasse = idClasse, CapaciteTotale = 10, PrixUnitaire = 5m, CodeDevise = "CDF" },
                        new() { IdEvenementClasse = idClasse, CapaciteTotale = 20, PrixUnitaire = 6m, CodeDevise = "CDF" }
                    }
                }, idSociete));
        }

        private static async Task<(int IdSociete, int IdClasseVip, int IdClasseStd)> SeedClassesAsync(
            CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Session B", DateCreation = DateTime.UtcNow };
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
            return (societe.IdSociete, vip.IdEvenementClasse, std.IdEvenementClasse);
        }
    }
}
