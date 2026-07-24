using System.Security.Claims;
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
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class UtilisateurToggleStatutTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static UtilisateurService BuildService(CongoTravelDbContext ctx) => new(ctx);

        private static UtilisateurController BuildController(
            IUtilisateurRepository repo,
            CongoTravelDbContext ctx,
            IPermissionService permissionService,
            IRefreshTokenService refreshTokenService,
            int authenticatedUserId)
        {
            var controller = new UtilisateurController(
                repo,
                new Mock<IUserDeviceRepository>().Object,
                new Mock<ISimpleJwtService>().Object,
                permissionService,
                refreshTokenService,
                new Mock<IGoogleAuthService>().Object,
                new Mock<IAppleAuthService>().Object,
                new ConfigurationBuilder().Build(),
                NullLogger<UtilisateurController>.Instance,
                ctx,
                new Mock<IAuditService>().Object,
                new Mock<IEmailService>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, authenticatedUserId.ToString()) },
                            "TestAuth"))
                    }
                }
            };
            return controller;
        }

        private static async Task<(Role clientRole, Role adminRole)> SeedRolesAsync(CongoTravelDbContext ctx)
        {
            var clientRole = new Role { IdRole = 1, Nom = "Client", Statut = true };
            var adminRole = new Role { IdRole = 2, Nom = "Admin", Statut = true };
            ctx.Roles.AddRange(clientRole, adminRole);
            await ctx.SaveChangesAsync();
            return (clientRole, adminRole);
        }

        [Fact]
        public async Task DeactivateAsync_sets_statut_false_when_active()
        {
            await using var ctx = BuildDb(nameof(DeactivateAsync_sets_statut_false_when_active));
            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 1,
                NomComplet = "Test",
                Email = "t@test.com",
                MotDePasseHash = "hash",
                Statut = true,
                IsConnecte = true
            });
            await ctx.SaveChangesAsync();

            var svc = BuildService(ctx);
            var ok = await svc.DeactivateAsync(1);

            Assert.True(ok);
            var user = await ctx.Utilisateurs.FindAsync(1);
            Assert.False(user!.Statut);
            Assert.False(user.IsConnecte);
        }

        [Fact]
        public async Task DeactivateAsync_returns_false_when_already_inactive()
        {
            await using var ctx = BuildDb(nameof(DeactivateAsync_returns_false_when_already_inactive));
            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 2,
                NomComplet = "Inactif",
                Email = "i@test.com",
                MotDePasseHash = "hash",
                Statut = false
            });
            await ctx.SaveChangesAsync();

            var svc = BuildService(ctx);
            Assert.False(await svc.DeactivateAsync(2));
        }

        [Fact]
        public async Task ToggleStatut_self_with_DeactivateSelf_deactivates_account()
        {
            var db = nameof(ToggleStatut_self_with_DeactivateSelf_deactivates_account);
            await using var ctx = BuildDb(db);
            var (clientRole, _) = await SeedRolesAsync(ctx);
            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 10,
                NomComplet = "Client User",
                Email = "client@test.com",
                MotDePasseHash = "hash",
                Statut = true,
                IdRole = clientRole.IdRole,
                IdSociete = 1
            });
            await ctx.SaveChangesAsync();

            var perm = new Mock<IPermissionService>();
            perm.Setup(p => p.UserHasPermissionAsync(10, "Utilisateur.DeactivateSelf")).ReturnsAsync(true);

            var refresh = new Mock<IRefreshTokenService>();
            refresh.Setup(r => r.RevokeAllRefreshTokensAsync(10)).ReturnsAsync(true);

            var svc = BuildService(ctx);
            var controller = BuildController(svc, ctx, perm.Object, refresh.Object, authenticatedUserId: 10);

            var action = await controller.ToggleStatut(10);
            var ok = Assert.IsType<OkObjectResult>(action.Result);

            var user = await ctx.Utilisateurs.FindAsync(10);
            Assert.False(user!.Statut);
            refresh.Verify(r => r.RevokeAllRefreshTokensAsync(10), Times.Once);
        }

        [Fact]
        public async Task ToggleStatut_self_without_permission_returns_403()
        {
            await using var ctx = BuildDb(nameof(ToggleStatut_self_without_permission_returns_403));
            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 11,
                NomComplet = "No Perm",
                Email = "noperm@test.com",
                MotDePasseHash = "hash",
                Statut = true
            });
            await ctx.SaveChangesAsync();

            var perm = new Mock<IPermissionService>();
            perm.Setup(p => p.UserHasPermissionAsync(11, "Utilisateur.DeactivateSelf")).ReturnsAsync(false);

            var svc = BuildService(ctx);
            var controller = BuildController(svc, ctx, perm.Object, new Mock<IRefreshTokenService>().Object, 11);

            var action = await controller.ToggleStatut(11);
            var result = Assert.IsType<ObjectResult>(action.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task ToggleStatut_self_already_inactive_returns_400()
        {
            await using var ctx = BuildDb(nameof(ToggleStatut_self_already_inactive_returns_400));
            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 12,
                NomComplet = "Inactive",
                Email = "inactive@test.com",
                MotDePasseHash = "hash",
                Statut = false
            });
            await ctx.SaveChangesAsync();

            var perm = new Mock<IPermissionService>();
            perm.Setup(p => p.UserHasPermissionAsync(12, "Utilisateur.DeactivateSelf")).ReturnsAsync(true);

            var svc = BuildService(ctx);
            var controller = BuildController(svc, ctx, perm.Object, new Mock<IRefreshTokenService>().Object, 12);

            var action = await controller.ToggleStatut(12);
            Assert.IsType<BadRequestObjectResult>(action.Result);
        }

        [Fact]
        public async Task ToggleStatut_other_user_without_Update_returns_403_with_not_self_code()
        {
            await using var ctx = BuildDb(nameof(ToggleStatut_other_user_without_Update_returns_403_with_not_self_code));
            ctx.Utilisateurs.AddRange(
                new Utilisateur { IdUtilisateur = 20, NomComplet = "Client", Email = "c@test.com", MotDePasseHash = "h", Statut = true, IdSociete = 1 },
                new Utilisateur { IdUtilisateur = 21, NomComplet = "Other", Email = "o@test.com", MotDePasseHash = "h", Statut = true, IdSociete = 1 }
            );
            await ctx.SaveChangesAsync();

            var perm = new Mock<IPermissionService>();
            perm.Setup(p => p.UserHasPermissionAsync(20, "Utilisateur.Update")).ReturnsAsync(false);

            var svc = BuildService(ctx);
            var controller = BuildController(svc, ctx, perm.Object, new Mock<IRefreshTokenService>().Object, 20);

            var action = await controller.ToggleStatut(21);
            var result = Assert.IsType<ObjectResult>(action.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);

            var body = result.Value!;
            var codeProp = body.GetType().GetProperty("code");
            Assert.NotNull(codeProp);
            Assert.Equal("TOGGLE_STATUT_NOT_SELF", codeProp!.GetValue(body)?.ToString());
        }

        [Fact]
        public async Task ToggleStatut_admin_same_societe_toggles_other_user()
        {
            await using var ctx = BuildDb(nameof(ToggleStatut_admin_same_societe_toggles_other_user));
            var (_, adminRole) = await SeedRolesAsync(ctx);
            ctx.Utilisateurs.AddRange(
                new Utilisateur
                {
                    IdUtilisateur = 30,
                    NomComplet = "Admin",
                    Email = "admin@test.com",
                    MotDePasseHash = "h",
                    Statut = true,
                    IdRole = adminRole.IdRole,
                    IdSociete = 1
                },
                new Utilisateur
                {
                    IdUtilisateur = 31,
                    NomComplet = "Target",
                    Email = "target@test.com",
                    MotDePasseHash = "h",
                    Statut = true,
                    IdSociete = 1
                }
            );
            await ctx.SaveChangesAsync();

            var perm = new Mock<IPermissionService>();
            perm.Setup(p => p.UserHasPermissionAsync(30, "Utilisateur.Update")).ReturnsAsync(true);

            var svc = BuildService(ctx);
            var controller = BuildController(svc, ctx, perm.Object, new Mock<IRefreshTokenService>().Object, 30);

            var action = await controller.ToggleStatut(31);
            Assert.IsType<OkObjectResult>(action.Result);

            var target = await ctx.Utilisateurs.FindAsync(31);
            Assert.False(target!.Statut);
        }

        [Fact]
        public async Task ToggleStatut_admin_other_societe_returns_forbid()
        {
            await using var ctx = BuildDb(nameof(ToggleStatut_admin_other_societe_returns_forbid));
            var (_, adminRole) = await SeedRolesAsync(ctx);
            ctx.Utilisateurs.AddRange(
                new Utilisateur
                {
                    IdUtilisateur = 40,
                    NomComplet = "Admin S1",
                    Email = "a1@test.com",
                    MotDePasseHash = "h",
                    Statut = true,
                    IdRole = adminRole.IdRole,
                    IdSociete = 1
                },
                new Utilisateur
                {
                    IdUtilisateur = 41,
                    NomComplet = "User S2",
                    Email = "u2@test.com",
                    MotDePasseHash = "h",
                    Statut = true,
                    IdSociete = 2
                }
            );
            await ctx.SaveChangesAsync();

            var perm = new Mock<IPermissionService>();
            perm.Setup(p => p.UserHasPermissionAsync(40, "Utilisateur.Update")).ReturnsAsync(true);

            var svc = BuildService(ctx);
            var controller = BuildController(svc, ctx, perm.Object, new Mock<IRefreshTokenService>().Object, 40);

            var action = await controller.ToggleStatut(41);
            Assert.IsType<ForbidResult>(action.Result);
        }
    }
}
