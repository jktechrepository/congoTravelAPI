using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Evenement.Strategies;
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

        [Fact]
        public async Task Evenement_CreateHoldAsync_throws_when_vente_en_ligne_desactivee()
        {
            await using var ctx = BuildDb(nameof(Evenement_CreateHoldAsync_throws_when_vente_en_ligne_desactivee));
            var (idSociete, idSite) = await EvenementTestFactories.SeedSocieteWithSiteAsync(ctx, "EVT RIA");

            var session = new EvenementSession
            {
                IdSociete = idSociete,
                IdSite = idSite,
                CodeSession = $"RIA-{Guid.NewGuid():N}"[..10],
                Libelle = "Session RIA",
                StartAtUtc = DateTime.UtcNow.AddDays(2),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Published,
                VenteEnLigneActive = false,
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
                PrixUnitaire = 20m,
                CodeDevise = "USD"
            });

            var config = await new ConfigSocieteService(ctx).GetOrCreateAsync(idSociete);
            config.ReservationIsActif = true;
            await ctx.SaveChangesAsync();

            var holdService = new EvenementHoldService(
                ctx,
                new EvenementInventoryHoldStrategyFactory(
                    new EvenementGlobalQuotaHoldStrategy(ctx),
                    new EvenementClassQuotaHoldStrategy(ctx),
                    new EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                NullLogger<EvenementHoldService>.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                holdService.CreateHoldAsync(
                    session.IdEvenementSession,
                    idSociete,
                    new EvenementHoldRequestDto
                    {
                        Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } }
                    }));

            Assert.Contains("Vente en ligne désactivée", ex.Message);
            Assert.Empty(ctx.EvenementReservations);
        }
    }
}
