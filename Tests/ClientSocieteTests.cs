using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Client;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class ClientSocieteTests
    {
        private static CongoTravelDbContext BuildDb(string testName) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase($"ClientSocieteTests_{testName}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static ClientService BuildService(CongoTravelDbContext ctx)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FrontendSettings:BaseUrl"] = "https://test.local"
                })
                .Build();

            return new ClientService(
                ctx,
                new Mock<IEmailService>().Object,
                new Mock<IEmailVerificationService>().Object,
                new Mock<ISmsNotificationService>().Object,
                new Mock<IUtilisateurRepository>().Object,
                NullLogger<ClientService>.Instance,
                config);
        }

        private static Mock<ICurrentUserService> MockUser(int societeId, bool isSuperAdmin = false)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.SocieteId).Returns(societeId);
            mock.SetupGet(x => x.IsSuperAdmin).Returns(isSuperAdmin);
            return mock;
        }

        private static ClientController BuildController(
            IClientRepository repo,
            CongoTravelDbContext ctx,
            Mock<ICurrentUserService> userMock)
        {
            var controller = new ClientController(
                new Mock<IAuditService>().Object,
                userMock.Object,
                null!,
                repo,
                ctx,
                NullLogger<ClientController>.Instance,
                new Mock<IEmailVerificationService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            return controller;
        }

        private static async Task SeedClientsAndReservationsAsync(CongoTravelDbContext ctx)
        {
            ctx.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "Client Soc1", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow },
                new Client { IdClient = 2, NomClient = "Client Soc2", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow },
                new Client { IdClient = 3, NomClient = "Sans reservation", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow },
                new Client { IdClient = 4, NomClient = "Client Les Deux", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow }
            );

            ctx.Reservations.AddRange(
                new Reservation
                {
                    IdReservation = 1,
                    IdClient = 1,
                    IdSociete = 1,
                    IdUtilisateur = 1,
                    IdVoyage = 1,
                    StatutReservation = "CONFIRMEE",
                    Statut = true,
                    DateReservation = DateTime.UtcNow.Date,
                    NombreDePlace = 1
                },
                new Reservation
                {
                    IdReservation = 2,
                    IdClient = 2,
                    IdSociete = 2,
                    IdUtilisateur = 1,
                    IdVoyage = 2,
                    StatutReservation = "CONFIRMEE",
                    Statut = true,
                    DateReservation = DateTime.UtcNow.Date,
                    NombreDePlace = 1
                },
                new Reservation
                {
                    IdReservation = 3,
                    IdClient = 4,
                    IdSociete = 1,
                    IdUtilisateur = 1,
                    IdVoyage = 1,
                    StatutReservation = "EN_ATTENTE",
                    Statut = true,
                    DateReservation = DateTime.UtcNow.Date,
                    NombreDePlace = 1
                },
                new Reservation
                {
                    IdReservation = 4,
                    IdClient = 4,
                    IdSociete = 2,
                    IdUtilisateur = 1,
                    IdVoyage = 2,
                    StatutReservation = "CONFIRMEE",
                    Statut = true,
                    DateReservation = DateTime.UtcNow.Date,
                    NombreDePlace = 1
                },
                new Reservation
                {
                    IdReservation = 5,
                    IdClient = 1,
                    IdSociete = 1,
                    IdUtilisateur = 1,
                    IdVoyage = 1,
                    StatutReservation = "ANNULEE",
                    Statut = false,
                    DateReservation = DateTime.UtcNow.Date,
                    NombreDePlace = 1
                }
            );

            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task GetBySocieteAsync_returns_only_clients_with_active_reservations_in_societe()
        {
            await using var ctx = BuildDb(nameof(GetBySocieteAsync_returns_only_clients_with_active_reservations_in_societe));
            await SeedClientsAndReservationsAsync(ctx);
            var svc = BuildService(ctx);

            var societe1 = (await svc.GetBySocieteAsync(1)).Select(c => c.IdClient).OrderBy(x => x).ToList();
            var societe2 = (await svc.GetBySocieteAsync(2)).Select(c => c.IdClient).OrderBy(x => x).ToList();

            Assert.Equal(new[] { 1, 4 }, societe1);
            Assert.Equal(new[] { 2, 4 }, societe2);
            Assert.DoesNotContain(3, societe1);
            Assert.DoesNotContain(3, societe2);
        }

        [Fact]
        public async Task GetBySocietePagedAsync_filters_by_societe_reservations()
        {
            await using var ctx = BuildDb(nameof(GetBySocietePagedAsync_filters_by_societe_reservations));
            await SeedClientsAndReservationsAsync(ctx);
            var svc = BuildService(ctx);

            var result = await svc.GetBySocietePagedAsync(1, new ClientPagedSearchRequestDto
            {
                PageNumber = 1,
                PageSize = 20
            });

            Assert.Equal(2, result.TotalCount);
            Assert.Contains(result.Data, c => c.IdClient == 1);
            Assert.Contains(result.Data, c => c.IdClient == 4);
        }

        [Fact]
        public async Task GetBySocieteAndSearchAsync_filters_by_societe_and_search_term()
        {
            await using var ctx = BuildDb(nameof(GetBySocieteAndSearchAsync_filters_by_societe_and_search_term));
            await SeedClientsAndReservationsAsync(ctx);
            var svc = BuildService(ctx);

            var result = (await svc.GetBySocieteAndSearchAsync(1, "Les Deux", includeInactive: false)).ToList();

            Assert.Single(result);
            Assert.Equal(4, result[0].IdClient);
        }

        [Fact]
        public async Task GetClientsBySociete_returns_forbidden_when_token_societe_mismatch()
        {
            await using var ctx = BuildDb(nameof(GetClientsBySociete_returns_forbidden_when_token_societe_mismatch));
            await SeedClientsAndReservationsAsync(ctx);
            var svc = BuildService(ctx);
            var controller = BuildController(svc, ctx, MockUser(societeId: 1));

            var action = await controller.GetClientsBySociete(2);

            Assert.IsType<ObjectResult>(action.Result);
            var obj = (ObjectResult)action.Result!;
            Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
        }

        [Fact]
        public async Task GetClientsBySociete_returns_idSociete_in_response()
        {
            await using var ctx = BuildDb(nameof(GetClientsBySociete_returns_idSociete_in_response));
            await SeedClientsAndReservationsAsync(ctx);
            var svc = BuildService(ctx);
            var controller = BuildController(svc, ctx, MockUser(societeId: 1, isSuperAdmin: true));

            var action = await controller.GetClientsBySociete(1);
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var payload = Assert.IsAssignableFrom<IEnumerable<ClientResponseDto>>(ok.Value).ToList();

            Assert.All(payload, c => Assert.Equal(1, c.IdSociete));
        }
    }
}
