using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class BilletPricingEnrichmentServiceTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        [Fact]
        public async Task EnrichPrixVoyageAsync_resolves_per_seat_category_without_siege_navigation()
        {
            var db = nameof(EnrichPrixVoyageAsync_resolves_per_seat_category_without_siege_navigation);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var s = new Societe { Nom = "S", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var eco = new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "ECO",
                Libelle = "Eco",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            var vip = new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "VIP",
                Libelle = "VIP",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.AddRange(eco, vip);
            await ctx.SaveChangesAsync();

            var voy = new Voyage
            {
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 99999,
                IdSociete = s.IdSociete,
                IdVehicule = 1,
                IdDestination = 1,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Voyages.Add(voy);
            await ctx.SaveChangesAsync();

            ctx.VoyageTarifsCategorieSiege.AddRange(
                new VoyageTarifCategorieSiege
                {
                    IdVoyage = voy.Id,
                    IdCategorieSiege = eco.IdCategorieSiege,
                    Prix = 1000,
                    IdSociete = s.IdSociete,
                    DateCreation = DateTime.UtcNow
                },
                new VoyageTarifCategorieSiege
                {
                    IdVoyage = voy.Id,
                    IdCategorieSiege = vip.IdCategorieSiege,
                    Prix = 15000,
                    IdSociete = s.IdSociete,
                    DateCreation = DateTime.UtcNow
                });
            await ctx.SaveChangesAsync();

            var siegeEco = new Siege
            {
                IdVehicule = 1,
                NumeroOrdre = 1,
                CodeSiege = "ECO/1",
                EstActif = true,
                IdSociete = s.IdSociete,
                IdCategorieSiege = eco.IdCategorieSiege,
                DateCreation = DateTime.UtcNow
            };
            var siegeVip = new Siege
            {
                IdVehicule = 1,
                NumeroOrdre = 2,
                CodeSiege = "VIP/1",
                EstActif = true,
                IdSociete = s.IdSociete,
                IdCategorieSiege = vip.IdCategorieSiege,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sieges.AddRange(siegeEco, siegeVip);
            await ctx.SaveChangesAsync();

            var reservation = new Reservation { IdVoyage = voy.Id, Voyage = voy };
            var bEco = new Billet
            {
                IdBillet = 1,
                IdSiege = siegeEco.IdSiege,
                Siege = null,
                Reservation = reservation
            };
            var bVip = new Billet
            {
                IdBillet = 2,
                IdSiege = siegeVip.IdSiege,
                Siege = null,
                Reservation = reservation
            };

            var dtoEco = new BilletResponseDto { IdBillet = 1, PrixVoyage = 99999 };
            var dtoVip = new BilletResponseDto { IdBillet = 2, PrixVoyage = 99999 };

            var svc = new BilletPricingEnrichmentService(ctx, new VoyageTarifService(ctx));
            await svc.EnrichPrixVoyageAsync(new[] { bEco, bVip }, new List<BilletResponseDto> { dtoEco, dtoVip });

            Assert.Equal(1000, dtoEco.PrixVoyage);
            Assert.Equal(15000, dtoVip.PrixVoyage);
            Assert.NotEqual(dtoEco.PrixVoyage, dtoVip.PrixVoyage);
            Assert.NotEqual(16000, dtoEco.PrixVoyage);
            Assert.NotEqual(16000, dtoVip.PrixVoyage);
        }
    }
}
