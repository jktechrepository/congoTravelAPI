using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using AutoMapper;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Mapping;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class TypeVehiculeSocieteTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static TypeVehiculeService BuildService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<TypeVehiculeService>.Instance);

        private static IMapper CreateMapper() =>
            new MapperConfiguration(
                cfg => cfg.AddProfile<VehiculeMappingProfile>(),
                NullLoggerFactory.Instance).CreateMapper();

        private static Mock<ICurrentUserService> MockUser(int societeId, bool isSuperAdmin = false)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.SocieteId).Returns(societeId);
            mock.SetupGet(x => x.IsSuperAdmin).Returns(isSuperAdmin);
            return mock;
        }

        [Fact]
        public async Task Create_allows_same_libelle_across_different_societes()
        {
            await using var ctx = BuildDb(nameof(Create_allows_same_libelle_across_different_societes));
            SeedSocietes(ctx, 1, 2);
            var svc = BuildService(ctx);

            await svc.CreateAsync(new TypeVehicule
            {
                Libelle = "VIP",
                IdSociete = 1,
                Statut = true
            });

            var created = await svc.CreateAsync(new TypeVehicule
            {
                Libelle = "VIP",
                IdSociete = 2,
                Statut = true
            });

            Assert.Equal(2, created.IdSociete);
            Assert.Equal(2, await ctx.TypeVehicules.CountAsync());
        }

        [Fact]
        public async Task Create_rejects_duplicate_libelle_within_same_societe()
        {
            await using var ctx = BuildDb(nameof(Create_rejects_duplicate_libelle_within_same_societe));
            SeedSocietes(ctx, 1);
            var svc = BuildService(ctx);

            await svc.CreateAsync(new TypeVehicule { Libelle = "Standard", IdSociete = 1, Statut = true });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(new TypeVehicule { Libelle = "Standard", IdSociete = 1, Statut = true }));
        }

        [Fact]
        public async Task GetBySociete_returns_only_types_for_requested_societe()
        {
            await using var ctx = BuildDb(nameof(GetBySociete_returns_only_types_for_requested_societe));
            SeedSocietes(ctx, 1, 2);
            var svc = BuildService(ctx);

            await svc.CreateAsync(new TypeVehicule { Libelle = "A", IdSociete = 1, Statut = true });
            await svc.CreateAsync(new TypeVehicule { Libelle = "B", IdSociete = 2, Statut = true });
            await svc.CreateAsync(new TypeVehicule { Libelle = "C", IdSociete = 1, Statut = true });

            var societe1 = await svc.GetBySocieteAsync(1);

            Assert.Equal(2, societe1.Count);
            Assert.All(societe1, t => Assert.Equal(1, t.IdSociete));
        }

        [Fact]
        public async Task Update_rejects_duplicate_libelle_within_same_societe()
        {
            await using var ctx = BuildDb(nameof(Update_rejects_duplicate_libelle_within_same_societe));
            SeedSocietes(ctx, 1);
            var svc = BuildService(ctx);

            var first = await svc.CreateAsync(new TypeVehicule { Libelle = "VIP", IdSociete = 1, Statut = true });
            var second = await svc.CreateAsync(new TypeVehicule { Libelle = "Standard", IdSociete = 1, Statut = true });

            second.Libelle = first.Libelle;
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateAsync(second));
        }

        [Fact]
        public async Task Controller_Create_returns_403_when_societe_mismatch()
        {
            await using var ctx = BuildDb(nameof(Controller_Create_returns_403_when_societe_mismatch));
            SeedSocietes(ctx, 1, 2);

            var controller = new TypeVehiculeController(
                BuildService(ctx),
                MockUser(societeId: 1).Object,
                CreateMapper(),
                NullLogger<TypeVehiculeController>.Instance);

            var result = await controller.Create(new CreateTypeVehiculeDto
            {
                Libelle = "VIP",
                IdSociete = 2,
                Statut = true
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
            await svc.CreateAsync(new TypeVehicule { Libelle = "VIP", IdSociete = 1, Statut = true });

            var controller = new TypeVehiculeController(
                svc,
                MockUser(societeId: 1).Object,
                CreateMapper(),
                NullLogger<TypeVehiculeController>.Instance);

            var result = await controller.Create(new CreateTypeVehiculeDto
            {
                Libelle = "VIP",
                IdSociete = 1,
                Statut = true
            });

            var status = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.NotNull(status.Value);
        }

        [Fact]
        public async Task Controller_Create_returns_201_for_cross_societe_duplicate_libelle()
        {
            await using var ctx = BuildDb(nameof(Controller_Create_returns_201_for_cross_societe_duplicate_libelle));
            SeedSocietes(ctx, 1, 2);
            var svc = BuildService(ctx);
            await svc.CreateAsync(new TypeVehicule { Libelle = "VIP", IdSociete = 1, Statut = true });

            var controller = new TypeVehiculeController(
                svc,
                MockUser(societeId: 2).Object,
                CreateMapper(),
                NullLogger<TypeVehiculeController>.Instance);

            var result = await controller.Create(new CreateTypeVehiculeDto
            {
                Libelle = "VIP",
                IdSociete = 2,
                Statut = true
            });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<TypeVehiculeResponseDto>(created.Value);
            Assert.Equal(2, dto.IdSociete);
            Assert.Equal("VIP", dto.Libelle);
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
