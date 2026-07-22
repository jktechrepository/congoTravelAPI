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
    public class StatistiquesTransportTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockAdminUser(int societeId = 1) =>
            MockUser(UserRoles.ADMIN, societeId, isSuperAdmin: false);

        private static Mock<ICurrentUserService> MockSuperAdminUser(int societeId = 1) =>
            MockUser(UserRoles.SUPER_ADMIN, societeId, isSuperAdmin: true);

        private static Mock<ICurrentUserService> MockUser(string role, int societeId, bool isSuperAdmin)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.UserRole).Returns(role);
            mock.SetupGet(x => x.IsSuperAdmin).Returns(isSuperAdmin);
            mock.SetupGet(x => x.SocieteId).Returns(societeId);
            mock.SetupGet(x => x.UserId).Returns(10);
            return mock;
        }

        [Fact]
        public async Task GetStatistiquesAsync_returns_transport_kpis_for_societe()
        {
            await using var ctx = BuildDb($"{nameof(StatistiquesTransportTests)}_{nameof(GetStatistiquesAsync_returns_transport_kpis_for_societe)}");
            SeedData(ctx, societeId: 1);
            await ctx.SaveChangesAsync();

            var svc = new StatistiquesService(ctx, NullLogger<StatistiquesService>.Instance);
            var result = await svc.GetStatistiquesAsync(1);

            Assert.Equal("CDF", result.CodeDevisePrincipale);
            Assert.Equal(5000m, result.Financieres.ChiffreAffaires);
            Assert.Equal(1, result.Generales.TotalReservations);
            Assert.Equal(12, result.Financieres.EvolutionMensuelle.Count);
            Assert.NotEmpty(result.Operationnelles.RepartitionParDestination);
        }

        [Fact]
        public async Task GetStatistiquesAsync_uses_montant_paye_devise_principale()
        {
            await using var ctx = BuildDb($"{nameof(StatistiquesTransportTests)}_{nameof(GetStatistiquesAsync_uses_montant_paye_devise_principale)}");
            SeedData(ctx, societeId: 1);
            await ctx.SaveChangesAsync();

            var paiement = ctx.Paiements.First();
            paiement.MontantPaye = 100m;
            paiement.MontantPayeDevisePrincipale = 5000m;
            await ctx.SaveChangesAsync();

            var svc = new StatistiquesService(ctx, NullLogger<StatistiquesService>.Instance);
            var result = await svc.GetStatistiquesAsync(1);

            Assert.Equal(5000m, result.Financieres.ChiffreAffaires);
            Assert.Equal(5000m, result.Generales.TotalPaiements);
        }

        [Fact]
        public async Task GetStatistiquesAsync_respects_date_period()
        {
            await using var ctx = BuildDb($"{nameof(StatistiquesTransportTests)}_{nameof(GetStatistiquesAsync_respects_date_period)}");
            SeedData(ctx, societeId: 1);

            var oldReservation = new Reservation
            {
                IdReservation = 99,
                IdSociete = 1,
                IdClient = 1,
                IdUtilisateur = 1,
                IdVoyage = 1,
                DateReservation = new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                StatutReservation = "CONFIRMEE",
                Statut = true,
                NombreDePlace = 1
            };
            ctx.Reservations.Add(oldReservation);
            await ctx.SaveChangesAsync();

            var svc = new StatistiquesService(ctx, NullLogger<StatistiquesService>.Instance);
            var debut = DateTime.UtcNow.AddDays(-1);
            var fin = DateTime.UtcNow.AddDays(1);
            var result = await svc.GetStatistiquesAsync(1, debut, fin);

            Assert.Equal(1, result.Generales.TotalReservations);
        }

        [Fact]
        public async Task Controller_returns_forbid_when_societe_mismatch()
        {
            var user = MockAdminUser(societeId: 2);
            var controller = new StatistiquesController(
                new StatistiquesService(
                    BuildDb($"{nameof(StatistiquesTransportTests)}_{nameof(Controller_returns_forbid_when_societe_mismatch)}"),
                    NullLogger<StatistiquesService>.Instance),
                user.Object,
                NullLogger<StatistiquesController>.Instance);

            var result = await controller.GetStatistiques(1);
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(403, status.StatusCode);
        }

        [Fact]
        public async Task Controller_returns_ok_for_super_admin_any_societe()
        {
            await using var ctx = BuildDb($"{nameof(StatistiquesTransportTests)}_{nameof(Controller_returns_ok_for_super_admin_any_societe)}");
            SeedData(ctx, societeId: 1);
            await ctx.SaveChangesAsync();

            var controller = new StatistiquesController(
                new StatistiquesService(ctx, NullLogger<StatistiquesService>.Instance),
                MockSuperAdminUser(societeId: 99).Object,
                NullLogger<StatistiquesController>.Instance);

            var result = await controller.GetStatistiques(1);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task Controller_returns_ok_for_admin_matching_societe()
        {
            await using var ctx = BuildDb($"{nameof(StatistiquesTransportTests)}_{nameof(Controller_returns_ok_for_admin_matching_societe)}");
            SeedData(ctx, societeId: 1);
            await ctx.SaveChangesAsync();

            var controller = new StatistiquesController(
                new StatistiquesService(ctx, NullLogger<StatistiquesService>.Instance),
                MockAdminUser(1).Object,
                NullLogger<StatistiquesController>.Instance);

            var result = await controller.GetStatistiques(1);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        private static void SeedData(CongoTravelDbContext ctx, int societeId)
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
                IdClient = 1,
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

            ctx.TypeVehicules.Add(new TypeVehicule
            {
                IdTypeVehicule = 1,
                Libelle = "Standard",
                IdSociete = societeId,
                Statut = true,
                DateCreation = DateTime.UtcNow
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
                DateDepart = DateTime.UtcNow.Date,
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
                IdClient = 1,
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
                MethodePaiement = "CASH",
                DateCreation = DateTime.UtcNow,
                DatePaiement = DateTime.UtcNow,
                Statut = true,
                IsDeleted = false
            });
        }
    }
}
