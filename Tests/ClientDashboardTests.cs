using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class ClientDashboardTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockClientUser(
            int clientId = 1,
            int userId = 10,
            int societeId = 0) =>
            MockUser(UserRoles.CLIENT, clientId, userId, societeId, isSuperAdmin: false);

        private static Mock<ICurrentUserService> MockUser(
            string role,
            int clientId,
            int userId,
            int societeId,
            bool isSuperAdmin)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.UserRole).Returns(role);
            mock.SetupGet(x => x.IsSuperAdmin).Returns(isSuperAdmin);
            mock.SetupGet(x => x.ClientId).Returns(clientId);
            mock.SetupGet(x => x.UserId).Returns(userId);
            mock.SetupGet(x => x.SocieteId).Returns(societeId);
            mock.Setup(x => x.GetSocieteId()).Returns(societeId);
            return mock;
        }

        [Fact]
        public async Task GetDashboardDataAsync_returns_client_scoped_metrics()
        {
            await using var ctx = BuildDb($"{nameof(ClientDashboardTests)}_{nameof(GetDashboardDataAsync_returns_client_scoped_metrics)}");
            SeedBaseData(ctx, clientId: 1, societeId: 1);
            ctx.Reservations.Add(new Reservation
            {
                IdReservation = 2,
                IdSociete = 1,
                IdClient = 2,
                IdUtilisateur = 1,
                IdVoyage = 1,
                DateReservation = DateTime.UtcNow,
                StatutReservation = "CONFIRMEE",
                Statut = true,
                NombreDePlace = 1
            });
            await ctx.SaveChangesAsync();

            var svc = new ClientDashboardService(
                ctx, NullLogger<ClientDashboardService>.Instance, MockClientUser().Object);

            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(1, result.Statistiques.NombreReservations);
            Assert.Single(result.ReservationsRecentes);
            Assert.Equal("CDF", result.CodeDevisePrincipale);
        }

        [Fact]
        public async Task GetDashboardDataAsync_uses_montant_paye_devise_principale()
        {
            await using var ctx = BuildDb($"{nameof(ClientDashboardTests)}_{nameof(GetDashboardDataAsync_uses_montant_paye_devise_principale)}");
            SeedBaseData(ctx, clientId: 1, societeId: 1);
            await ctx.SaveChangesAsync();
            var paiement = ctx.Paiements.First();
            paiement.MontantPaye = 100m;
            paiement.MontantPayeDevisePrincipale = 5000m;
            await ctx.SaveChangesAsync();

            var svc = new ClientDashboardService(
                ctx, NullLogger<ClientDashboardService>.Instance, MockClientUser().Object);

            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(5000m, result.Statistiques.MontantTotalPaye);
            Assert.Equal(5000m, result.PaiementsRecents.First().MontantPaye);
        }

        [Fact]
        public async Task GetDashboardDataAsync_paiements_not_filtered_by_wrong_societe()
        {
            await using var ctx = BuildDb($"{nameof(ClientDashboardTests)}_{nameof(GetDashboardDataAsync_paiements_not_filtered_by_wrong_societe)}");
            SeedBaseData(ctx, clientId: 1, societeId: 2);
            await ctx.SaveChangesAsync();

            var user = MockClientUser(clientId: 1, societeId: 99);
            var svc = new ClientDashboardService(
                ctx, NullLogger<ClientDashboardService>.Instance, user.Object);

            var result = await svc.GetDashboardDataAsync();

            Assert.Single(result.PaiementsRecents);
        }

        [Fact]
        public async Task Controller_returns_forbid_for_admin_role()
        {
            var admin = MockUser(UserRoles.ADMIN, clientId: 0, userId: 10, societeId: 1, isSuperAdmin: false);
            var controller = new ClientDashboardController(
                new ClientDashboardService(
                    BuildDb($"{nameof(ClientDashboardTests)}_{nameof(Controller_returns_forbid_for_admin_role)}"),
                    NullLogger<ClientDashboardService>.Instance,
                    admin.Object),
                admin.Object,
                NullLogger<ClientDashboardController>.Instance);

            var result = await controller.GetClientDashboard();
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task Controller_returns_forbid_when_client_id_unresolvable()
        {
            var user = MockUser(UserRoles.CLIENT, clientId: 0, userId: 0, societeId: 0, isSuperAdmin: false);
            var controller = new ClientDashboardController(
                new ClientDashboardService(
                    BuildDb($"{nameof(ClientDashboardTests)}_{nameof(Controller_returns_forbid_when_client_id_unresolvable)}"),
                    NullLogger<ClientDashboardService>.Instance,
                    user.Object),
                user.Object,
                NullLogger<ClientDashboardController>.Instance);

            var result = await controller.GetClientDashboard();
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(403, status.StatusCode);
        }

        [Fact]
        public async Task Controller_returns_ok_for_client()
        {
            await using var ctx = BuildDb($"{nameof(ClientDashboardTests)}_{nameof(Controller_returns_ok_for_client)}");
            SeedBaseData(ctx, clientId: 1, societeId: 1);
            await ctx.SaveChangesAsync();

            var user = MockClientUser();
            var controller = new ClientDashboardController(
                new ClientDashboardService(ctx, NullLogger<ClientDashboardService>.Instance, user.Object),
                user.Object,
                NullLogger<ClientDashboardController>.Instance);

            var result = await controller.GetClientDashboard();
            Assert.IsType<OkObjectResult>(result.Result);
        }

        private static void SeedBaseData(CongoTravelDbContext ctx, int clientId, int societeId)
        {
            ctx.Societes.Add(new Societe
            {
                IdSociete = societeId,
                Nom = "Rusa Demo",
                CodeDevisePrincipale = "CDF",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Clients.Add(new Client
            {
                IdClient = clientId,
                NomClient = "Client Test",
                AdresseClient = "A",
                Statut = true,
                IsActif = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Destinations.Add(new Destination
            {
                IdDestination = 1,
                IdSociete = societeId,
                VilleDepart = "Kinshasa",
                VilleArrivee = "Goma",
                Statut = true
            });

            ctx.Vehicules.Add(new Vehicule
            {
                IdVehicule = 1,
                IdSociete = societeId,
                IdTypeVehicule = 1,
                AliasVehicule = "BUS-1",
                NombreSiege = 20,
                Statut = true
            });

            ctx.Voyages.Add(new Voyage
            {
                Id = 1,
                IdSociete = societeId,
                IdVehicule = 1,
                IdDestination = 1,
                DateDepart = DateTime.UtcNow.Date.AddDays(7),
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 5000,
                PrixDevisePrincipale = 5000,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                Statut = true
            });

            ctx.Reservations.Add(new Reservation
            {
                IdReservation = 1,
                IdSociete = societeId,
                IdClient = clientId,
                IdUtilisateur = 1,
                IdVoyage = 1,
                DateReservation = DateTime.UtcNow,
                StatutReservation = "CONFIRMEE",
                Statut = true,
                NombreDePlace = 1
            });

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 1,
                IdSociete = societeId,
                IdReservation = 1,
                IdUtilisateur = 1,
                MontantAPaye = 5000,
                MontantPaye = 5000,
                MontantAPayeDevisePrincipale = 5000,
                MontantPayeDevisePrincipale = 5000,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                DateCreation = DateTime.UtcNow,
                DatePaiement = DateTime.UtcNow,
                Statut = true,
                IsDeleted = false
            });
        }
    }
}
