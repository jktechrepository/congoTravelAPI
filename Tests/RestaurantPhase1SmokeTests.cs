using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Services.Restaurant;
using Xunit;

namespace CongoTravel.Tests
{
    public class RestaurantPhase1SmokeTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public void AddRestaurantReservations_registers_services()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<CongoTravelDbContext>(options =>
                options.UseInMemoryDatabase(nameof(AddRestaurantReservations_registers_services)));
            services.AddRestaurantReservations();

            using var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IRestaurantEtablissementService>());
            Assert.NotNull(provider.GetService<IRestaurantCreneauService>());
        }

        [Fact]
        public async Task Create_and_publish_etablissement()
        {
            await using var ctx = BuildDb(nameof(Create_and_publish_etablissement));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Resto Societe");

            var service = new RestaurantEtablissementService(
                ctx, NullLogger<RestaurantEtablissementService>.Instance);

            var draft = await service.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
            {
                CodeRestaurant = "REST-SMOKE",
                Nom = "Le Congo Grill",
                Adresse = "Gombe",
                AcomptePourcentDefaut = 20m,
                IdSite = idSite
            }, idSociete);

            Assert.Equal("Draft", draft.Status);
            Assert.Equal(idSite, draft.IdSite);

            var published = await service.PublishAsync(draft.IdRestaurant, idSociete);
            Assert.Equal("Published", published.Status);
        }

        [Fact]
        public async Task Create_creneau_draft_with_global_quota()
        {
            await using var ctx = BuildDb(nameof(Create_creneau_draft_with_global_quota));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Resto Creneau");

            var etablissementService = new RestaurantEtablissementService(
                ctx, NullLogger<RestaurantEtablissementService>.Instance);
            var creneauService = new RestaurantCreneauService(
                ctx, NullLogger<RestaurantCreneauService>.Instance);

            var etablissement = await etablissementService.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
            {
                CodeRestaurant = "REST-CRENEAU",
                Nom = "Table Test",
                IdSite = idSite
            }, idSociete);

            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(18);
            var draft = await creneauService.CreateDraftAsync(new RestaurantCreateCreneauRequestDto
            {
                IdRestaurant = etablissement.IdRestaurant,
                DateService = DateOnly.FromDateTime(start),
                StartAtUtc = start,
                EndAtUtc = start.AddHours(2),
                InventoryMode = "GlobalQuota",
                CodeDevise = "USD",
                GlobalQuota = new RestaurantCreateCreneauGlobalQuotaDto
                {
                    CapaciteTotale = 40,
                    PrixUnitaire = 10m
                }
            }, idSociete);

            Assert.Equal("Draft", draft.Status);
            Assert.Equal("GlobalQuota", draft.InventoryMode);
            Assert.NotNull(draft.GlobalQuota);
            Assert.Equal(40, draft.GlobalQuota!.CapaciteTotale);
            Assert.Equal(10m, draft.GlobalQuota.PrixUnitaire);
            Assert.Equal(40, draft.CouvertsRestants);
        }

        [Fact]
        public async Task Publish_creneau_rejects_overlapping_published()
        {
            await using var ctx = BuildDb(nameof(Publish_creneau_rejects_overlapping_published));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Resto Overlap");

            var etablissementService = new RestaurantEtablissementService(
                ctx, NullLogger<RestaurantEtablissementService>.Instance);
            var creneauService = new RestaurantCreneauService(
                ctx, NullLogger<RestaurantCreneauService>.Instance);

            var etablissement = await etablissementService.PublishAsync(
                (await etablissementService.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
                {
                    CodeRestaurant = "REST-OVERLAP",
                    Nom = "Overlap Test",
                    IdSite = idSite
                }, idSociete)).IdRestaurant,
                idSociete);

            var day = DateTime.UtcNow.Date.AddDays(2);
            var startA = day.AddHours(19);
            var startB = day.AddHours(20);

            var first = await creneauService.CreateDraftAsync(new RestaurantCreateCreneauRequestDto
            {
                IdRestaurant = etablissement.IdRestaurant,
                DateService = DateOnly.FromDateTime(day),
                StartAtUtc = startA,
                EndAtUtc = startA.AddHours(2),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new RestaurantCreateCreneauGlobalQuotaDto
                {
                    CapaciteTotale = 20,
                    PrixUnitaire = 5m
                }
            }, idSociete);

            var published = await creneauService.PublishAsync(first.IdRestaurantCreneau, idSociete);
            Assert.Equal("Published", published.Status);

            var overlapping = await creneauService.CreateDraftAsync(new RestaurantCreateCreneauRequestDto
            {
                IdRestaurant = etablissement.IdRestaurant,
                DateService = DateOnly.FromDateTime(day),
                StartAtUtc = startB,
                EndAtUtc = startB.AddHours(2),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new RestaurantCreateCreneauGlobalQuotaDto
                {
                    CapaciteTotale = 15,
                    PrixUnitaire = 5m
                }
            }, idSociete);

            await Assert.ThrowsAsync<RestaurantCreneauConflictException>(() =>
                creneauService.PublishAsync(overlapping.IdRestaurantCreneau, idSociete));
        }
    }
}
