using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class VoyageTarifPatchTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        private static async Task<(CongoTravelDbContext ctx, int idVoyage, int idSociete, int idEco, int idPrem)>
            SeedAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe
            {
                Nom = "Co",
                CodeDevisePrincipale = "CDF",
                DateCreation = DateTime.UtcNow
            };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            ctx.DevisesMonetaires.Add(new DeviseMonetaire
            {
                CodeDevise = "CDF",
                Libelle = "Franc",
                Statut = true,
                IdSociete = societe.IdSociete
            });

            var eco = new CategorieSiege
            {
                IdSociete = societe.IdSociete,
                CodeCategorieSiege = "ECO",
                Libelle = "Eco",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            var prem = new CategorieSiege
            {
                IdSociete = societe.IdSociete,
                CodeCategorieSiege = "PREMIERE",
                Libelle = "Première",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.AddRange(eco, prem);

            var tv = new TypeVehicule
            {
                Libelle = "Std",
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.TypeVehicules.Add(tv);

            var dest = new Destination
            {
                VilleDepart = "A",
                VilleArrivee = "B",
                Montant = 1,
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(dest);

            var site = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "S1",
                NomSite = "Gare",
                NomResponsableSite = "Resp",
                Genre = "Masculin",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(site);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "B1",
                Marques = "X",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 2,
                IdSociete = societe.IdSociete,
                NumeroDePlaque = "AA",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);
            await ctx.SaveChangesAsync();

            var voy = new Voyage
            {
                DateDepart = DateTime.UtcNow.Date.AddDays(2),
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 5000,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 5000,
                IdVehicule = vh.IdVehicule,
                IdDestination = dest.IdDestination,
                IdSociete = societe.IdSociete,
                IdSite = site.IdSite,
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
                    Prix = 5000,
                    IdSociete = societe.IdSociete,
                    DateCreation = DateTime.UtcNow
                },
                new VoyageTarifCategorieSiege
                {
                    IdVoyage = voy.Id,
                    IdCategorieSiege = prem.IdCategorieSiege,
                    Prix = 8000,
                    IdSociete = societe.IdSociete,
                    DateCreation = DateTime.UtcNow
                });
            await ctx.SaveChangesAsync();

            return (ctx, voy.Id, societe.IdSociete, eco.IdCategorieSiege, prem.IdCategorieSiege);
        }

        [Fact]
        public async Task Patch_tarif_single_category_updates_row_and_voyage_prix_reference()
        {
            var db = nameof(Patch_tarif_single_category_updates_row_and_voyage_prix_reference);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (c, idVoyage, idSociete, idEco, _) = await SeedAsync(ctx);

            var tarifSvc = new VoyageTarifService(c);
            var voyageSvc = new VoyageService(
                c,
                NullLogger<VoyageService>.Instance,
                tarifSvc,
                SiegeDisponibiliteTestHelper.Create(c));

            await tarifSvc.UpsertTarifForVoyageAsync(idVoyage, idSociete, idEco, 6500);
            await voyageSvc.SyncVoyagePrixReferenceFromTarifsAsync(idVoyage);

            var ecoRow = await c.VoyageTarifsCategorieSiege.AsNoTracking()
                .FirstAsync(t => t.IdVoyage == idVoyage && t.IdCategorieSiege == idEco);
            Assert.Equal(6500, ecoRow.Prix);

            var voyage = await c.Voyages.AsNoTracking().FirstAsync(v => v.Id == idVoyage);
            Assert.Equal(6500, voyage.Prix);
        }

        [Fact]
        public async Task Patch_tarif_inserts_row_when_missing()
        {
            var db = nameof(Patch_tarif_inserts_row_when_missing);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (c, idVoyage, idSociete, idEco, idPrem) = await SeedAsync(ctx);

            c.VoyageTarifsCategorieSiege.RemoveRange(
                c.VoyageTarifsCategorieSiege.Where(t => t.IdVoyage == idVoyage && t.IdCategorieSiege == idPrem));
            await c.SaveChangesAsync();

            var tarifSvc = new VoyageTarifService(c);
            await tarifSvc.UpsertTarifForVoyageAsync(idVoyage, idSociete, idPrem, 9500);

            var row = await c.VoyageTarifsCategorieSiege.AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdVoyage == idVoyage && t.IdCategorieSiege == idPrem);
            Assert.NotNull(row);
            Assert.Equal(9500, row!.Prix);
        }

        [Fact]
        public async Task HasTarifsForVoyageAsync_returns_true_when_rows_exist()
        {
            var db = nameof(HasTarifsForVoyageAsync_returns_true_when_rows_exist);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (c, idVoyage, _, _, _) = await SeedAsync(ctx);

            var tarifSvc = new VoyageTarifService(c);
            Assert.True(await tarifSvc.HasTarifsForVoyageAsync(idVoyage));
        }
    }
}
