using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Site;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteBootstrapTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(db)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

        private static Mock<IEmailService> EmailMock()
        {
            var m = new Mock<IEmailService>();
            m.Setup(e => e.SendWelcomeEmailAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            return m;
        }

        private static SiteCreateDto SampleDto(string suffix, int idSociete)
        {
            return new SiteCreateDto
            {
                IdSociete = idSociete,
                CodeSite = $"SITE_{suffix}",
                NomSite = "Site test",
                NomResponsableSite = "Responsable lieu",
                Genre = "Masculin",
                Email = $"gerant_site_{suffix}@test.local",
                Telephone = "+243333",
                Statut = true
            };
        }

        [Fact]
        public async Task CreateWithGerantAsync_creates_site_agent_user_and_user_role()
        {
            var db = nameof(CreateWithGerantAsync_creates_site_agent_user_and_user_role);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var societe = new Societe
            {
                Nom = "Soc",
                EmailContact = "admin@test.local",
                DateCreation = DateTime.UtcNow
            };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var svc = new SiteService(ctx, EmailMock().Object, NullLogger<SiteService>.Instance);
            var dto = SampleDto("a", societe.IdSociete);
            var result = await svc.CreateWithGerantAsync(dto);

            Assert.True(result.Site.IdSite > 0);
            Assert.Equal(dto.CodeSite, result.Site.CodeSite);
            Assert.Equal(societe.IdSociete, result.Site.IdSociete);

            Assert.Equal(result.Site.IdSite, result.GerantAgent.IdSite);
            Assert.Equal("Gerant", result.GerantAgent.RoleAgent);
            Assert.Equal(result.Site.IdSite, result.GerantUtilisateur.IdSite);
            Assert.Equal(result.GerantAgent.IdAgent, result.GerantUtilisateur.IdAgent);

            var gerantRole = await ctx.Roles.FirstAsync(r => r.Nom == "Gerant");
            Assert.Equal(gerantRole.IdRole, result.GerantUtilisateur.IdRole);

            var ur = await ctx.UserRoles.FirstAsync(x => x.IdUtilisateur == result.GerantUtilisateur.IdUtilisateur);
            Assert.Equal(gerantRole.IdRole, ur.IdRole);
        }

        [Fact]
        public async Task CreateWithGerantAsync_throws_when_gerant_email_equals_societe_contact()
        {
            var db = nameof(CreateWithGerantAsync_throws_when_gerant_email_equals_societe_contact);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var societe = new Societe
            {
                Nom = "Soc",
                EmailContact = "same@test.local",
                DateCreation = DateTime.UtcNow
            };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var svc = new SiteService(ctx, EmailMock().Object, NullLogger<SiteService>.Instance);
            var dto = SampleDto("b", societe.IdSociete);
            dto.Email = societe.EmailContact!;

            var ex = await Assert.ThrowsAsync<SiteBootstrapConflictException>(() => svc.CreateWithGerantAsync(dto));
            Assert.Equal(SiteBootstrapConflictReason.GerantEmailSameAsSocieteContact, ex.Reason);
        }

        [Fact]
        public async Task CreateWithGerantAsync_throws_when_gerant_email_already_used()
        {
            var db = nameof(CreateWithGerantAsync_throws_when_gerant_email_already_used);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var societe = new Societe { Nom = "Soc", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var taken = "taken_site@test.local";
            ctx.Utilisateurs.Add(new Utilisateur
            {
                NomComplet = "X",
                Email = taken,
                MotDePasseHash = "x",
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var svc = new SiteService(ctx, EmailMock().Object, NullLogger<SiteService>.Instance);
            var dto = SampleDto("c", societe.IdSociete);
            dto.Email = taken;

            var ex = await Assert.ThrowsAsync<SiteBootstrapConflictException>(() => svc.CreateWithGerantAsync(dto));
            Assert.Equal(SiteBootstrapConflictReason.GerantEmailAlreadyExists, ex.Reason);
        }

        [Fact]
        public async Task CreateWithGerantAsync_uses_telephone_when_email_is_missing()
        {
            var db = nameof(CreateWithGerantAsync_uses_telephone_when_email_is_missing);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var societe = new Societe { Nom = "Soc", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var svc = new SiteService(ctx, EmailMock().Object, NullLogger<SiteService>.Instance);
            var dto = SampleDto("telonly", societe.IdSociete);
            dto.Email = null;
            dto.Telephone = "+243999000111";

            var result = await svc.CreateWithGerantAsync(dto);

            Assert.Equal("+243999000111", result.GerantAgent.EmailAgent);
            Assert.Equal("+243999000111", result.GerantUtilisateur.Email);
        }

        [Fact]
        public async Task CreateWithGerantAsync_throws_when_email_and_telephone_are_missing()
        {
            var db = nameof(CreateWithGerantAsync_throws_when_email_and_telephone_are_missing);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var societe = new Societe { Nom = "Soc", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var svc = new SiteService(ctx, EmailMock().Object, NullLogger<SiteService>.Instance);
            var dto = SampleDto("nocontact", societe.IdSociete);
            dto.Email = null;
            dto.Telephone = null;

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateWithGerantAsync(dto));
            Assert.Contains("Email ou Telephone", ex.Message);
        }

        [Fact]
        public async Task CreateWithGerantAsync_throws_when_site_code_already_exists()
        {
            var db = nameof(CreateWithGerantAsync_throws_when_site_code_already_exists);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var societe = new Societe { Nom = "Soc", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            ctx.Sites.Add(new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "DUP",
                NomSite = "Existing",
                NomResponsableSite = "R",
                Genre = "Masculin",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var svc = new SiteService(ctx, EmailMock().Object, NullLogger<SiteService>.Instance);
            var dto = SampleDto("d", societe.IdSociete);
            dto.CodeSite = "DUP";

            var ex = await Assert.ThrowsAsync<SiteBootstrapConflictException>(() => svc.CreateWithGerantAsync(dto));
            Assert.Equal(SiteBootstrapConflictReason.SiteCodeAlreadyExists, ex.Reason);
        }

    }
}
