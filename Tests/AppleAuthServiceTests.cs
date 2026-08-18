using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Authentification;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class AppleAuthServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static async Task SeedAsync(CongoTravelDbContext ctx)
        {
            ctx.Societes.Add(new Societe { Nom = "TestSoc", Statut = true, DateCreation = DateTime.UtcNow });
            ctx.Roles.Add(new Role { Nom = "Client", Niveau = 5, Statut = true });
            await ctx.SaveChangesAsync();
        }

        private static AppleAuthService BuildService(CongoTravelDbContext ctx, IAppleTokenValidator validator)
        {
            var jwt = new Mock<ISimpleJwtService>();
            jwt.Setup(j => j.GenerateToken(It.IsAny<Utilisateur>(), It.IsAny<int?>()))
                .Returns("apple-access-token");

            var refresh = new Mock<IRefreshTokenService>();
            refresh.Setup(r => r.GenerateRefreshTokenAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync("apple-refresh-token");

            var permissions = new Mock<IPermissionService>();
            permissions.Setup(p => p.GetUserPermissionsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<string>());
            permissions.Setup(p => p.GetUserRolesAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Role>());
            permissions.Setup(p => p.GetUserPrimaryRoleAsync(It.IsAny<int>()))
                .ReturnsAsync((Role?)null);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:ExpirationMinutes"] = "120" })
                .Build();

            var responseBuilder = new AuthentificationResponseBuilder(
                ctx, jwt.Object, refresh.Object, permissions.Object,
                new ConfigSocieteService(ctx),
                config,
                NullLogger<AuthentificationResponseBuilder>.Instance);

            var accounts = new ExternalAuthAccountService(ctx, NullLogger<ExternalAuthAccountService>.Instance);

            return new AppleAuthService(validator, accounts, responseBuilder);
        }

        private static Mock<IAppleTokenValidator> MockId(ExternalAuthIdentity identity)
        {
            var m = new Mock<IAppleTokenValidator>();
            m.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(identity);
            return m;
        }

        [Fact]
        public async Task SignIn_creates_user_without_telephone()
        {
            await using var ctx = BuildDb(nameof(SignIn_creates_user_without_telephone));
            await SeedAsync(ctx);

            var service = BuildService(ctx, MockId(new ExternalAuthIdentity
            {
                Sub = "apple-sub-1",
                Email = "apple.user@privaterelay.appleid.com",
                EmailVerified = true,
                Name = "Apple User"
            }).Object);

            var response = await service.SignInWithAppleAsync("tok");

            Assert.True(response.Success);
            Assert.Equal("apple-access-token", response.AccessToken);
            Assert.False(response.DoitChangerMotDePasse);
            Assert.Equal(AuthProviders.Apple, response.Utilisateur!.AuthProvider);
            Assert.Equal("apple-sub-1", response.Utilisateur.ExternalSubjectId);
            Assert.Null(response.Utilisateur.Telephone);
            Assert.Equal(1, await ctx.Clients.CountAsync());
        }

        [Fact]
        public async Task SignIn_same_sub_reuses_account()
        {
            await using var ctx = BuildDb(nameof(SignIn_same_sub_reuses_account));
            await SeedAsync(ctx);

            var identity = new ExternalAuthIdentity
            {
                Sub = "apple-sub-2",
                Email = "once@icloud.com",
                EmailVerified = true,
                Name = "Once"
            };
            var service = BuildService(ctx, MockId(identity).Object);

            var a = await service.SignInWithAppleAsync("t1");
            // 2e appel sans email (comportement Apple)
            identity.Email = null;
            var b = await service.SignInWithAppleAsync("t2");

            Assert.Equal(a.Utilisateur!.IdUtilisateur, b.Utilisateur!.IdUtilisateur);
            Assert.Equal(1, await ctx.Clients.CountAsync());
        }

        [Fact]
        public async Task SignIn_links_existing_email()
        {
            await using var ctx = BuildDb(nameof(SignIn_links_existing_email));
            await SeedAsync(ctx);
            var societeId = ctx.Societes.First().IdSociete;

            var client = new Client
            {
                NomClient = "Local",
                EmailClient = "local@icloud.com",
                Statut = true,
                IsActif = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Clients.Add(client);
            await ctx.SaveChangesAsync();
            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdClient = client.IdClient,
                NomComplet = "Local",
                Email = "local@icloud.com",
                MotDePasseHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                DoitChangerMotDePasse = true,
                Statut = true,
                IdSociete = societeId,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var service = BuildService(ctx, MockId(new ExternalAuthIdentity
            {
                Sub = "apple-link",
                Email = "local@icloud.com",
                EmailVerified = true
            }).Object);

            var response = await service.SignInWithAppleAsync("tok");

            Assert.Equal(AuthProviders.Apple, response.Utilisateur!.AuthProvider);
            Assert.Equal("apple-link", response.Utilisateur.ExternalSubjectId);
            Assert.Equal(1, await ctx.Clients.CountAsync());
        }

        [Fact]
        public async Task SignIn_invalid_token_throws_401()
        {
            await using var ctx = BuildDb(nameof(SignIn_invalid_token_throws_401));
            var validator = new Mock<IAppleTokenValidator>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ExternalAuthException(401, "ID token Apple invalide ou expiré."));

            var service = BuildService(ctx, validator.Object);
            var ex = await Assert.ThrowsAsync<ExternalAuthException>(() => service.SignInWithAppleAsync("bad"));
            Assert.Equal(401, ex.StatusCode);
        }

        [Fact]
        public async Task SignIn_missing_email_on_first_login_throws_400()
        {
            await using var ctx = BuildDb(nameof(SignIn_missing_email_on_first_login_throws_400));
            await SeedAsync(ctx);

            var service = BuildService(ctx, MockId(new ExternalAuthIdentity
            {
                Sub = "apple-no-email",
                Email = null,
                EmailVerified = false
            }).Object);

            var ex = await Assert.ThrowsAsync<ExternalAuthException>(() => service.SignInWithAppleAsync("tok"));
            Assert.Equal(400, ex.StatusCode);
        }
    }
}
