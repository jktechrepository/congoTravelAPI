using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class VoyageTarifServiceTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        private static async Task<(CongoTravelDbContext ctx, int idVoyage, int idEco, int idPrem, int idSiegeEco, int idSiegePrem)>
            SeedVoyageTwoCategoriesAsync(CongoTravelDbContext ctx)
        {
            var s = new Societe { Nom = "Co", DateCreation = DateTime.UtcNow };
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
            var prem = new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "PREMIERE",
                Libelle = "Première",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.AddRange(eco, prem);
            await ctx.SaveChangesAsync();

            var tv = new TypeVehicule { Libelle = "Std", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "B1",
                Marques = "X",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 2,
                IdSociete = s.IdSociete,
                NumeroDePlaque = "AA",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);
            var dest = new Destination
            {
                VilleDepart = "A",
                VilleArrivee = "B",
                Montant = 1,
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(dest);
            await ctx.SaveChangesAsync();

            var voy = new Voyage
            {
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(6),
                Prix = 9999,
                IdVehicule = vh.IdVehicule,
                IdDestination = dest.IdDestination,
                IdSociete = s.IdSociete,
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
                    IdCategorieSiege = prem.IdCategorieSiege,
                    Prix = 2500,
                    IdSociete = s.IdSociete,
                    DateCreation = DateTime.UtcNow
                });
            await ctx.SaveChangesAsync();

            var sg1 = new Siege
            {
                IdVehicule = vh.IdVehicule,
                NumeroOrdre = 1,
                CodeSiege = "B1/1",
                EstActif = true,
                IdSociete = s.IdSociete,
                IdCategorieSiege = eco.IdCategorieSiege,
                DateCreation = DateTime.UtcNow
            };
            var sg2 = new Siege
            {
                IdVehicule = vh.IdVehicule,
                NumeroOrdre = 2,
                CodeSiege = "B1/2",
                EstActif = true,
                IdSociete = s.IdSociete,
                IdCategorieSiege = prem.IdCategorieSiege,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sieges.AddRange(sg1, sg2);
            await ctx.SaveChangesAsync();

            return (ctx, voy.Id, eco.IdCategorieSiege, prem.IdCategorieSiege, sg1.IdSiege, sg2.IdSiege);
        }

        [Fact]
        public async Task ResolvePrixAsync_returns_configured_tarif_when_row_exists()
        {
            var db = nameof(ResolvePrixAsync_returns_configured_tarif_when_row_exists);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (_, idVoyage, idEco, _, _, _) = await SeedVoyageTwoCategoriesAsync(ctx);

            var svc = new VoyageTarifService(ctx);
            var prix = await svc.ResolvePrixAsync(idVoyage, idEco, prixFallbackVoyage: 9999);

            Assert.Equal(1000, prix);
        }

        [Fact]
        public async Task ResolvePrixAsync_uses_fallback_when_no_tarif_row()
        {
            var db = nameof(ResolvePrixAsync_uses_fallback_when_no_tarif_row);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (_, idVoyage, _, idPrem, _, _) = await SeedVoyageTwoCategoriesAsync(ctx);

            var svc = new VoyageTarifService(ctx);
            // Catégorie inexistante pour ce voyage : pas de ligne tarif
            var fakeCatId = idPrem + 99999;
            var prix = await svc.ResolvePrixAsync(idVoyage, fakeCatId, prixFallbackVoyage: 42);

            Assert.Equal(42, prix);
        }

        [Fact]
        public async Task ComputeTotalForSiegesAsync_sums_tarifs_per_seat_category()
        {
            var db = nameof(ComputeTotalForSiegesAsync_sums_tarifs_per_seat_category);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (_, idVoyage, _, _, idEcoSiege, idPremSiege) = await SeedVoyageTwoCategoriesAsync(ctx);

            var svc = new VoyageTarifService(ctx);
            var total = await svc.ComputeTotalForSiegesAsync(
                idVoyage,
                new[] { idEcoSiege, idPremSiege },
                prixFallbackVoyage: 0);

            Assert.Equal(3500m, total);
        }

        [Fact]
        public async Task ComputeTotalForSiegesAsync_throws_when_siege_missing()
        {
            var db = nameof(ComputeTotalForSiegesAsync_throws_when_siege_missing);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (_, idVoyage, _, _, idEcoSiege, _) = await SeedVoyageTwoCategoriesAsync(ctx);

            var svc = new VoyageTarifService(ctx);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.ComputeTotalForSiegesAsync(idVoyage, new[] { idEcoSiege, 999999 }, 0));
        }

        [Fact]
        public async Task ReplaceTarifsForVoyageAsync_replaces_rows()
        {
            var db = nameof(ReplaceTarifsForVoyageAsync_replaces_rows);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (_, idVoyage, idEco, idPrem, _, _) = await SeedVoyageTwoCategoriesAsync(ctx);

            var svc = new VoyageTarifService(ctx);
            await svc.ReplaceTarifsForVoyageAsync(
                idVoyage,
                ctx.Voyages.AsNoTracking().First(v => v.Id == idVoyage).IdSociete,
                new[] { (idEco, 111), (idPrem, 222) });

            var rows = await ctx.VoyageTarifsCategorieSiege.AsNoTracking()
                .Where(t => t.IdVoyage == idVoyage)
                .OrderBy(t => t.IdCategorieSiege)
                .ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.Equal(111, rows.First(t => t.IdCategorieSiege == idEco).Prix);
            Assert.Equal(222, rows.First(t => t.IdCategorieSiege == idPrem).Prix);
        }

        [Fact]
        public async Task ReplaceTarifsForVoyageAsync_throws_on_duplicate_categories()
        {
            var db = nameof(ReplaceTarifsForVoyageAsync_throws_on_duplicate_categories);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (_, idVoyage, idEco, _, _, _) = await SeedVoyageTwoCategoriesAsync(ctx);
            var idSociete = ctx.Voyages.AsNoTracking().First(v => v.Id == idVoyage).IdSociete;

            var svc = new VoyageTarifService(ctx);
            await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.ReplaceTarifsForVoyageAsync(idVoyage, idSociete, new[] { (idEco, 1), (idEco, 2) }));
        }

        [Fact]
        public async Task EnsureDefaultEcoTarifAsync_inserts_only_when_empty()
        {
            var db = nameof(EnsureDefaultEcoTarifAsync_inserts_only_when_empty);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (c, idVoyage, idEco, _, _, _) = await SeedVoyageTwoCategoriesAsync(ctx);
            var idSociete = c.Voyages.AsNoTracking().First(v => v.Id == idVoyage).IdSociete;

            c.VoyageTarifsCategorieSiege.RemoveRange(c.VoyageTarifsCategorieSiege);
            await c.SaveChangesAsync();

            var svc = new VoyageTarifService(c);
            await svc.EnsureDefaultEcoTarifAsync(idVoyage, idSociete, prixVoyage: 7777);

            var row = await c.VoyageTarifsCategorieSiege.SingleAsync(
                t => t.IdVoyage == idVoyage && t.IdCategorieSiege == idEco);
            Assert.Equal(7777, row.Prix);

            await svc.EnsureDefaultEcoTarifAsync(idVoyage, idSociete, prixVoyage: 1);
            Assert.Equal(1, await c.VoyageTarifsCategorieSiege.CountAsync(t => t.IdVoyage == idVoyage));
        }

        [Fact]
        public async Task UpsertTarifForVoyageAsync_updates_existing_row()
        {
            var db = nameof(UpsertTarifForVoyageAsync_updates_existing_row);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (_, idVoyage, idEco, _, _, _) = await SeedVoyageTwoCategoriesAsync(ctx);
            var idSociete = ctx.Voyages.AsNoTracking().First(v => v.Id == idVoyage).IdSociete;

            var svc = new VoyageTarifService(ctx);
            await svc.UpsertTarifForVoyageAsync(idVoyage, idSociete, idEco, 3333);

            var prix = await ctx.VoyageTarifsCategorieSiege.AsNoTracking()
                .Where(t => t.IdVoyage == idVoyage && t.IdCategorieSiege == idEco)
                .Select(t => t.Prix)
                .FirstAsync();
            Assert.Equal(3333, prix);
        }

        [Fact]
        public async Task ResolveReferencePrixFromTarifs_prefers_eco()
        {
            var db = nameof(ResolveReferencePrixFromTarifs_prefers_eco);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (_, idVoyage, idEco, idPrem, _, _) = await SeedVoyageTwoCategoriesAsync(ctx);
            var idSociete = ctx.Voyages.AsNoTracking().First(v => v.Id == idVoyage).IdSociete;

            var svc = new VoyageTarifService(ctx);
            var reference = await svc.ResolveReferencePrixFromTarifsAsync(idVoyage, idSociete, prixFallbackVoyage: 1);

            Assert.Equal(1000, reference);
        }

        [Fact]
        public async Task SyncEcoTarifPrixAsync_updates_eco_row()
        {
            var db = nameof(SyncEcoTarifPrixAsync_updates_eco_row);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (_, idVoyage, idEco, _, _, _) = await SeedVoyageTwoCategoriesAsync(ctx);
            var idSociete = ctx.Voyages.AsNoTracking().First(v => v.Id == idVoyage).IdSociete;

            var svc = new VoyageTarifService(ctx);
            await svc.SyncEcoTarifPrixAsync(idVoyage, idSociete, nouveauPrixVoyage: 5555);

            var ecoPrix = await ctx.VoyageTarifsCategorieSiege.AsNoTracking()
                .Where(t => t.IdVoyage == idVoyage && t.IdCategorieSiege == idEco)
                .Select(t => t.Prix)
                .FirstAsync();
            Assert.Equal(5555, ecoPrix);
        }
    }
}
