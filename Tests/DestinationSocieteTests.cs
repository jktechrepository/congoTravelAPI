using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Destination;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class DestinationSocieteTests
    {
        private static CongoTravelDbContext BuildDb(string testName) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase($"DestinationSocieteTests_{testName}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static DestinationService BuildService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<DestinationService>.Instance);

        private static Mock<ICurrentUserService> MockUser(int societeId, bool isSuperAdmin = false)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.SocieteId).Returns(societeId);
            mock.SetupGet(x => x.IsSuperAdmin).Returns(isSuperAdmin);
            return mock;
        }

        private static Mock<IAuditService> MockAudit()
        {
            var mock = new Mock<IAuditService>();
            mock.Setup(x => x.LogCreateAsync(
                    It.IsAny<Destination>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()))
                .Returns(Task.CompletedTask);
            return mock;
        }

        private static DestinationController BuildController(
            DestinationService svc,
            CongoTravelDbContext ctx,
            Mock<ICurrentUserService> userMock)
        {
            var controller = new DestinationController(
                svc,
                MockAudit().Object,
                userMock.Object,
                ctx,
                NullLogger<DestinationController>.Instance);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        private static Destination NewDestination(int idSociete, string depart = "Kinshasa", string arrivee = "Goma") =>
            new()
            {
                VilleDepart = depart,
                VilleArrivee = arrivee,
                Montant = 50m,
                IdSociete = idSociete,
                Statut = true
            };

        [Fact]
        public async Task Create_allows_same_villes_across_different_societes()
        {
            await using var ctx = BuildDb(nameof(Create_allows_same_villes_across_different_societes));
            SeedSocietes(ctx, 1, 2);
            var svc = BuildService(ctx);

            await svc.CreateAsync(NewDestination(1));

            var created = await svc.CreateAsync(NewDestination(2));

            Assert.Equal(2, created.IdSociete);
            Assert.Equal(2, await ctx.Destinations.CountAsync());
        }

        [Fact]
        public async Task Create_rejects_duplicate_villes_within_same_societe()
        {
            await using var ctx = BuildDb(nameof(Create_rejects_duplicate_villes_within_same_societe));
            SeedSocietes(ctx, 1);
            var svc = BuildService(ctx);

            await svc.CreateAsync(NewDestination(1));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(NewDestination(1)));
        }

        [Fact]
        public async Task GetBySociete_returns_only_destinations_for_requested_societe()
        {
            await using var ctx = BuildDb(nameof(GetBySociete_returns_only_destinations_for_requested_societe));
            SeedSocietes(ctx, 1, 2);
            var svc = BuildService(ctx);

            await svc.CreateAsync(NewDestination(1, "Kinshasa", "Goma"));
            await svc.CreateAsync(NewDestination(2, "Kinshasa", "Goma"));
            await svc.CreateAsync(NewDestination(1, "Kinshasa", "Lubumbashi"));

            var societe1 = (await svc.GetBySocieteAsync(1)).ToList();

            Assert.Equal(2, societe1.Count);
            Assert.All(societe1, d => Assert.Equal(1, d.IdSociete));
        }

        [Fact]
        public async Task Update_rejects_duplicate_villes_within_same_societe()
        {
            await using var ctx = BuildDb(nameof(Update_rejects_duplicate_villes_within_same_societe));
            SeedSocietes(ctx, 1);
            var svc = BuildService(ctx);

            var first = await svc.CreateAsync(NewDestination(1, "Kinshasa", "Goma"));
            var second = await svc.CreateAsync(NewDestination(1, "Kinshasa", "Lubumbashi"));

            second.VilleArrivee = first.VilleArrivee;
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateAsync(second));
        }

        [Fact]
        public async Task Controller_Create_returns_403_when_societe_mismatch()
        {
            await using var ctx = BuildDb(nameof(Controller_Create_returns_403_when_societe_mismatch));
            SeedSocietes(ctx, 1, 2);

            var controller = BuildController(
                BuildService(ctx),
                ctx,
                MockUser(societeId: 1));

            var result = await controller.CreateDestination(new CreateDestinationDto
            {
                VilleDepart = "Kinshasa",
                VilleArrivee = "Goma",
                Montant = 50m,
                IdSociete = 2
            });

            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(403, status.StatusCode);
        }

        [Fact]
        public async Task Controller_Create_returns_409_on_intra_societe_duplicate()
        {
            await using var ctx = BuildDb(nameof(Controller_Create_returns_409_on_intra_societe_duplicate));
            SeedSocietes(ctx, 1);
            var svc = BuildService(ctx);
            await svc.CreateAsync(NewDestination(1));

            var controller = BuildController(svc, ctx, MockUser(societeId: 1));

            var result = await controller.CreateDestination(new CreateDestinationDto
            {
                VilleDepart = "Kinshasa",
                VilleArrivee = "Goma",
                Montant = 60m,
                IdSociete = 1
            });

            var status = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.NotNull(status.Value);
        }

        [Fact]
        public async Task Controller_Create_returns_201_for_cross_societe_duplicate_villes()
        {
            await using var ctx = BuildDb(nameof(Controller_Create_returns_201_for_cross_societe_duplicate_villes));
            SeedSocietes(ctx, 1, 2);
            var svc = BuildService(ctx);
            await svc.CreateAsync(NewDestination(1));

            var controller = BuildController(svc, ctx, MockUser(societeId: 2));

            var result = await controller.CreateDestination(new CreateDestinationDto
            {
                VilleDepart = "Kinshasa",
                VilleArrivee = "Goma",
                Montant = 55m,
                IdSociete = 2
            });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<DestinationResponseDto>(created.Value);
            Assert.Equal(2, dto.IdSociete);
            Assert.Equal("Kinshasa", dto.VilleDepart);
            Assert.Equal("Goma", dto.VilleArrivee);
        }

        private static void SeedSocietes(CongoTravelDbContext ctx, params int[] ids)
        {
            foreach (var id in ids)
            {
                ctx.Societes.Add(new Societe
                {
                    IdSociete = id,
                    Nom = $"Societe {id}",
                    CodeDevisePrincipale = "CDF",
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
            }

            ctx.SaveChanges();
        }
    }
}
