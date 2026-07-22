using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiegeServiceCategorieTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        [Fact]
        public async Task EnsureSeatsForVehiculeAsync_creates_seats_with_eco_category()
        {
            var db = nameof(EnsureSeatsForVehiculeAsync_creates_seats_with_eco_category);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var s = new Societe { Nom = "S", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            ctx.CategorieSieges.Add(new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "ECO",
                Libelle = "Eco",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            var tv = new TypeVehicule { Libelle = "T", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "V1",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 3,
                IdSociete = s.IdSociete,
                NumeroDePlaque = "ZZ",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);
            await ctx.SaveChangesAsync();

            var svc = new SiegeService(ctx, NullLogger<SiegeService>.Instance);
            await svc.EnsureSeatsForVehiculeAsync(vh.IdVehicule);

            var sieges = await ctx.Sieges.Where(x => x.IdVehicule == vh.IdVehicule).OrderBy(x => x.NumeroOrdre).ToListAsync();
            Assert.Equal(3, sieges.Count);
            var ecoId = await ctx.CategorieSieges.Where(c => c.CodeCategorieSiege == "ECO").Select(c => c.IdCategorieSiege).SingleAsync();
            Assert.All(sieges, si => Assert.Equal(ecoId, si.IdCategorieSiege));
        }

        [Fact]
        public async Task EnsureSeatsForVehiculeAsync_throws_when_no_eco_for_societe()
        {
            var db = nameof(EnsureSeatsForVehiculeAsync_throws_when_no_eco_for_societe);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var s = new Societe { Nom = "S2", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            var tv = new TypeVehicule { Libelle = "T2", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "V2",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 1,
                IdSociete = s.IdSociete,
                NumeroDePlaque = "YY",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);
            await ctx.SaveChangesAsync();

            var svc = new SiegeService(ctx, NullLogger<SiegeService>.Instance);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.EnsureSeatsForVehiculeAsync(vh.IdVehicule));
        }

        [Fact]
        public async Task GetActiveRepartitionByVehiculeIdsAsync_counts_only_active_seats()
        {
            var db = nameof(GetActiveRepartitionByVehiculeIdsAsync_counts_only_active_seats);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var s = new Societe { Nom = "S3", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var eco = new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "ECO",
                Libelle = "Economique",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            var prem = new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "PREM",
                Libelle = "Premiere",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.AddRange(eco, prem);
            var tv = new TypeVehicule { Libelle = "T3", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "V3",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 4,
                IdSociete = s.IdSociete,
                NumeroDePlaque = "XX",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);
            await ctx.SaveChangesAsync();

            ctx.Sieges.AddRange(
                new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 1, CodeSiege = "ECO/1", EstActif = true, IdSociete = s.IdSociete, IdCategorieSiege = eco.IdCategorieSiege, DateCreation = DateTime.UtcNow },
                new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 2, CodeSiege = "ECO/2", EstActif = true, IdSociete = s.IdSociete, IdCategorieSiege = eco.IdCategorieSiege, DateCreation = DateTime.UtcNow },
                new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 3, CodeSiege = "PREM/1", EstActif = true, IdSociete = s.IdSociete, IdCategorieSiege = prem.IdCategorieSiege, DateCreation = DateTime.UtcNow },
                new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 4, CodeSiege = "PREM/2", EstActif = false, IdSociete = s.IdSociete, IdCategorieSiege = prem.IdCategorieSiege, DateCreation = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            var svc = new SiegeService(ctx, NullLogger<SiegeService>.Instance);
            var result = await svc.GetActiveRepartitionByVehiculeIdsAsync(new[] { vh.IdVehicule });

            Assert.True(result.ContainsKey(vh.IdVehicule));
            var repartition = result[vh.IdVehicule];
            Assert.Equal(2, repartition.Count);

            var ecoLine = repartition.Single(r => r.CodeCategorieSiege == "ECO");
            Assert.Equal(eco.IdCategorieSiege, ecoLine.IdCategorieSiege);
            Assert.Equal("Economique", ecoLine.Libelle);
            Assert.Equal(2, ecoLine.NombreSiegeParCategorie);

            var premLine = repartition.Single(r => r.CodeCategorieSiege == "PREM");
            Assert.Equal(1, premLine.NombreSiegeParCategorie);
        }

        [Fact]
        public async Task GetActiveRepartitionByVehiculeIdsAsync_returns_empty_list_when_no_active_seats()
        {
            var db = nameof(GetActiveRepartitionByVehiculeIdsAsync_returns_empty_list_when_no_active_seats);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var s = new Societe { Nom = "S4", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            var tv = new TypeVehicule { Libelle = "T4", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            var vh = new Vehicule
            {
                AliasVehicule = "V4",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 0,
                IdSociete = s.IdSociete,
                NumeroDePlaque = "WW",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);
            await ctx.SaveChangesAsync();

            var svc = new SiegeService(ctx, NullLogger<SiegeService>.Instance);
            var result = await svc.GetActiveRepartitionByVehiculeIdsAsync(new[] { vh.IdVehicule });

            Assert.True(result.ContainsKey(vh.IdVehicule));
            Assert.Empty(result[vh.IdVehicule]);
        }

        [Fact]
        public async Task EnsureSeatsForVehiculeWithCategorieDistributionAsync_reorders_codes_without_duplicates()
        {
            var db = nameof(EnsureSeatsForVehiculeWithCategorieDistributionAsync_reorders_codes_without_duplicates);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var s = new Societe { Nom = "S5", DateCreation = DateTime.UtcNow };
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
                Libelle = "Vip",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.AddRange(eco, vip);

            var tv = new TypeVehicule
            {
                Libelle = "T5",
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "V5",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 4,
                IdSociete = s.IdSociete,
                NumeroDePlaque = "VV",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);
            await ctx.SaveChangesAsync();

            // État initial : 3 ECO + 1 VIP.
            ctx.Sieges.AddRange(
                new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 1, CodeSiege = "ECO/1", EstActif = true, IdSociete = s.IdSociete, IdCategorieSiege = eco.IdCategorieSiege, DateCreation = DateTime.UtcNow },
                new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 2, CodeSiege = "ECO/2", EstActif = true, IdSociete = s.IdSociete, IdCategorieSiege = eco.IdCategorieSiege, DateCreation = DateTime.UtcNow },
                new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 3, CodeSiege = "ECO/3", EstActif = true, IdSociete = s.IdSociete, IdCategorieSiege = eco.IdCategorieSiege, DateCreation = DateTime.UtcNow },
                new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 4, CodeSiege = "VIP/1", EstActif = true, IdSociete = s.IdSociete, IdCategorieSiege = vip.IdCategorieSiege, DateCreation = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            var svc = new SiegeService(ctx, NullLogger<SiegeService>.Instance);
            // Nouvelle répartition : 1 ECO + 3 VIP (force des permutations de codes).
            await svc.EnsureSeatsForVehiculeWithCategorieDistributionAsync(
                vh.IdVehicule,
                new List<(int IdCategorieSiege, int NombreSiegeParCategorie)>
                {
                    (eco.IdCategorieSiege, 1),
                    (vip.IdCategorieSiege, 3)
                });

            var sieges = await ctx.Sieges
                .Where(x => x.IdVehicule == vh.IdVehicule)
                .OrderBy(x => x.NumeroOrdre)
                .ToListAsync();

            Assert.Equal(4, sieges.Count);
            Assert.Equal(4, sieges.Select(x => x.CodeSiege).Distinct().Count());
            Assert.Equal("ECO/1", sieges[0].CodeSiege);
            Assert.Equal("VIP/1", sieges[1].CodeSiege);
            Assert.Equal("VIP/2", sieges[2].CodeSiege);
            Assert.Equal("VIP/3", sieges[3].CodeSiege);
        }
    }
}
