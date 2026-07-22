using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class CaissierDashboardTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockCaissierUser(int societeId = 1, int userId = 10) =>
            MockUser(UserRoles.CAISSIER, societeId, userId, isSuperAdmin: false);

        private static Mock<ICurrentUserService> MockSuperAdminUser(int societeId = 1, int userId = 10) =>
            MockUser(UserRoles.SUPER_ADMIN, societeId, userId, isSuperAdmin: true);

        private static Mock<ICurrentUserService> MockUser(string role, int societeId, int userId, bool isSuperAdmin)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.UserRole).Returns(role);
            mock.SetupGet(x => x.IsSuperAdmin).Returns(isSuperAdmin);
            mock.SetupGet(x => x.SocieteId).Returns(societeId);
            mock.SetupGet(x => x.UserId).Returns(userId);
            mock.Setup(x => x.GetSocieteId()).Returns(societeId);
            return mock;
        }

        [Fact]
        public async Task GetDashboardDataAsync_returns_caissier_scoped_metrics()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_returns_caissier_scoped_metrics));
            const int caissierId = 10;
            const int otherCaissierId = 99;

            SeedBaseData(ctx, caissierId);
            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 2,
                IdSociete = 1,
                IdReservation = 1,
                IdUtilisateur = otherCaissierId,
                MontantAPaye = 3000,
                MontantPaye = 3000,
                MontantAPayeDevisePrincipale = 3000,
                MontantPayeDevisePrincipale = 3000,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                DateCreation = DateTime.UtcNow,
                DatePaiement = DateTime.UtcNow,
                Statut = true,
                IsDeleted = false
            });
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(
                ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissierUser(userId: caissierId).Object);

            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(5000m, result.StatistiquesJournalieres.TotalRevenusTransport);
            Assert.Equal(1, result.StatistiquesJournalieres.NombreTransactions);
            Assert.Equal("CDF", result.CodeDevisePrincipale);
            Assert.Single(result.PaiementsRecents);
        }

        [Fact]
        public async Task GetDashboardDataAsync_uses_montant_paye_devise_principale()
        {
            await using var ctx = BuildDb($"{nameof(CaissierDashboardTests)}_{nameof(GetDashboardDataAsync_uses_montant_paye_devise_principale)}");
            SeedBaseData(ctx, 10);
            await ctx.SaveChangesAsync();
            var paiement = ctx.Paiements.First();
            paiement.MontantPaye = 100m;
            paiement.MontantPayeDevisePrincipale = 5000m;
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(
                ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissierUser().Object);

            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(5000m, result.StatistiquesJournalieres.TotalRevenusTransport);
            Assert.Equal(5000m, result.ResumeCaisse.TotalEntrees);
        }

        [Fact]
        public async Task Controller_returns_forbid_for_admin_role()
        {
            var admin = MockUser(UserRoles.ADMIN, 1, 10, false);
            var controller = new CaissierDashboardController(
                new CaissierDashboardService(
                    BuildDb(nameof(Controller_returns_forbid_for_admin_role)),
                    NullLogger<CaissierDashboardService>.Instance,
                    admin.Object),
                admin.Object,
                NullLogger<CaissierDashboardController>.Instance);

            var result = await controller.GetCaissierDashboard();
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task Controller_returns_forbid_when_societe_id_invalid()
        {
            var caissier = MockUser(UserRoles.CAISSIER, 0, 10, false);
            var controller = new CaissierDashboardController(
                new CaissierDashboardService(
                    BuildDb(nameof(Controller_returns_forbid_when_societe_id_invalid)),
                    NullLogger<CaissierDashboardService>.Instance,
                    caissier.Object),
                caissier.Object,
                NullLogger<CaissierDashboardController>.Instance);

            var result = await controller.GetCaissierDashboard();
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(403, status.StatusCode);
        }

        [Fact]
        public async Task Controller_returns_ok_for_caissier()
        {
            await using var ctx = BuildDb(nameof(Controller_returns_ok_for_caissier));
            SeedBaseData(ctx, 10);
            await ctx.SaveChangesAsync();

            var user = MockCaissierUser();
            var controller = new CaissierDashboardController(
                new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, user.Object),
                user.Object,
                NullLogger<CaissierDashboardController>.Instance);

            var result = await controller.GetCaissierDashboard();
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetRapportCaisseAsync_scoped_to_caissier_and_splits_payment_methods()
        {
            await using var ctx = BuildDb(nameof(GetRapportCaisseAsync_scoped_to_caissier_and_splits_payment_methods));
            const int caissierId = 10;
            const int otherCaissierId = 99;
            var paymentDate = DateTime.UtcNow;

            SeedBaseData(ctx, caissierId);
            ctx.Paiements.AddRange(
                new Paiement
                {
                    IdPaiement = 2,
                    IdSociete = 1,
                    IdReservation = 1,
                    IdUtilisateur = caissierId,
                    MontantAPaye = 2000,
                    MontantPaye = 2000,
                    MontantAPayeDevisePrincipale = 2000,
                    MontantPayeDevisePrincipale = 2000,
                    CodeDevisePaiement = "CDF",
                    CodeDevisePrincipale = "CDF",
                    MethodePaiement = "MOBILE_MONEY",
                    DateCreation = paymentDate,
                    DatePaiement = paymentDate,
                    Statut = true,
                    IsDeleted = false
                },
                new Paiement
                {
                    IdPaiement = 3,
                    IdSociete = 1,
                    IdReservation = 1,
                    IdUtilisateur = otherCaissierId,
                    MontantAPaye = 9000,
                    MontantPaye = 9000,
                    MontantAPayeDevisePrincipale = 9000,
                    MontantPayeDevisePrincipale = 9000,
                    CodeDevisePaiement = "CDF",
                    CodeDevisePrincipale = "CDF",
                    MethodePaiement = "CASH",
                    DateCreation = paymentDate,
                    DatePaiement = paymentDate,
                    Statut = true,
                    IsDeleted = false
                });
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(
                ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissierUser(userId: caissierId).Object);

            var result = await svc.GetRapportCaisseAsync(paymentDate.Date, null, null);

            Assert.Equal(caissierId, result.IdUtilisateur);
            Assert.Equal(2, result.Synthese.NombreTransactions);
            Assert.Equal(7000m, result.Synthese.TotalEncaisse);
            Assert.Equal(5000m, result.Especes.MontantDevisePrincipale);
            Assert.Equal(2000m, result.Electronique.MontantDevisePrincipale);
            Assert.Equal(2000m, result.Electronique.Detail.MobileMoney.MontantDevisePrincipale);
        }

        [Fact]
        public async Task Controller_GetRapportCaisse_returns_ok_for_caissier()
        {
            await using var ctx = BuildDb(nameof(Controller_GetRapportCaisse_returns_ok_for_caissier));
            SeedBaseData(ctx, 10);
            await ctx.SaveChangesAsync();

            var user = MockCaissierUser();
            var controller = new CaissierDashboardController(
                new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, user.Object),
                user.Object,
                NullLogger<CaissierDashboardController>.Instance);

            var result = await controller.GetRapportCaisse(DateTime.UtcNow.Date, null, null);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<RapportCaisseDto>(ok.Value);
            Assert.Equal(10, dto.IdUtilisateur);
            Assert.Equal(5000m, dto.Synthese.TotalEncaisse);
        }

        [Fact]
        public async Task Controller_GetRapportCaisse_returns_forbid_for_admin_role()
        {
            var admin = MockUser(UserRoles.ADMIN, 1, 10, false);
            var controller = new CaissierDashboardController(
                new CaissierDashboardService(
                    BuildDb(nameof(Controller_GetRapportCaisse_returns_forbid_for_admin_role)),
                    NullLogger<CaissierDashboardService>.Instance,
                    admin.Object),
                admin.Object,
                NullLogger<CaissierDashboardController>.Instance);

            var result = await controller.GetRapportCaisse(null, null, null);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task Controller_GetRapportCaisse_returns_badrequest_when_half_interval()
        {
            await using var ctx = BuildDb(nameof(Controller_GetRapportCaisse_returns_badrequest_when_half_interval));
            var user = MockCaissierUser();
            var controller = new CaissierDashboardController(
                new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, user.Object),
                user.Object,
                NullLogger<CaissierDashboardController>.Instance);

            var result = await controller.GetRapportCaisse(null, DateTime.UtcNow.Date, null);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        private static void SeedBaseData(CongoTravelDbContext ctx, int caissierId)
        {
            ctx.Societes.Add(new Societe
            {
                IdSociete = 1,
                Nom = "Rusa Demo",
                CodeDevisePrincipale = "CDF",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = caissierId,
                NomComplet = "Caissier Test",
                Email = "caissier@test.local",
                MotDePasseHash = "x",
                IdSociete = 1,
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

            ctx.Voyages.Add(new Voyage
            {
                Id = 1,
                IdSociete = 1,
                IdVehicule = 1,
                IdDestination = 1,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 5000,
                PrixDevisePrincipale = 5000,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                Statut = true
            });

            ctx.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client Payé",
                AdresseClient = "A",
                Statut = true,
                IsActif = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Reservations.Add(new Reservation
            {
                IdReservation = 1,
                IdSociete = 1,
                IdClient = 1,
                IdUtilisateur = caissierId,
                IdVoyage = 1,
                DateReservation = DateTime.UtcNow,
                StatutReservation = "CONFIRMEE",
                Statut = true,
                NombreDePlace = 1
            });

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 1,
                IdSociete = 1,
                IdReservation = 1,
                IdUtilisateur = caissierId,
                MontantAPaye = 5000,
                MontantPaye = 5000,
                MontantAPayeDevisePrincipale = 5000,
                MontantPayeDevisePrincipale = 5000,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                DateCreation = DateTime.UtcNow,
                DatePaiement = DateTime.UtcNow,
                Statut = true,
                IsDeleted = false,
                MethodePaiement = "CASH"
            });
        }
    }
}
