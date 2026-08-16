using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Services;
using CongoTravel.Services.SiteTouristique;
using Xunit;

namespace CongoTravel.Tests
{
    public class ReservationIsActifGateTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task Restaurant_CreateHoldAsync_throws_when_reservation_not_actif()
        {
            await using var ctx = BuildDb(nameof(Restaurant_CreateHoldAsync_throws_when_reservation_not_actif));
            var (idSociete, idSite, idCreneau) = await RestaurantTestFactories.SeedPublishedCreneauAsync(
                ctx, "ria");

            var configSvc = new ConfigSocieteService(ctx);
            var config = await configSvc.GetOrCreateAsync(idSociete);
            config.ReservationIsActif = false;
            await ctx.SaveChangesAsync();

            var holdService = RestaurantTestFactories.CreateHoldService(ctx);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                holdService.CreateHoldAsync(
                    idCreneau,
                    idSociete,
                    new RestaurantHoldRequestDto
                    {
                        IdSite = idSite,
                        Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 1 } }
                    }));

            Assert.Equal("La reservation n'est pas Activée pour cette société", ex.Message);
            Assert.Empty(ctx.RestaurantReservations);
        }

        [Fact]
        public async Task SiteTouristique_CreateHoldAsync_throws_when_reservation_not_actif()
        {
            await using var ctx = BuildDb(nameof(SiteTouristique_CreateHoldAsync_throws_when_reservation_not_actif));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "ST RIA");

            var lieuService = SiteTouristiqueTestFactories.CreateLieuService(ctx);
            var journeeService = new SiteTouristiqueJourneeService(ctx, NullLogger<SiteTouristiqueJourneeService>.Instance);

            var lieu = await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = $"RIA-{Guid.NewGuid():N}"[..10],
                Nom = "Lieu RIA",
                IdSite = idSite
            }, idSociete);
            await lieuService.PublishAsync(lieu.IdSiteTouristique, idSociete);

            var journee = await journeeService.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = lieu.IdSiteTouristique,
                DateVisite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                InventoryMode = "GlobalQuota",
                CodeDevise = "USD",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 50,
                    PrixUnitaire = 20m
                }
            }, idSociete);
            await journeeService.PublishAsync(journee.IdSiteTouristiqueJournee, idSociete);

            var config = await ctx.ConfigSocietes.SingleAsync(c => c.IdSociete == idSociete);
            config.ReservationIsActif = false;
            await ctx.SaveChangesAsync();

            var holdService = SiteTouristiqueTestFactories.CreateHoldService(ctx);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                holdService.CreateHoldAsync(
                    journee.IdSiteTouristiqueJournee,
                    idSociete,
                    new SiteTouristiqueHoldRequestDto
                    {
                        Items = new List<SiteTouristiqueHoldItemRequestDto> { new() { Quantity = 1 } }
                    }));

            Assert.Equal("La reservation n'est pas Activée pour cette société", ex.Message);
            Assert.Empty(ctx.SiteTouristiqueReservations);
        }
    }
}
