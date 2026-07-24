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
    public class GoogleAuthServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static async Task SeedSocieteAndClientRoleAsync(CongoTravelDbContext ctx)
        {
            ctx.Societes.Add(new Societe { Nom = "TestSoc", Statut = true, DateCreation = DateTime.UtcNow });
            ctx.Roles.Add(new Role { Nom = "Client", Niveau = 5, Statut = true });
            await ctx.SaveChangesAsync();
        }

        private static GoogleAuthService BuildService(
            CongoTravelDbContext ctx,
            IGoogleTokenValidator validator)
        {
            var jwt = new Mock<ISimpleJwtService>();
            jwt.Setup(j => j.GenerateToken(It.IsAny<Utilisateur>(), It.IsAny<int?>()))
                .Returns("test-access-token");

            var refresh = new Mock<IRefreshTokenService>();
            refresh.Setup(r => r.GenerateRefreshTokenAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync("test-refresh-token");

            var permissions = new Mock<IPermissionService>();
            permissions.Setup(p => p.GetUserPermissionsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "Client.Read" });
            permissions.Setup(p => p.GetUserRolesAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Role>());
            permissions.Setup(p => p.GetUserPrimaryRoleAsync(It.IsAny<int>()))
                .ReturnsAsync((Role?)null);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:ExpirationMinutes"] = "120"
                })
                .Build();

            var builder = new AuthentificationResponseBuilder(
                ctx,
                jwt.Object,
                refresh.Object,
                permissions.Object,
                config,
                NullLogger<AuthentificationResponseBuilder>.Instance);

            var accounts = new ExternalAuthAccountService(ctx, NullLogger<ExternalAuthAccountService>.Instance);

            return new GoogleAuthService(
                validator,
                accounts,
                builder);
        }

        private static Mock<IGoogleTokenValidator> MockIdentity(GoogleIdentity identity)
        {
            var mock = new Mock<IGoogleTokenValidator>();
            mock.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(identity);
            return mock;
        }

        [Fact]
        public async Task SignIn_creates_client_and_user_without_telephone()
        {
            await using var ctx = BuildDb(nameof(SignIn_creates_client_and_user_without_telephone));
            await SeedSocieteAndClientRoleAsync(ctx);

            var identity = new GoogleIdentity
            {
                Sub = "google-sub-1",
                Email = "newuser@gmail.com",
                EmailVerified = true,
                Name = "New User"
            };
            var service = BuildService(ctx, MockIdentity(identity).Object);

            var response = await service.SignInWithGoogleAsync("fake-token");

            Assert.True(response.Success);
            Assert.Equal("test-access-token", response.AccessToken);
            Assert.Equal("test-refresh-token", response.RefreshToken);
            Assert.False(response.DoitChangerMotDePasse);
            Assert.NotNull(response.Utilisateur);
            Assert.Equal(AuthProviders.Google, response.Utilisateur!.AuthProvider);
            Assert.Equal("google-sub-1", response.Utilisateur.ExternalSubjectId);
            Assert.Null(response.Utilisateur.Telephone);
            Assert.NotNull(response.Client);
            Assert.Null(response.Client!.Telephone);

            Assert.Equal(1, await ctx.Clients.CountAsync());
            Assert.Equal(1, await ctx.Utilisateurs.CountAsync(u => u.AuthProvider == AuthProviders.Google));
        }

        [Fact]
        public async Task SignIn_same_sub_reuses_user_no_second_client()
        {
            await using var ctx = BuildDb(nameof(SignIn_same_sub_reuses_user_no_second_client));
            await SeedSocieteAndClientRoleAsync(ctx);

            var identity = new GoogleIdentity
            {
                Sub = "google-sub-2",
                Email = "repeat@gmail.com",
                EmailVerified = true,
                Name = "Repeat User"
            };
            var service = BuildService(ctx, MockIdentity(identity).Object);

            var first = await service.SignInWithGoogleAsync("tok1");
            var second = await service.SignInWithGoogleAsync("tok2");

            Assert.Equal(first.Utilisateur!.IdUtilisateur, second.Utilisateur!.IdUtilisateur);
            Assert.Equal(1, await ctx.Clients.CountAsync());
        }

        [Fact]
        public async Task SignIn_links_existing_local_user_by_email()
        {
            await using var ctx = BuildDb(nameof(SignIn_links_existing_local_user_by_email));
            await SeedSocieteAndClientRoleAsync(ctx);
            var societeId = ctx.Societes.First().IdSociete;

            var client = new Client
            {
                NomClient = "Existing",
                EmailClient = "existing@gmail.com",
                Telephone = "243900000000",
                Statut = true,
                IsActif = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Clients.Add(client);
            await ctx.SaveChangesAsync();

            var user = new Utilisateur
            {
                IdClient = client.IdClient,
                NomComplet = "Existing",
                Email = "existing@gmail.com",
                MotDePasseHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                DoitChangerMotDePasse = true,
                Statut = true,
                IdSociete = societeId,
                DateCreation = DateTime.UtcNow
            };
            ctx.Utilisateurs.Add(user);
            await ctx.SaveChangesAsync();

            var identity = new GoogleIdentity
            {
                Sub = "google-sub-link",
                Email = "existing@gmail.com",
                EmailVerified = true,
                Name = "Existing"
            };
            var service = BuildService(ctx, MockIdentity(identity).Object);

            var response = await service.SignInWithGoogleAsync("tok");

            Assert.Equal(user.IdUtilisateur, response.Utilisateur!.IdUtilisateur);
            Assert.Equal(AuthProviders.Google, response.Utilisateur.AuthProvider);
            Assert.Equal("google-sub-link", response.Utilisateur.ExternalSubjectId);
            Assert.Equal(1, await ctx.Clients.CountAsync());
        }

        [Fact]
        public async Task SignIn_invalid_token_throws_401()
        {
            await using var ctx = BuildDb(nameof(SignIn_invalid_token_throws_401));
            var validator = new Mock<IGoogleTokenValidator>();
            validator.Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ExternalAuthException(401, "ID token Google invalide ou expiré."));

            var service = BuildService(ctx, validator.Object);

            var ex = await Assert.ThrowsAnyAsync<ExternalAuthException>(() =>
                service.SignInWithGoogleAsync("bad"));
            Assert.Equal(401, ex.StatusCode);
        }

        [Fact]
        public async Task SignIn_unverified_email_throws_400_on_create()
        {
            await using var ctx = BuildDb(nameof(SignIn_unverified_email_throws_400_on_create));
            await SeedSocieteAndClientRoleAsync(ctx);

            var identity = new GoogleIdentity
            {
                Sub = "google-sub-unverified",
                Email = "unverified@gmail.com",
                EmailVerified = false,
                Name = "No Verify"
            };
            var service = BuildService(ctx, MockIdentity(identity).Object);

            var ex = await Assert.ThrowsAnyAsync<ExternalAuthException>(() =>
                service.SignInWithGoogleAsync("tok"));
            Assert.Equal(400, ex.StatusCode);
        }
    }
}
