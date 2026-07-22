using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>
    /// SearchTerm sur les listes paginées voyage (traduction SQL EF Core).
    /// </summary>
    public class VoyagePagedSearchTermTests
    {
        private sealed record VoyagePagedSeed(
            int IdSociete,
            int IdSite,
            int IdVehicule,
            int IdDestination,
            int IdRecentVoyage);

        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        private static async Task<(int IdSociete, int IdVoyage)> SeedVoyageAsync(CongoTravelDbContext ctx)
        {
            var seed = await SeedVoyagePagedContextAsync(ctx);
            return (seed.IdSociete, seed.IdRecentVoyage);
        }

        private static async Task<VoyagePagedSeed> SeedVoyagePagedContextAsync(CongoTravelDbContext ctx)
        {
            var s = new Societe { Nom = "SearchCo", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var site = new Site
            {
                IdSociete = s.IdSociete,
                CodeSite = "S1",
                NomSite = "Site Test",
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(site);

            var tv = new TypeVehicule { Libelle = "T", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "BUS-ALPHA",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 10,
                IdSociete = s.IdSociete,
                NumeroDePlaque = "XY",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);

            var dest = new Destination
            {
                VilleDepart = "Kinshasa",
                VilleArrivee = "Matadi",
                Montant = 1,
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(dest);
            await ctx.SaveChangesAsync();

            var voy = new Voyage
            {
                DateDepart = new DateTime(2026, 5, 20),
                HeureDepart = new TimeSpan(8, 30, 0),
                Prix = 15000,
                IdVehicule = vh.IdVehicule,
                IdDestination = dest.IdDestination,
                IdSociete = s.IdSociete,
                IdSite = site.IdSite,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Voyages.Add(voy);
            await ctx.SaveChangesAsync();

            return new VoyagePagedSeed(s.IdSociete, site.IdSite, vh.IdVehicule, dest.IdDestination, voy.Id);
        }

        private static VoyageService CreateService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<VoyageService>.Instance, new Mock<IVoyageTarifService>().Object, SiegeDisponibiliteTestHelper.Create(ctx));

        private static async Task AddOldVoyageAsync(CongoTravelDbContext ctx, VoyagePagedSeed seed)
        {
            ctx.Voyages.Add(new Voyage
            {
                DateDepart = new DateTime(2020, 1, 15),
                HeureDepart = new TimeSpan(6, 0, 0),
                Prix = 5000,
                IdVehicule = seed.IdVehicule,
                IdDestination = seed.IdDestination,
                IdSociete = seed.IdSociete,
                IdSite = seed.IdSite,
                Statut = true,
                DateCreation = DateTime.UtcNow.AddYears(-1)
            });
            await ctx.SaveChangesAsync();
        }

        private static async Task AddOtherSocieteVoyageAsync(CongoTravelDbContext ctx)
        {
            var societe2 = new Societe { Nom = "OtherCo", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe2);
            await ctx.SaveChangesAsync();

            var site2 = new Site
            {
                IdSociete = societe2.IdSociete,
                CodeSite = "S2",
                NomSite = "Site 2",
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(site2);

            var tv2 = new TypeVehicule { Libelle = "T2", IdSociete = societe2.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv2);
            await ctx.SaveChangesAsync();

            var vh2 = new Vehicule
            {
                AliasVehicule = "BUS-BETA",
                Marques = "M2",
                IdTypeVehicule = tv2.IdTypeVehicule,
                NombreSiege = 10,
                IdSociete = societe2.IdSociete,
                NumeroDePlaque = "ZZ",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh2);

            var dest2 = new Destination
            {
                VilleDepart = "Lubumbashi",
                VilleArrivee = "Likasi",
                Montant = 1,
                IdSociete = societe2.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(dest2);
            await ctx.SaveChangesAsync();

            ctx.Voyages.Add(new Voyage
            {
                DateDepart = new DateTime(2026, 7, 20),
                HeureDepart = new TimeSpan(10, 0, 0),
                Prix = 22000,
                IdVehicule = vh2.IdVehicule,
                IdDestination = dest2.IdDestination,
                IdSociete = societe2.IdSociete,
                IdSite = site2.IdSite,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        private static (DateTime Debut, DateTime Fin) DayRange(DateTime date) =>
            (date.Date, date.Date.AddDays(1).AddTicks(-1));

        [Fact]
        public async Task GetBySocietePagedAsync_searchTerm_by_city_does_not_throw()
        {
            var db = nameof(GetBySocietePagedAsync_searchTerm_by_city_does_not_throw);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (idSociete, _) = await SeedVoyageAsync(ctx);

            var svc = CreateService(ctx);

            var result = await svc.GetBySocietePagedAsync(
                idSociete,
                new PagedRequest { PageNumber = 1, PageSize = 10, SearchTerm = "Kinshasa" });

            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetBySocietePagedAsync_searchTerm_by_vehicle_alias_matches()
        {
            var db = nameof(GetBySocietePagedAsync_searchTerm_by_vehicle_alias_matches);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (idSociete, _) = await SeedVoyageAsync(ctx);

            var svc = CreateService(ctx);

            var result = await svc.GetBySocietePagedAsync(
                idSociete,
                new PagedRequest { PageNumber = 1, PageSize = 10, SearchTerm = "ALPHA" });

            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetBySocietePagedAsync_searchTerm_by_prix_matches()
        {
            var db = nameof(GetBySocietePagedAsync_searchTerm_by_prix_matches);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var (idSociete, _) = await SeedVoyageAsync(ctx);

            var svc = CreateService(ctx);

            var result = await svc.GetBySocietePagedAsync(
                idSociete,
                new PagedRequest { PageNumber = 1, PageSize = 10, SearchTerm = "15000" });

            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetBySocietePagedAsync_without_date_filter_includes_all_depart_dates()
        {
            var db = nameof(GetBySocietePagedAsync_without_date_filter_includes_all_depart_dates);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedVoyagePagedContextAsync(ctx);
            await AddOldVoyageAsync(ctx, seed);

            var recent = await ctx.Voyages.FindAsync(seed.IdRecentVoyage);
            Assert.NotNull(recent);

            var svc = CreateService(ctx);
            var request = new PagedRequest { PageNumber = 1, PageSize = 20 };

            var sansFiltre = await svc.GetBySocietePagedAsync(seed.IdSociete, request);
            Assert.Equal(2, sansFiltre.TotalCount);

            var (debut, fin) = DayRange(recent.DateDepart);
            var jourFiltre = await svc.GetBySocietePagedAsync(seed.IdSociete, request, debut, fin);
            Assert.Equal(1, jourFiltre.TotalCount);
            Assert.Equal(seed.IdRecentVoyage, jourFiltre.Data.Single().Id);
        }

        [Fact]
        public async Task GetBySitePagedAsync_without_date_filter_includes_all_depart_dates()
        {
            var db = nameof(GetBySitePagedAsync_without_date_filter_includes_all_depart_dates);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedVoyagePagedContextAsync(ctx);
            await AddOldVoyageAsync(ctx, seed);

            var recent = await ctx.Voyages.FindAsync(seed.IdRecentVoyage);
            Assert.NotNull(recent);

            var svc = CreateService(ctx);
            var request = new PagedRequest { PageNumber = 1, PageSize = 20 };

            var sansFiltre = await svc.GetBySitePagedAsync(seed.IdSite, request);
            Assert.Equal(2, sansFiltre.TotalCount);

            var (debut, fin) = DayRange(recent.DateDepart);
            var jourFiltre = await svc.GetBySitePagedAsync(seed.IdSite, request, debut, fin);
            Assert.Equal(1, jourFiltre.TotalCount);
            Assert.Equal(seed.IdRecentVoyage, jourFiltre.Data.Single().Id);
        }

        [Fact]
        public async Task GetByVehiculePagedAsync_without_date_filter_includes_all_depart_dates()
        {
            var db = nameof(GetByVehiculePagedAsync_without_date_filter_includes_all_depart_dates);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedVoyagePagedContextAsync(ctx);
            await AddOldVoyageAsync(ctx, seed);

            var recent = await ctx.Voyages.FindAsync(seed.IdRecentVoyage);
            Assert.NotNull(recent);

            var svc = CreateService(ctx);
            var request = new PagedRequest { PageNumber = 1, PageSize = 20 };

            var sansFiltre = await svc.GetByVehiculePagedAsync(seed.IdVehicule, request);
            Assert.Equal(2, sansFiltre.TotalCount);

            var (debut, fin) = DayRange(recent.DateDepart);
            var jourFiltre = await svc.GetByVehiculePagedAsync(seed.IdVehicule, request, debut, fin);
            Assert.Equal(1, jourFiltre.TotalCount);
            Assert.Equal(seed.IdRecentVoyage, jourFiltre.Data.Single().Id);
        }

        [Fact]
        public async Task GetByDestinationPagedAsync_without_date_filter_includes_all_depart_dates()
        {
            var db = nameof(GetByDestinationPagedAsync_without_date_filter_includes_all_depart_dates);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedVoyagePagedContextAsync(ctx);
            await AddOldVoyageAsync(ctx, seed);

            var recent = await ctx.Voyages.FindAsync(seed.IdRecentVoyage);
            Assert.NotNull(recent);

            var svc = CreateService(ctx);
            var request = new PagedRequest { PageNumber = 1, PageSize = 20 };

            var sansFiltre = await svc.GetByDestinationPagedAsync(seed.IdDestination, request);
            Assert.Equal(2, sansFiltre.TotalCount);

            var (debut, fin) = DayRange(recent.DateDepart);
            var jourFiltre = await svc.GetByDestinationPagedAsync(seed.IdDestination, request, debut, fin);
            Assert.Equal(1, jourFiltre.TotalCount);
            Assert.Equal(seed.IdRecentVoyage, jourFiltre.Data.Single().Id);
        }

        [Fact]
        public async Task SearchPagedAsync_filters_by_ville_depart()
        {
            var db = nameof(SearchPagedAsync_filters_by_ville_depart);
            await using var ctx = new CongoTravelDbContext(Options(db));
            await SeedVoyagePagedContextAsync(ctx);
            await AddOtherSocieteVoyageAsync(ctx);
            var svc = CreateService(ctx);

            var result = await svc.SearchPagedAsync(
                new PagedRequest { PageNumber = 1, PageSize = 20 },
                villeDepart: "kin");

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Kinshasa", result.Data.Single().Destination!.VilleDepart);
        }

        [Fact]
        public async Task SearchPagedAsync_filters_by_ville_arrivee()
        {
            var db = nameof(SearchPagedAsync_filters_by_ville_arrivee);
            await using var ctx = new CongoTravelDbContext(Options(db));
            await SeedVoyagePagedContextAsync(ctx);
            await AddOtherSocieteVoyageAsync(ctx);
            var svc = CreateService(ctx);

            var result = await svc.SearchPagedAsync(
                new PagedRequest { PageNumber = 1, PageSize = 20 },
                villeArrivee: "mat");

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Matadi", result.Data.Single().Destination!.VilleArrivee);
        }

        [Fact]
        public async Task SearchPagedAsync_filters_by_ville_depart_and_ville_arrivee()
        {
            var db = nameof(SearchPagedAsync_filters_by_ville_depart_and_ville_arrivee);
            await using var ctx = new CongoTravelDbContext(Options(db));
            await SeedVoyagePagedContextAsync(ctx);
            await AddOtherSocieteVoyageAsync(ctx);
            var svc = CreateService(ctx);

            var result = await svc.SearchPagedAsync(
                new PagedRequest { PageNumber = 1, PageSize = 20 },
                villeDepart: "kin",
                villeArrivee: "mat");

            Assert.Equal(1, result.TotalCount);
            var voyage = result.Data.Single();
            Assert.Equal("Kinshasa", voyage.Destination!.VilleDepart);
            Assert.Equal("Matadi", voyage.Destination.VilleArrivee);
        }

        [Fact]
        public async Task SearchPagedAsync_id_societe_optional_filters_when_provided()
        {
            var db = nameof(SearchPagedAsync_id_societe_optional_filters_when_provided);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedVoyagePagedContextAsync(ctx);
            await AddOtherSocieteVoyageAsync(ctx);
            var svc = CreateService(ctx);

            var sansSociete = await svc.SearchPagedAsync(
                new PagedRequest { PageNumber = 1, PageSize = 20 });
            Assert.Equal(2, sansSociete.TotalCount);

            var avecSociete = await svc.SearchPagedAsync(
                new PagedRequest { PageNumber = 1, PageSize = 20 },
                idSociete: seed.IdSociete);
            Assert.Equal(1, avecSociete.TotalCount);
            Assert.Equal(seed.IdSociete, avecSociete.Data.Single().IdSociete);
        }
    }
}
