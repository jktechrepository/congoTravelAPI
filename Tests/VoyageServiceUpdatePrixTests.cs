using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>
    /// Validation stricte et cohérence prix voyage / tarifs catégorie.
    /// </summary>
    public class VoyageServiceUpdatePrixTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        private static async Task<(
            CongoTravelDbContext ctx,
            int idVoyage,
            int idSociete,
            int idSite,
            int idEco,
            int idPrem,
            int idSiegeEco,
            int idSiegePrem)> SeedMultiCategoryVoyageAsync(CongoTravelDbContext ctx, int prixVoyage = 5000)
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
                DateDepart = DateTime.UtcNow.Date.AddDays(3),
                HeureDepart = TimeSpan.FromHours(6),
                Prix = prixVoyage,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = prixVoyage,
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

            var sgEco = new Siege
            {
                IdVehicule = vh.IdVehicule,
                NumeroOrdre = 1,
                CodeSiege = "B1/1",
                EstActif = true,
                IdSociete = societe.IdSociete,
                IdCategorieSiege = eco.IdCategorieSiege,
                DateCreation = DateTime.UtcNow
            };
            var sgPrem = new Siege
            {
                IdVehicule = vh.IdVehicule,
                NumeroOrdre = 2,
                CodeSiege = "B1/2",
                EstActif = true,
                IdSociete = societe.IdSociete,
                IdCategorieSiege = prem.IdCategorieSiege,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sieges.AddRange(sgEco, sgPrem);
            await ctx.SaveChangesAsync();

            return (ctx, voy.Id, societe.IdSociete, site.IdSite, eco.IdCategorieSiege, prem.IdCategorieSiege, sgEco.IdSiege, sgPrem.IdSiege);
        }

        private static VoyageService CreateVoyageService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<VoyageService>.Instance, new VoyageTarifService(ctx), SiegeDisponibiliteTestHelper.Create(ctx));

        [Fact]
        public async Task Put_voyage_prix_change_without_tarifs_throws_when_tarifs_exist()
        {
            var db = nameof(Put_voyage_prix_change_without_tarifs_throws_when_tarifs_exist);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (c, idVoyage, _, _, _, _, _, _) = await SeedMultiCategoryVoyageAsync(ctx);

            var voyageSvc = CreateVoyageService(c);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                voyageSvc.EnsurePrixUpdateAllowedAsync(idVoyage, nouveauPrix: 7000, tarifsFournis: false));

            Assert.Contains("catégorie de siège", ex.Message);
        }

        [Fact]
        public async Task Put_voyage_prix_unchanged_without_tarifs_is_allowed()
        {
            var db = nameof(Put_voyage_prix_unchanged_without_tarifs_is_allowed);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (c, idVoyage, _, _, _, _, _, _) = await SeedMultiCategoryVoyageAsync(ctx);

            var voyageSvc = CreateVoyageService(c);
            await voyageSvc.EnsurePrixUpdateAllowedAsync(idVoyage, nouveauPrix: 5000, tarifsFournis: false);
        }

        [Fact]
        public async Task Put_voyage_with_tarifs_array_is_allowed_when_prix_changes()
        {
            var db = nameof(Put_voyage_with_tarifs_array_is_allowed_when_prix_changes);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (c, idVoyage, _, _, _, _, _, _) = await SeedMultiCategoryVoyageAsync(ctx);

            var voyageSvc = CreateVoyageService(c);
            await voyageSvc.EnsurePrixUpdateAllowedAsync(idVoyage, nouveauPrix: 7000, tarifsFournis: true);
        }

        [Fact]
        public async Task Update_prix_without_tarifs_does_not_change_category_tarifs()
        {
            var db = nameof(Update_prix_without_tarifs_does_not_change_category_tarifs);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (c, idVoyage, idSociete, idSite, idEco, idPrem, _, _) =
                await SeedMultiCategoryVoyageAsync(ctx);

            var voyageSvc = CreateVoyageService(c);
            var tarifSvc = new VoyageTarifService(c);
            var original = await c.Voyages.AsNoTracking().FirstAsync(v => v.Id == idVoyage);

            await tarifSvc.UpsertTarifForVoyageAsync(idVoyage, idSociete, idEco, 6000);

            var updatePayload = new Voyage
            {
                Id = idVoyage,
                DateDepart = original.DateDepart,
                HeureDepart = original.HeureDepart,
                Prix = original.Prix,
                CodeDevisePrix = "CDF",
                IdVehicule = original.IdVehicule,
                IdDestination = original.IdDestination,
                IdSociete = idSociete,
                IdSite = idSite,
                Statut = true
            };

            await voyageSvc.UpdateAsync(updatePayload);

            Assert.Equal(6000, await c.VoyageTarifsCategorieSiege.AsNoTracking()
                .Where(t => t.IdVoyage == idVoyage && t.IdCategorieSiege == idEco)
                .Select(t => t.Prix)
                .FirstAsync());
            Assert.Equal(8000, await c.VoyageTarifsCategorieSiege.AsNoTracking()
                .Where(t => t.IdVoyage == idVoyage && t.IdCategorieSiege == idPrem)
                .Select(t => t.Prix)
                .FirstAsync());
        }

        [Fact]
        public async Task Put_tarifs_syncs_voyage_prix_reference_from_eco()
        {
            var db = nameof(Put_tarifs_syncs_voyage_prix_reference_from_eco);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (c, idVoyage, idSociete, _, idEco, idPrem, _, _) =
                await SeedMultiCategoryVoyageAsync(ctx);

            var voyageSvc = CreateVoyageService(c);
            var tarifSvc = new VoyageTarifService(c);

            await tarifSvc.ReplaceTarifsForVoyageAsync(
                idVoyage,
                idSociete,
                new[] { (idEco, 6000), (idPrem, 9000) });

            await voyageSvc.SyncVoyagePrixReferenceFromTarifsAsync(idVoyage);

            var voyage = await c.Voyages.AsNoTracking().FirstAsync(v => v.Id == idVoyage);
            Assert.Equal(6000, voyage.Prix);
            Assert.Equal(6000m, voyage.PrixDevisePrincipale);
        }

        [Fact]
        public async Task Patch_eco_tarif_reservation_matches_voyage_prix()
        {
            var db = nameof(Patch_eco_tarif_reservation_matches_voyage_prix);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (c, idVoyage, idSociete, _, idEco, _, idSiegeEco, _) =
                await SeedMultiCategoryVoyageAsync(ctx);

            var voyageSvc = CreateVoyageService(c);
            var tarifSvc = new VoyageTarifService(c);

            await tarifSvc.UpsertTarifForVoyageAsync(idVoyage, idSociete, idEco, 7000);
            await voyageSvc.SyncVoyagePrixReferenceFromTarifsAsync(idVoyage);

            var voyage = await c.Voyages.AsNoTracking().FirstAsync(v => v.Id == idVoyage);
            var montantEco = await tarifSvc.ComputeTotalForSiegesAsync(
                idVoyage, new[] { idSiegeEco }, voyage.Prix);

            Assert.Equal(7000, voyage.Prix);
            Assert.Equal(7000m, montantEco);
        }
    }
}
