using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Mapping;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class SuperAdminDashboardTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static FinancierDashboardService CreateFinancierService(CongoTravelDbContext ctx, bool isSuperAdmin = true)
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(x => x.IsSuperAdmin).Returns(isSuperAdmin);
            user.SetupGet(x => x.HasFinanceAccess).Returns(true);
            user.SetupGet(x => x.SocieteId).Returns(1);
            return DashboardEnrichmentTestHelper.CreateFinancierDashboardService(ctx, user.Object);
        }

        private static SuperAdminDashboardService CreateDashboardService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                CreateFinancierService(ctx),
                DashboardEnrichmentTestHelper.CreateEvenementDashboardMock().Object,
                new ReservationService(
                    ctx,
                    Mock.Of<IConfigSocieteRepository>(),
                    NullLogger<ReservationService>.Instance),
                new MapperConfiguration(
                    cfg => cfg.AddProfile<VehiculeMappingProfile>(),
                    NullLoggerFactory.Instance).CreateMapper(),
                NullLogger<SuperAdminDashboardService>.Instance);

        [Fact]
        public async Task GetDashboardDataAsync_aggregates_global_and_societe_stats()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_aggregates_global_and_societe_stats));
            SeedTransportData(ctx);
            await ctx.SaveChangesAsync();

            var svc = CreateDashboardService(ctx);

            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(1, result.GlobalStatistiques.TotalSocietes);
            Assert.Equal(1, result.GlobalStatistiques.SocietesActives);
            Assert.Equal(1, result.GlobalStatistiques.TotalClient);
            Assert.Equal(1, result.GlobalStatistiques.TotalClientActif);
            Assert.Equal(1, result.GlobalStatistiques.TotalReservation);
            Assert.Equal(1, result.GlobalStatistiques.TotalVoyagesActifs);
            Assert.Equal(1, result.GlobalStatistiques.TotalReservationsConfirmeesMois);
            Assert.Equal(1, result.GlobalStatistiques.TotalBilletsEmisMois);
            Assert.Equal(5000m, result.GlobalStatistiques.ChiffreAffairesMois);
            Assert.Single(result.Societes);
            Assert.Single(result.Top5SocietesCa);
            Assert.Equal(1, result.Top5SocietesCa[0].Rang);
            Assert.NotNull(result.Reservations);
            Assert.Single(result.Reservations.Data);
            Assert.Equal(1, result.Reservations.TotalCount);
            Assert.Equal(1, result.Reservations.PageNumber);
            Assert.Equal(20, result.Reservations.PageSize);
            Assert.Equal(3, result.CollecteParOrigineGroupe.Count);
            Assert.Equal(5000m, result.CollecteOrigineGroupeSynthese.MontantClassifie);
        }

        [Fact]
        public async Task GetDashboardDataAsync_total_client_actif_counts_distinct_clients_with_reservation()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_total_client_actif_counts_distinct_clients_with_reservation));
            SeedTransportData(ctx);

            ctx.Clients.Add(new Client
            {
                IdClient = 2,
                NomClient = "Client B sans reservation",
                AdresseClient = "Adresse",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            await ctx.SaveChangesAsync();

            var svc = CreateDashboardService(ctx);

            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(2, result.GlobalStatistiques.TotalClient);
            Assert.Equal(1, result.GlobalStatistiques.TotalClientActif);
        }

        [Fact]
        public async Task GetDashboardDataAsync_total_reservation_counts_only_active_statut()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_total_reservation_counts_only_active_statut));
            SeedTransportData(ctx);

            ctx.Reservations.Add(new Reservation
            {
                IdReservation = 2,
                IdVoyage = 1,
                IdClient = 1,
                IdUtilisateur = 1,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                DateCreation = DateTime.UtcNow,
                Statut = false,
                StatutReservation = "ANNULEE",
                NombreDePlace = 1
            });

            await ctx.SaveChangesAsync();

            var svc = CreateDashboardService(ctx);
            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(1, result.GlobalStatistiques.TotalReservation);
            Assert.Equal(2, result.Reservations.TotalCount);
        }

        [Fact]
        public async Task GetDashboardDataAsync_reservations_respects_pagination_query()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_reservations_respects_pagination_query));
            SeedTransportData(ctx);

            ctx.Reservations.Add(new Reservation
            {
                IdReservation = 2,
                IdVoyage = 1,
                IdClient = 1,
                IdUtilisateur = 1,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow.AddDays(-1),
                DateCreation = DateTime.UtcNow.AddDays(-1),
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 2
            });

            await ctx.SaveChangesAsync();

            var svc = CreateDashboardService(ctx);
            var result = await svc.GetDashboardDataAsync(new PagedRequest { PageNumber = 1, PageSize = 1 });

            Assert.Equal(2, result.Reservations.TotalCount);
            Assert.Single(result.Reservations.Data);
            Assert.Equal(1, result.Reservations.PageSize);
        }

        [Fact]
        public async Task ReservationService_GetPagedAsync_returns_seeded_reservation()
        {
            await using var ctx = BuildDb(nameof(ReservationService_GetPagedAsync_returns_seeded_reservation));
            SeedTransportData(ctx);
            await ctx.SaveChangesAsync();

            Assert.Equal(1, await ctx.Reservations.CountAsync());

            var repo = new ReservationService(
                ctx,
                Mock.Of<IConfigSocieteRepository>(),
                NullLogger<ReservationService>.Instance);

            var paged = await repo.GetPagedAsync(new PagedRequest());

            Assert.Equal(1, paged.TotalCount);
            Assert.NotEmpty(paged.Data);
        }

        [Fact]
        public async Task Controller_returns_forbid_when_not_super_admin()
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.IsSuperAdmin).Returns(false);
            currentUser.SetupGet(x => x.UserId).Returns(2);

            var controller = new SuperAdminDashboardController(
                CreateDashboardService(BuildDb(nameof(Controller_returns_forbid_when_not_super_admin))),
                currentUser.Object,
                NullLogger<SuperAdminDashboardController>.Instance);

            var result = await controller.GetSuperAdminDashboard(null);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task Controller_returns_ok_when_super_admin()
        {
            await using var ctx = BuildDb(nameof(Controller_returns_ok_when_super_admin));
            SeedTransportData(ctx);
            await ctx.SaveChangesAsync();

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.IsSuperAdmin).Returns(true);
            currentUser.SetupGet(x => x.UserId).Returns(1);

            var controller = new SuperAdminDashboardController(
                CreateDashboardService(ctx),
                currentUser.Object,
                NullLogger<SuperAdminDashboardController>.Instance);

            var result = await controller.GetSuperAdminDashboard(null);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        private static void SeedTransportData(CongoTravelDbContext ctx)
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

            ctx.Voyages.Add(new Voyage
            {
                Id = 1,
                IdSociete = 1,
                IdVehicule = 1,
                IdDestination = 1,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 5000,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 5000,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.TypeVehicules.Add(new TypeVehicule
            {
                IdTypeVehicule = 1,
                Libelle = "Bus",
                IdSociete = 1,
                Statut = true
            });

            ctx.Vehicules.Add(new Vehicule
            {
                IdVehicule = 1,
                AliasVehicule = "BUS-1",
                NombreSiege = 20,
                IdSociete = 1,
                IdTypeVehicule = 1,
                Statut = true
            });

            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 1,
                NomComplet = "Agent Test",
                Email = "agent@test.com",
                MotDePasseHash = "hash",
                Statut = true,
                IdSociete = 1
            });

            ctx.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client A",
                AdresseClient = "Adresse",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Reservations.Add(new Reservation
            {
                IdReservation = 1,
                IdVoyage = 1,
                IdClient = 1,
                IdUtilisateur = 1,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                DateCreation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            });

            ctx.Billets.Add(new Billet
            {
                IdBillet = 1,
                IdSociete = 1,
                IdReservation = 1,
                QrCode = "QR-1",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false
            });

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 1,
                IdSociete = 1,
                IdReservation = 1,
                MontantAPaye = 5000m,
                MontantPaye = 5000m,
                MontantAPayeDevisePrincipale = 5000m,
                MontantPayeDevisePrincipale = 5000m,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                Statut = true,
                DateCreation = DateTime.UtcNow,
                DatePaiement = DateTime.UtcNow,
                Origine = Models.Enums.OrigineOperation.CAISSIER,
                IdUtilisateur = 1
            });
        }
    }
}
