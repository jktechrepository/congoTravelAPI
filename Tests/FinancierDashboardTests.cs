using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class FinancierDashboardTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockFinancierUser(int societeId) =>
            MockUser(isSuperAdmin: false, hasFinanceAccess: true, societeId);

        private static Mock<ICurrentUserService> MockSuperAdminUser() =>
            MockUser(isSuperAdmin: true, hasFinanceAccess: true, societeId: 1);

        private static Mock<ICurrentUserService> MockUser(bool isSuperAdmin, bool hasFinanceAccess, int societeId)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.IsSuperAdmin).Returns(isSuperAdmin);
            mock.SetupGet(x => x.HasFinanceAccess).Returns(hasFinanceAccess);
            mock.SetupGet(x => x.SocieteId).Returns(societeId);
            mock.SetupGet(x => x.UserId).Returns(1);
            return mock;
        }

        [Fact]
        public async Task GetDashboardDataAsync_financier_scoped_to_token_societe()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_financier_scoped_to_token_societe));
            SeedTwoSocietes(ctx, withPaymentSociete1: true, withUnpaidSociete2: true);
            await ctx.SaveChangesAsync();

            var svc = DashboardEnrichmentTestHelper.CreateFinancierDashboardService(
                ctx, MockFinancierUser(1).Object);

            var result = await svc.GetDashboardDataAsync();

            Assert.Single(result.SocietesFinancieres);
            Assert.Equal(1, result.SocietesFinancieres[0].IdSociete);
            Assert.Equal(5000m, result.GlobalStatistiques.ChiffreAffairesMois);
            Assert.Equal(1, result.GlobalStatistiques.NombreTotalTransactions);
            Assert.Equal(12, result.Tendances.RevenusTransport.Count);
            Assert.Equal(3, result.CollecteParOrigineGroupe.Count);
            Assert.Equal(3, result.SocietesFinancieres[0].CollecteParOrigineGroupe.Count);
        }

        [Fact]
        public async Task GetDashboardDataAsync_superadmin_aggregates_all_societes()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_superadmin_aggregates_all_societes));
            SeedTwoSocietes(ctx, withPaymentSociete1: true, withUnpaidSociete2: true);
            await ctx.SaveChangesAsync();

            var svc = DashboardEnrichmentTestHelper.CreateFinancierDashboardService(
                ctx, MockSuperAdminUser().Object);

            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(2, result.SocietesFinancieres.Count);
            Assert.Equal(5000m, result.GlobalStatistiques.ChiffreAffairesMois);
            Assert.True(result.GlobalStatistiques.MontantReservationsNonPayees >= 8000m);
        }

        [Fact]
        public async Task Controller_returns_forbid_without_finance_access()
        {
            var noFinance = MockUser(isSuperAdmin: false, hasFinanceAccess: false, societeId: 1);
            var controller = new FinancierDashboardController(
                DashboardEnrichmentTestHelper.CreateFinancierDashboardService(
                    BuildDb(nameof(Controller_returns_forbid_without_finance_access)),
                    noFinance.Object),
                noFinance.Object,
                NullLogger<FinancierDashboardController>.Instance);

            var result = await controller.GetFinancierDashboard();

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task Controller_returns_ok_with_finance_access()
        {
            await using var ctx = BuildDb(nameof(Controller_returns_ok_with_finance_access));
            SeedTwoSocietes(ctx, withPaymentSociete1: true, withUnpaidSociete2: false);
            await ctx.SaveChangesAsync();

            var user = MockFinancierUser(1);
            var controller = new FinancierDashboardController(
                DashboardEnrichmentTestHelper.CreateFinancierDashboardService(ctx, user.Object),
                user.Object,
                NullLogger<FinancierDashboardController>.Instance);

            var result = await controller.GetFinancierDashboard();

            Assert.IsType<OkObjectResult>(result.Result);
        }

        private static void SeedTwoSocietes(
            CongoTravelDbContext ctx,
            bool withPaymentSociete1,
            bool withUnpaidSociete2)
        {
            ctx.Societes.AddRange(
                new Societe { IdSociete = 1, Nom = "Societe A", CodeDevisePrincipale = "CDF", Statut = true, DateCreation = DateTime.UtcNow },
                new Societe { IdSociete = 2, Nom = "Societe B", CodeDevisePrincipale = "CDF", Statut = true, DateCreation = DateTime.UtcNow });

            ctx.Destinations.AddRange(
                new Destination { IdDestination = 1, IdSociete = 1, VilleDepart = "Kin", VilleArrivee = "Goma", Statut = true },
                new Destination { IdDestination = 2, IdSociete = 2, VilleDepart = "Lub", VilleArrivee = "Kan", Statut = true });

            ctx.Vehicules.AddRange(
                new Vehicule { IdVehicule = 1, IdSociete = 1, IdTypeVehicule = 1, AliasVehicule = "A1", NombreSiege = 20, Statut = true },
                new Vehicule { IdVehicule = 2, IdSociete = 2, IdTypeVehicule = 1, AliasVehicule = "B1", NombreSiege = 30, Statut = true });

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
                    Id = 2, IdSociete = 2, IdVehicule = 2, IdDestination = 2,
                    DateDepart = DateTime.UtcNow.Date, HeureDepart = TimeSpan.FromHours(9),
                    Prix = 8000, CodeDevisePrix = "CDF", CodeDevisePrincipale = "CDF",
                    PrixDevisePrincipale = 8000, Statut = true
                });

            ctx.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "C1", AdresseClient = "A", Statut = true, IsActif = true },
                new Client { IdClient = 2, NomClient = "C2", AdresseClient = "B", Statut = true, IsActif = true });

            ctx.Reservations.AddRange(
                new Reservation
                {
                    IdReservation = 1, IdSociete = 1, IdClient = 1, IdUtilisateur = 1, IdVoyage = 1,
                    DateReservation = DateTime.UtcNow, StatutReservation = "CONFIRMEE", Statut = true, NombreDePlace = 1
                },
                new Reservation
                {
                    IdReservation = 2, IdSociete = 2, IdClient = 2, IdUtilisateur = 1, IdVoyage = 2,
                    DateReservation = DateTime.UtcNow, StatutReservation = "CONFIRMEE", Statut = true, NombreDePlace = 1
                });

            if (withPaymentSociete1)
            {
                ctx.Paiements.Add(new Paiement
                {
                    IdPaiement = 1, IdSociete = 1, IdReservation = 1, IdUtilisateur = 1,
                    MontantAPaye = 5000, MontantPaye = 5000,
                    MontantAPayeDevisePrincipale = 5000, MontantPayeDevisePrincipale = 5000,
                    CodeDevisePaiement = "CDF", CodeDevisePrincipale = "CDF",
                    DatePaiement = DateTime.UtcNow, Statut = true, IsDeleted = false
                });
            }

            if (withUnpaidSociete2)
            {
                // reservation 2 has no payment -> unpaid 8000
            }
        }
    }
}
