using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class InfoPaiementResolutionServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static async Task<(int IdSociete, int IdSitePrincipal, int IdSiteSatellite)> SeedPrincipalWithInfoPaiementAsync(
            CongoTravelDbContext ctx,
            bool addSatellite = true)
        {
            var societe = new Societe { Nom = "Soc", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var principal = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "PRIN",
                NomSite = "Principal",
                Statut = true,
                IsSitePrincipal = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(principal);
            await ctx.SaveChangesAsync();

            ctx.InfoPaiementsSociete.Add(new InfoPaiementSociete
            {
                IdSociete = societe.IdSociete,
                IdSite = principal.IdSite,
                CodeMarchand = "MERCH-PRIN",
                ApiToken = "token-principal",
                Statut = true,
                ActifMobileMoney = true,
                ActifCarteBancaire = true,
                DateCreation = DateTime.UtcNow
            });

            int satelliteId = 0;
            if (addSatellite)
            {
                var satellite = new Site
                {
                    IdSociete = societe.IdSociete,
                    CodeSite = "SAT",
                    NomSite = "Satellite",
                    Statut = true,
                    IsSitePrincipal = false,
                    DateCreation = DateTime.UtcNow
                };
                ctx.Sites.Add(satellite);
                await ctx.SaveChangesAsync();
                satelliteId = satellite.IdSite;
            }

            await ctx.SaveChangesAsync();
            return (societe.IdSociete, principal.IdSite, satelliteId);
        }

        [Fact]
        public async Task ResolveActiveForSiteAsync_returns_direct_config_when_site_has_active_InfoPaiement()
        {
            await using var ctx = BuildDb(nameof(ResolveActiveForSiteAsync_returns_direct_config_when_site_has_active_InfoPaiement));
            var (idSociete, idPrincipal, _) = await SeedPrincipalWithInfoPaiementAsync(ctx, addSatellite: false);

            var svc = new InfoPaiementResolutionService(ctx, NullLogger<InfoPaiementResolutionService>.Instance);
            var result = await svc.ResolveActiveForSiteAsync(idPrincipal, idSociete);

            Assert.Equal("MERCH-PRIN", result.CodeMarchand);
            Assert.Equal(idPrincipal, result.IdSite);
        }

        [Fact]
        public async Task ResolveActiveForSiteAsync_falls_back_to_principal_InfoPaiement_for_satellite()
        {
            await using var ctx = BuildDb(nameof(ResolveActiveForSiteAsync_falls_back_to_principal_InfoPaiement_for_satellite));
            var (idSociete, idPrincipal, idSatellite) = await SeedPrincipalWithInfoPaiementAsync(ctx);

            var svc = new InfoPaiementResolutionService(ctx, NullLogger<InfoPaiementResolutionService>.Instance);
            var result = await svc.ResolveActiveForSiteAsync(idSatellite, idSociete);

            Assert.Equal("MERCH-PRIN", result.CodeMarchand);
            Assert.Equal(idPrincipal, result.IdSite);
        }

        [Fact]
        public async Task ResolveActiveForSiteAsync_logs_fallback_message_for_satellite()
        {
            await using var ctx = BuildDb(nameof(ResolveActiveForSiteAsync_logs_fallback_message_for_satellite));
            var (idSociete, idPrincipal, idSatellite) = await SeedPrincipalWithInfoPaiementAsync(ctx);

            var logger = new Mock<ILogger<InfoPaiementResolutionService>>();
            var svc = new InfoPaiementResolutionService(ctx, logger.Object);

            await svc.ResolveActiveForSiteAsync(idSatellite, idSociete);

            logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) =>
                        v.ToString()!.Contains("FlexPay InfoPaiement fallback", StringComparison.Ordinal)
                        && v.ToString()!.Contains(idSatellite.ToString(), StringComparison.Ordinal)
                        && v.ToString()!.Contains(idPrincipal.ToString(), StringComparison.Ordinal)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ResolveActiveForSiteAsync_falls_back_to_societe_config_when_principal_lacks_own_InfoPaiement()
        {
            await using var ctx = BuildDb(nameof(ResolveActiveForSiteAsync_falls_back_to_societe_config_when_principal_lacks_own_InfoPaiement));
            var societe = new Societe { Nom = "Soc60", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var legacy = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "LEG",
                NomSite = "Ancien principal",
                Statut = true,
                IsSitePrincipal = false,
                DateCreation = DateTime.UtcNow
            };
            var newPrincipal = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "NEW",
                NomSite = "Nouveau site 71",
                Statut = true,
                IsSitePrincipal = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.AddRange(legacy, newPrincipal);
            await ctx.SaveChangesAsync();

            ctx.InfoPaiementsSociete.Add(new InfoPaiementSociete
            {
                IdSociete = societe.IdSociete,
                IdSite = legacy.IdSite,
                CodeMarchand = "MERCH-LEGACY",
                ApiToken = "token-legacy",
                Statut = true,
                ActifMobileMoney = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var svc = new InfoPaiementResolutionService(ctx, NullLogger<InfoPaiementResolutionService>.Instance);
            var result = await svc.ResolveActiveForSiteAsync(newPrincipal.IdSite, societe.IdSociete);

            Assert.Equal("MERCH-LEGACY", result.CodeMarchand);
            Assert.Equal(legacy.IdSite, result.IdSite);
        }

        [Fact]
        public async Task ResolveActiveForSiteAsync_logs_societe_wide_fallback_when_principal_lacks_InfoPaiement()
        {
            await using var ctx = BuildDb(nameof(ResolveActiveForSiteAsync_logs_societe_wide_fallback_when_principal_lacks_InfoPaiement));
            var societe = new Societe { Nom = "Soc", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var legacy = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "LEG",
                NomSite = "Legacy",
                Statut = true,
                IsSitePrincipal = false,
                DateCreation = DateTime.UtcNow
            };
            var principal = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "PRIN",
                NomSite = "Principal sans config",
                Statut = true,
                IsSitePrincipal = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.AddRange(legacy, principal);
            await ctx.SaveChangesAsync();

            ctx.InfoPaiementsSociete.Add(new InfoPaiementSociete
            {
                IdSociete = societe.IdSociete,
                IdSite = legacy.IdSite,
                CodeMarchand = "M",
                ApiToken = "t",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var logger = new Mock<ILogger<InfoPaiementResolutionService>>();
            var svc = new InfoPaiementResolutionService(ctx, logger.Object);
            await svc.ResolveActiveForSiteAsync(principal.IdSite, societe.IdSociete);

            logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) =>
                        v.ToString()!.Contains("fallback société", StringComparison.OrdinalIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ResolveActiveForSiteAsync_throws_explicit_message_when_principal_has_no_InfoPaiement_anywhere()
        {
            await using var ctx = BuildDb(nameof(ResolveActiveForSiteAsync_throws_explicit_message_when_principal_has_no_InfoPaiement_anywhere));
            var societe = new Societe { Nom = "Soc", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var principal = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "PRIN",
                NomSite = "Principal vide",
                Statut = true,
                IsSitePrincipal = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(principal);
            await ctx.SaveChangesAsync();

            var svc = new InfoPaiementResolutionService(ctx, NullLogger<InfoPaiementResolutionService>.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.ResolveActiveForSiteAsync(principal.IdSite, societe.IdSociete));

            Assert.Contains("site principal", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("aucun autre site", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResolveActiveForSiteAsync_throws_when_no_principal_site()
        {
            await using var ctx = BuildDb(nameof(ResolveActiveForSiteAsync_throws_when_no_principal_site));
            var societe = new Societe { Nom = "Soc", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var orphan = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "ORPH",
                NomSite = "Orphelin",
                Statut = true,
                IsSitePrincipal = false,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(orphan);
            await ctx.SaveChangesAsync();

            var svc = new InfoPaiementResolutionService(ctx, NullLogger<InfoPaiementResolutionService>.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.ResolveActiveForSiteAsync(orphan.IdSite, societe.IdSociete));

            Assert.Contains("site principal", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResolveActiveForSiteAsync_throws_when_principal_has_no_active_InfoPaiement()
        {
            await using var ctx = BuildDb(nameof(ResolveActiveForSiteAsync_throws_when_principal_has_no_active_InfoPaiement));
            var (idSociete, idPrincipal, idSatellite) = await SeedPrincipalWithInfoPaiementAsync(ctx);

            var inactive = await ctx.InfoPaiementsSociete.SingleAsync(i => i.IdSite == idPrincipal);
            inactive.Statut = false;
            await ctx.SaveChangesAsync();

            var svc = new InfoPaiementResolutionService(ctx, NullLogger<InfoPaiementResolutionService>.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.ResolveActiveForSiteAsync(idSatellite, idSociete));

            Assert.Contains("FlexPay", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
