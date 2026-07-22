using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using System.Linq;
using Xunit;

namespace CongoTravel.Tests
{
    public class VoyageServiceResponseTarifsTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(db)
                .Options;

        [Fact]
        public async Task GetByIdAsync_loads_tarifs_categorie_siege()
        {
            var db = nameof(GetByIdAsync_loads_tarifs_categorie_siege);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var societe = new Societe { Nom = "S", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var typeVehicule = new TypeVehicule { Libelle = "Bus", IdSociete = societe.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(typeVehicule);
            var destination = new Destination
            {
                VilleDepart = "A",
                VilleArrivee = "B",
                Montant = 1,
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(destination);
            var site = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "S1",
                NomSite = "Site 1",
                NomResponsableSite = "Resp",
                Genre = "Masculin",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(site);
            await ctx.SaveChangesAsync();

            var vehicule = new Vehicule
            {
                AliasVehicule = "BUS1",
                Marques = "M",
                IdTypeVehicule = typeVehicule.IdTypeVehicule,
                NombreSiege = 2,
                IdSociete = societe.IdSociete,
                NumeroDePlaque = "AA-1",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vehicule);
            await ctx.SaveChangesAsync();

            var voyage = new Voyage
            {
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 1000,
                CodeDevisePrix = "CDF",
                IdVehicule = vehicule.IdVehicule,
                IdDestination = destination.IdDestination,
                IdSociete = societe.IdSociete,
                IdSite = site.IdSite,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Voyages.Add(voyage);

            var categorie = new CategorieSiege
            {
                IdSociete = societe.IdSociete,
                CodeCategorieSiege = "ECO",
                Libelle = "Eco",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.Add(categorie);
            await ctx.SaveChangesAsync();

            ctx.VoyageTarifsCategorieSiege.Add(new VoyageTarifCategorieSiege
            {
                IdVoyage = voyage.Id,
                IdCategorieSiege = categorie.IdCategorieSiege,
                Prix = 1000,
                IdSociete = societe.IdSociete,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var service = new VoyageService(
                ctx,
                NullLogger<VoyageService>.Instance,
                new Mock<IVoyageTarifService>().Object,
                SiegeDisponibiliteTestHelper.Create(ctx));

            var loaded = await service.GetByIdAsync(voyage.Id);

            Assert.NotNull(loaded);
            Assert.NotNull(loaded!.VoyageTarifsCategorieSiege);
            Assert.Single(loaded.VoyageTarifsCategorieSiege!);
            Assert.Equal(categorie.IdCategorieSiege, loaded.VoyageTarifsCategorieSiege!.First().IdCategorieSiege);
        }
    }
}
