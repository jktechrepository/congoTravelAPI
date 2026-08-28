using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
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
    public class AuthentificationResponseActivitesTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static AuthentificationResponseBuilder CreateBuilder(CongoTravelDbContext ctx)
        {
            var jwt = new Mock<ISimpleJwtService>();
            jwt.Setup(j => j.GenerateToken(It.IsAny<Utilisateur>(), It.IsAny<int?>()))
                .Returns("token");

            var refresh = new Mock<IRefreshTokenService>();
            refresh.Setup(r => r.GenerateRefreshTokenAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync("refresh");

            var permissions = new Mock<IPermissionService>();
            permissions.Setup(p => p.GetUserPermissionsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<string>());
            permissions.Setup(p => p.GetUserRolesAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Role>());
            permissions.Setup(p => p.GetUserPrimaryRoleAsync(It.IsAny<int>()))
                .ReturnsAsync((Role?)null);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:ExpirationMinutes"] = "60" })
                .Build();

            return new AuthentificationResponseBuilder(
                ctx,
                jwt.Object,
                refresh.Object,
                permissions.Object,
                new ConfigSocieteService(ctx),
                config,
                NullLogger<AuthentificationResponseBuilder>.Instance);
        }

        [Fact]
        public async Task BuildAsync_returns_Transport_and_Restaurant_only_when_others_disabled()
        {
            await using var ctx = BuildDb(nameof(BuildAsync_returns_Transport_and_Restaurant_only_when_others_disabled));

            var societe = new Societe { Nom = "Soc Activites", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var configSvc = new ConfigSocieteService(ctx);
            var config = await configSvc.GetOrCreateAsync(societe.IdSociete);
            config.ActiviteTransport = true;
            config.ActiviteEvenement = false;
            config.ActiviteSiteTouristique = false;
            config.ActiviteRestaurant = true;
            config.ActiviteHotel = false;
            await ctx.SaveChangesAsync();

            var user = new Utilisateur
            {
                NomComplet = "Staff",
                MotDePasseHash = "x",
                Statut = true,
                IdSociete = societe.IdSociete,
                DateCreation = DateTime.UtcNow
            };
            ctx.Utilisateurs.Add(user);
            await ctx.SaveChangesAsync();

            var response = await CreateBuilder(ctx).BuildAsync(user);

            Assert.Equal(new[] { "Transport", "Restaurant" }, response.ActivitesSociete);
        }

        [Fact]
        public async Task BuildAsync_returns_empty_activites_when_user_has_no_societe()
        {
            await using var ctx = BuildDb(nameof(BuildAsync_returns_empty_activites_when_user_has_no_societe));

            var user = new Utilisateur
            {
                NomComplet = "Client",
                MotDePasseHash = "x",
                Statut = true,
                IdSociete = null,
                DateCreation = DateTime.UtcNow
            };
            ctx.Utilisateurs.Add(user);
            await ctx.SaveChangesAsync();

            var response = await CreateBuilder(ctx).BuildAsync(user);

            Assert.Empty(response.ActivitesSociete);
        }
    }
}
