using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.HealthChecks;
using CongoTravel.Models;
using CongoTravel.Models.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class GerantDashboardTests
    {
        private const int SiteA = 1;
        private const int SiteB = 2;

        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockGerantUser(int societeId = 1, int? siteId = null) =>
            MockUser(UserRoles.GERANT, societeId, isSuperAdmin: false, siteId: siteId);

        private static Mock<ICurrentUserService> MockSuperAdminUser(int societeId = 1, int? siteId = null) =>
            MockUser(UserRoles.SUPER_ADMIN, societeId, isSuperAdmin: true, siteId: siteId);

        private static Mock<ICurrentUserService> MockUser(string role, int societeId, bool isSuperAdmin, int? siteId = null)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.UserRole).Returns(role);
            mock.SetupGet(x => x.IsSuperAdmin).Returns(isSuperAdmin);
            mock.SetupGet(x => x.SocieteId).Returns(societeId);
            mock.SetupGet(x => x.SiteId).Returns(siteId);
            mock.SetupGet(x => x.UserId).Returns(10);
            return mock;
        }

        private static GerantDashboardService CreateGerantService(
            CongoTravelDbContext ctx,
            Mock<ICurrentUserService> user,
            bool grantEvenementPermission = false) =>
            new(
                ctx,
                user.Object,
                DashboardEnrichmentTestHelper.CreateEvenementDashboardMock().Object,
                DashboardEnrichmentTestHelper.CreatePermissionMock(grantEvenementPermission).Object,
                NullLogger<GerantDashboardService>.Instance);

        [Fact]
        public async Task GetDashboardDataAsync_returns_transport_metrics()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_returns_transport_metrics));
            SeedData(ctx);
            await ctx.SaveChangesAsync();

            var svc = CreateGerantService(ctx, MockGerantUser());
            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(5000m, result.SocieteStatistiques.ChiffreAffairesMois);
            Assert.Equal("CDF", result.SocieteStatistiques.CodeDevisePrincipale);
            Assert.Equal(2, result.TransportStatistiques.VoyagesActifs);
            Assert.Single(result.Top5ClientsCA);
            Assert.Single(result.Top5ClientsNonPayes);
            Assert.Equal(12, result.Tendances.EvolutionChiffreAffaires.Count);
            Assert.Equal(3, result.CollecteParOrigineGroupe.Count);
            Assert.Equal(5000m, result.CollecteOrigineGroupeSynthese.MontantClassifie);
            Assert.Null(result.EvenementStatistiques);
        }

        [Fact]
        public async Task GetDashboardDataAsync_includes_evenement_widget_when_permission_granted()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_includes_evenement_widget_when_permission_granted));
            SeedData(ctx);
            await ctx.SaveChangesAsync();

            var svc = CreateGerantService(ctx, MockGerantUser(), grantEvenementPermission: true);
            var result = await svc.GetDashboardDataAsync();

            Assert.NotNull(result.EvenementStatistiques);
            Assert.Equal(2, result.EvenementStatistiques!.Summary.SessionsPubliees);
        }

        [Fact]
        public async Task SuperAdmin_gerant_dashboard_includes_evenement_widget_without_explicit_permission()
        {
            await using var ctx = BuildDb(nameof(SuperAdmin_gerant_dashboard_includes_evenement_widget_without_explicit_permission));
            SeedData(ctx);
            await ctx.SaveChangesAsync();

            var svc = CreateGerantService(ctx, MockSuperAdminUser());
            var result = await svc.GetDashboardDataAsync();

            Assert.NotNull(result.EvenementStatistiques);
            Assert.Equal(2, result.EvenementStatistiques!.Summary.SessionsPubliees);
        }

        [Fact]
        public async Task GetDashboardDataAsync_filters_by_site()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_filters_by_site));
            SeedMultiSiteData(ctx);
            await ctx.SaveChangesAsync();

            var svc = CreateGerantService(ctx, MockGerantUser(siteId: SiteA));
            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(5000m, result.SocieteStatistiques.ChiffreAffairesMois);
            Assert.Equal(1, result.TransportStatistiques.VoyagesActifs);
            Assert.Single(result.Top5ClientsCA);
            Assert.Equal(1, result.Top5ClientsCA[0].IdClient);
            Assert.Equal(5000m, result.PaiementsStatistiques.PaiementsMois);
        }

        [Fact]
        public async Task GetDashboardDataAsync_fallback_societe_when_no_site()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_fallback_societe_when_no_site));
            SeedMultiSiteData(ctx);
            await ctx.SaveChangesAsync();

            var svc = CreateGerantService(ctx, MockGerantUser(siteId: null));
            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(15000m, result.SocieteStatistiques.ChiffreAffairesMois);
            Assert.Equal(2, result.TransportStatistiques.VoyagesActifs);
        }

        [Fact]
        public async Task SuperAdmin_uses_site_from_token()
        {
            await using var ctx = BuildDb(nameof(SuperAdmin_uses_site_from_token));
            SeedMultiSiteData(ctx);
            await ctx.SaveChangesAsync();

            var svc = CreateGerantService(ctx, MockSuperAdminUser(siteId: SiteB));
            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(10000m, result.SocieteStatistiques.ChiffreAffairesMois);
            Assert.Equal(1, result.TransportStatistiques.VoyagesActifs);
            Assert.Single(result.Top5ClientsCA);
            Assert.Equal(3, result.Top5ClientsCA[0].IdClient);
        }

        [Fact]
        public async Task Controller_returns_forbid_for_admin_role()
        {
            var admin = MockUser(UserRoles.ADMIN, 1, false);
            var controller = new GerantDashboardController(
                CreateGerantService(BuildDb(nameof(Controller_returns_forbid_for_admin_role)), admin),
                admin.Object,
                NullLogger<GerantDashboardController>.Instance);

            var result = await controller.GetGerantDashboard();
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task Controller_returns_ok_for_gerant()
        {
            await using var ctx = BuildDb(nameof(Controller_returns_ok_for_gerant));
            SeedData(ctx);
            await ctx.SaveChangesAsync();

            var user = MockGerantUser();
            var controller = new GerantDashboardController(
                CreateGerantService(ctx, user),
                user.Object,
                NullLogger<GerantDashboardController>.Instance);

            var result = await controller.GetGerantDashboard();
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task HealthCheck_returns_healthy()
        {
            await using var ctx = BuildDb(nameof(HealthCheck_returns_healthy));
            SeedData(ctx);
            await ctx.SaveChangesAsync();

            var svc = CreateGerantService(ctx, MockGerantUser());
            var health = new GerantDashboardHealthCheck(svc, NullLogger<GerantDashboardHealthCheck>.Instance);

            var result = await health.CheckHealthAsync(new HealthCheckContext());
            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        private static void SeedData(CongoTravelDbContext ctx)
        {
            ctx.Societes.Add(new Societe
            {
                IdSociete = 1,
                Nom = "Rusa Demo",
                CodeDevisePrincipale = "CDF",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Destinations.Add(new Destination
            {
                IdDestination = 1,
                IdSociete = 1,
                VilleDepart = "Kinshasa",
                VilleArrivee = "Goma",
                Statut = true
            });

            ctx.Vehicules.Add(new Vehicule
            {
                IdVehicule = 1,
                IdSociete = 1,
                IdTypeVehicule = 1,
                AliasVehicule = "BUS-1",
                NombreSiege = 20,
                Statut = true
            });

            ctx.Voyages.AddRange(
                new Voyage
                {
                    Id = 1, IdSociete = 1, IdVehicule = 1, IdDestination = 1,
                    DateDepart = DateTime.UtcNow.Date, HeureDepart = TimeSpan.FromHours(8),
                    Prix = 5000, CodeDevisePrix = "CDF", CodeDevisePrincipale = "CDF",
                    PrixDevisePrincipale = 5000, Statut = true
                },
                new Voyage
                {
                    Id = 2, IdSociete = 1, IdVehicule = 1, IdDestination = 1,
                    DateDepart = DateTime.UtcNow.Date.AddDays(1), HeureDepart = TimeSpan.FromHours(9),
                    Prix = 6000, CodeDevisePrix = "CDF", CodeDevisePrincipale = "CDF",
                    PrixDevisePrincipale = 6000, Statut = true
                });

            ctx.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "Client Payé", AdresseClient = "A", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow },
                new Client { IdClient = 2, NomClient = "Client Impayé", AdresseClient = "B", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow });

            ctx.Reservations.AddRange(
                new Reservation
                {
                    IdReservation = 1, IdSociete = 1, IdClient = 1, IdUtilisateur = 1, IdVoyage = 1,
                    DateReservation = DateTime.UtcNow, StatutReservation = "CONFIRMEE", Statut = true, NombreDePlace = 1
                },
                new Reservation
                {
                    IdReservation = 2, IdSociete = 1, IdClient = 2, IdUtilisateur = 1, IdVoyage = 2,
                    DateReservation = DateTime.UtcNow, StatutReservation = "CONFIRMEE", Statut = true, NombreDePlace = 1
                });

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 1, IdSociete = 1, IdReservation = 1, IdUtilisateur = 1,
                MontantAPaye = 5000, MontantPaye = 5000,
                MontantAPayeDevisePrincipale = 5000, MontantPayeDevisePrincipale = 5000,
                CodeDevisePaiement = "CDF", CodeDevisePrincipale = "CDF",
                DatePaiement = DateTime.UtcNow, Statut = true, IsDeleted = false,
                Origine = OrigineOperation.CAISSIER
            });
        }

        private static void SeedMultiSiteData(CongoTravelDbContext ctx)
        {
            ctx.Societes.Add(new Societe
            {
                IdSociete = 1,
                Nom = "Rusa Demo",
                CodeDevisePrincipale = "CDF",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Sites.AddRange(
                new Site
                {
                    IdSite = SiteA, IdSociete = 1, CodeSite = "KIN", NomSite = "Kinshasa",
                    NomResponsableSite = "Resp A", Statut = true
                },
                new Site
                {
                    IdSite = SiteB, IdSociete = 1, CodeSite = "GOM", NomSite = "Goma",
                    NomResponsableSite = "Resp B", Statut = true
                });

            ctx.Destinations.Add(new Destination
            {
                IdDestination = 1,
                IdSociete = 1,
                VilleDepart = "Kinshasa",
                VilleArrivee = "Goma",
                Statut = true
            });

            ctx.Vehicules.Add(new Vehicule
            {
                IdVehicule = 1,
                IdSociete = 1,
                IdTypeVehicule = 1,
                AliasVehicule = "BUS-1",
                NombreSiege = 20,
                Statut = true
            });

            ctx.Voyages.AddRange(
                new Voyage
                {
                    Id = 1, IdSociete = 1, IdSite = SiteA, IdVehicule = 1, IdDestination = 1,
                    DateDepart = DateTime.UtcNow.Date, HeureDepart = TimeSpan.FromHours(8),
                    Prix = 5000, CodeDevisePrix = "CDF", CodeDevisePrincipale = "CDF",
                    PrixDevisePrincipale = 5000, Statut = true
                },
                new Voyage
                {
                    Id = 2, IdSociete = 1, IdSite = SiteB, IdVehicule = 1, IdDestination = 1,
                    DateDepart = DateTime.UtcNow.Date.AddDays(1), HeureDepart = TimeSpan.FromHours(9),
                    Prix = 6000, CodeDevisePrix = "CDF", CodeDevisePrincipale = "CDF",
                    PrixDevisePrincipale = 6000, Statut = true
                });

            ctx.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "Client Payé A", AdresseClient = "A", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow },
                new Client { IdClient = 2, NomClient = "Client Impayé A", AdresseClient = "B", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow },
                new Client { IdClient = 3, NomClient = "Client Site B", AdresseClient = "C", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow });

            ctx.Reservations.AddRange(
                new Reservation
                {
                    IdReservation = 1, IdSociete = 1, IdSite = SiteA, IdClient = 1, IdUtilisateur = 1, IdVoyage = 1,
                    DateReservation = DateTime.UtcNow, StatutReservation = "CONFIRMEE", Statut = true, NombreDePlace = 1
                },
                new Reservation
                {
                    IdReservation = 2, IdSociete = 1, IdSite = SiteA, IdClient = 2, IdUtilisateur = 1, IdVoyage = 2,
                    DateReservation = DateTime.UtcNow, StatutReservation = "CONFIRMEE", Statut = true, NombreDePlace = 1
                },
                new Reservation
                {
                    IdReservation = 3, IdSociete = 1, IdSite = SiteB, IdClient = 3, IdUtilisateur = 1, IdVoyage = 2,
                    DateReservation = DateTime.UtcNow, StatutReservation = "CONFIRMEE", Statut = true, NombreDePlace = 1
                });

            ctx.Paiements.AddRange(
                new Paiement
                {
                    IdPaiement = 1, IdSociete = 1, IdSite = SiteA, IdReservation = 1, IdUtilisateur = 1,
                    MontantAPaye = 5000, MontantPaye = 5000,
                    MontantAPayeDevisePrincipale = 5000, MontantPayeDevisePrincipale = 5000,
                    CodeDevisePaiement = "CDF", CodeDevisePrincipale = "CDF",
                    DatePaiement = DateTime.UtcNow, Statut = true, IsDeleted = false,
                    Origine = OrigineOperation.CAISSIER
                },
                new Paiement
                {
                    IdPaiement = 2, IdSociete = 1, IdSite = SiteB, IdReservation = 3, IdUtilisateur = 1,
                    MontantAPaye = 10000, MontantPaye = 10000,
                    MontantAPayeDevisePrincipale = 10000, MontantPayeDevisePrincipale = 10000,
                    CodeDevisePaiement = "CDF", CodeDevisePrincipale = "CDF",
                    DatePaiement = DateTime.UtcNow, Statut = true, IsDeleted = false,
                    Origine = OrigineOperation.CLIENT
                });
        }
    }
}
