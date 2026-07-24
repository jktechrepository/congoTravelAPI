using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class AgentVisibilityFilterTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static AgentService CreateService(CongoTravelDbContext ctx, ICurrentUserService user) =>
            new(
                ctx,
                Mock.Of<IUsernameGeneratorService>(),
                Mock.Of<IEmailService>(),
                Mock.Of<IUtilisateurRepository>(),
                user,
                NullLogger<AgentService>.Instance);

        private static Mock<ICurrentUserService> MockUser(string primaryRole, int idSociete, bool isSuperAdmin = false)
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.PrimaryRole).Returns(primaryRole);
            user.SetupGet(u => u.UserRole).Returns(primaryRole);
            user.SetupGet(u => u.SocieteId).Returns(idSociete);
            user.SetupGet(u => u.IsSuperAdmin).Returns(isSuperAdmin);
            return user;
        }

        private static async Task<(int IdSociete1, int IdSociete2)> SeedAgentsAsync(CongoTravelDbContext ctx)
        {
            var s1 = new Societe { Nom = "Soc1", Statut = true, DateCreation = DateTime.UtcNow };
            var s2 = new Societe { Nom = "Soc2", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.AddRange(s1, s2);
            await ctx.SaveChangesAsync();

            ctx.Agents.AddRange(
                new Agent
                {
                    NomComplet = "Super Admin Agent",
                    EmailAgent = "sa@test.cd",
                    TelephoneAgent = "243900000001",
                    DateNaissance = new DateTime(1990, 1, 1),
                    RoleAgent = UserRoles.SUPER_ADMIN,
                    IdSociete = s1.IdSociete,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                },
                new Agent
                {
                    NomComplet = "Admin Agent",
                    EmailAgent = "admin-agent@test.cd",
                    TelephoneAgent = "243900000002",
                    DateNaissance = new DateTime(1990, 1, 1),
                    RoleAgent = UserRoles.ADMIN,
                    IdSociete = s1.IdSociete,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                },
                new Agent
                {
                    NomComplet = "Gerant Agent",
                    EmailAgent = "gerant@test.cd",
                    TelephoneAgent = "243900000003",
                    DateNaissance = new DateTime(1990, 1, 1),
                    RoleAgent = UserRoles.GERANT,
                    IdSociete = s1.IdSociete,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                },
                new Agent
                {
                    NomComplet = "Caissier Agent",
                    EmailAgent = "caissier@test.cd",
                    TelephoneAgent = "243900000004",
                    DateNaissance = new DateTime(1990, 1, 1),
                    RoleAgent = UserRoles.CAISSIER,
                    IdSociete = s1.IdSociete,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                },
                new Agent
                {
                    NomComplet = "Other Societe Caissier",
                    EmailAgent = "other@test.cd",
                    TelephoneAgent = "243900000005",
                    DateNaissance = new DateTime(1990, 1, 1),
                    RoleAgent = UserRoles.CAISSIER,
                    IdSociete = s2.IdSociete,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
            await ctx.SaveChangesAsync();
            return (s1.IdSociete, s2.IdSociete);
        }

        [Theory]
        [InlineData(UserRoles.SUPER_ADMIN, new[] { "Super-Admin", "Admin", "Gerant", "Caissier" })]
        [InlineData(UserRoles.ADMIN, new[] { "Admin", "Gerant", "Caissier" })]
        [InlineData(UserRoles.GERANT, new[] { "Gerant", "Caissier" })]
        [InlineData(UserRoles.CAISSIER, new[] { "Caissier" })]
        public void RoleVisibilityHelper_hidden_matrix(string caller, string[] expectedVisible)
        {
            var all = new[] { UserRoles.SUPER_ADMIN, UserRoles.ADMIN, UserRoles.GERANT, UserRoles.CAISSIER };
            var visible = all
                .Where(r => RoleVisibilityHelper.IsRoleVisibleToCaller(r, caller))
                .ToArray();
            Assert.Equal(expectedVisible, visible);
        }

        [Fact]
        public async Task GetAllAsync_gerant_hides_admin_and_super_admin()
        {
            await using var ctx = BuildDb(nameof(GetAllAsync_gerant_hides_admin_and_super_admin));
            var (idSociete, _) = await SeedAgentsAsync(ctx);
            var service = CreateService(ctx, MockUser(UserRoles.GERANT, idSociete).Object);

            var agents = (await service.GetAllAsync()).ToList();

            Assert.DoesNotContain(agents, a => a.RoleAgent == UserRoles.ADMIN);
            Assert.DoesNotContain(agents, a => a.RoleAgent == UserRoles.SUPER_ADMIN);
            Assert.Contains(agents, a => a.RoleAgent == UserRoles.GERANT);
            Assert.Contains(agents, a => a.RoleAgent == UserRoles.CAISSIER);
        }

        [Fact]
        public async Task GetAllAsync_admin_hides_super_admin_only()
        {
            await using var ctx = BuildDb(nameof(GetAllAsync_admin_hides_super_admin_only));
            var (idSociete, _) = await SeedAgentsAsync(ctx);
            var service = CreateService(ctx, MockUser(UserRoles.ADMIN, idSociete).Object);

            var agents = (await service.GetAllAsync()).ToList();

            Assert.DoesNotContain(agents, a => a.RoleAgent == UserRoles.SUPER_ADMIN);
            Assert.Contains(agents, a => a.RoleAgent == UserRoles.ADMIN);
        }

        [Fact]
        public async Task GetAllAsync_super_admin_sees_all_roles_and_societes()
        {
            await using var ctx = BuildDb(nameof(GetAllAsync_super_admin_sees_all_roles_and_societes));
            await SeedAgentsAsync(ctx);
            var service = CreateService(ctx, MockUser(UserRoles.SUPER_ADMIN, 0, isSuperAdmin: true).Object);

            var agents = (await service.GetAllAsync()).ToList();

            Assert.Equal(5, agents.Count);
        }

        [Fact]
        public async Task GetAllAsync_non_super_admin_scoped_to_jwt_societe()
        {
            await using var ctx = BuildDb(nameof(GetAllAsync_non_super_admin_scoped_to_jwt_societe));
            var (idSociete1, _) = await SeedAgentsAsync(ctx);
            var service = CreateService(ctx, MockUser(UserRoles.GERANT, idSociete1).Object);

            var agents = (await service.GetAllAsync()).ToList();

            Assert.All(agents, a => Assert.Equal(idSociete1, a.IdSociete));
            Assert.DoesNotContain(agents, a => a.EmailAgent == "other@test.cd");
        }

        [Fact]
        public async Task GetByIdAsync_returns_null_for_hidden_role()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_returns_null_for_hidden_role));
            var (idSociete, _) = await SeedAgentsAsync(ctx);
            var adminId = ctx.Agents.Single(a => a.RoleAgent == UserRoles.ADMIN).IdAgent;
            var service = CreateService(ctx, MockUser(UserRoles.GERANT, idSociete).Object);

            var agent = await service.GetByIdAsync(adminId);

            Assert.Null(agent);
        }
    }
}
