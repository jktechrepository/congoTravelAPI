using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using System.Reflection;
using Xunit;

namespace CongoTravel.Tests
{
    public class SocieteBootstrapTests
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

        private static CreateSocieteWithBootstrapDto SampleDto(string suffix)
        {
            return new CreateSocieteWithBootstrapDto
            {
                Societe = new CreateSocieteBootstrapSocieteDto
                {
                    Nom = $"Soc_{suffix}",
                    EmailContact = $"admin_{suffix}@test.local",
                    Telephone = "+243111",
                    NomCompletResponsable = "Resp",
                    GenreResponsable = "Masculin"
                },
                Site = new CreateSocieteBootstrapSiteDto
                {
                    CodeSite = "MAIN",
                    NomSite = "Siège",
                    NomResponsableSite = "Responsable siège",
                    Genre = "Masculin",
                    Statut = true,
                    Email = $"gerant_{suffix}@test.local",
                    Telephone = "+243222"
                }
            };
        }

        [Fact]
        public async Task CreateWithBootstrapAsync_creates_societe_site_gerant_and_admin_links()
        {
            var db = nameof(CreateWithBootstrapAsync_creates_societe_site_gerant_and_admin_links);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var svc = new SocieteService(ctx, EmailMock().Object, NullLogger<SocieteService>.Instance);

            var dto = SampleDto("a");
            var result = await svc.CreateWithBootstrapAsync(dto);

            Assert.True(result.Societe.IdSociete > 0);
            Assert.Equal(dto.Societe.Nom, result.Societe.Nom);
            Assert.Equal("MAIN", result.Site.CodeSite);
            Assert.Equal(result.Societe.IdSociete, result.Site.IdSociete);
            Assert.True(result.Site.IsSitePrincipal);

            var config = await ctx.ConfigSocietes.FirstOrDefaultAsync(c => c.IdSociete == result.Societe.IdSociete);
            Assert.NotNull(config);
            Assert.Equal(ConfigSocieteDefaults.HeuresLimiteReaffectation, config!.HeuresLimiteReaffectation);

            Assert.Equal(dto.Site.NomResponsableSite, result.GerantAgent.NomComplet);
            Assert.Equal(dto.Site.Email!.Trim(), result.GerantAgent.EmailAgent);
            Assert.Equal(dto.Site.Email!.Trim(), result.GerantUtilisateur.Email);
            Assert.True(result.GerantWelcomeEmailQueued);

            Assert.Equal(result.Site.IdSite, result.GerantAgent.IdSite);
            Assert.Equal("Gerant", result.GerantAgent.RoleAgent);
            Assert.Equal(result.Site.IdSite, result.GerantUtilisateur.IdSite);
            Assert.Equal(result.GerantAgent.IdAgent, result.GerantUtilisateur.IdAgent);

            var adminRole = await ctx.Roles.FirstAsync(r => r.Nom == "Admin");
            var gerantRole = await ctx.Roles.FirstAsync(r => r.Nom == "Gerant");
            Assert.NotNull(result.AdminUtilisateur);
            Assert.Equal(adminRole.IdRole, result.AdminUtilisateur!.IdRole);
            Assert.Equal(result.Site.IdSite, result.AdminUtilisateur.IdSite);
            var adminAgent = await ctx.Agents.FirstAsync(a => a.IdAgent == result.AdminUtilisateur.IdAgent);
            Assert.Equal(result.Site.IdSite, adminAgent.IdSite);
            Assert.Equal(gerantRole.IdRole, result.GerantUtilisateur.IdRole);

            var ur = await ctx.UserRoles.FirstAsync(x => x.IdUtilisateur == result.GerantUtilisateur.IdUtilisateur);
            Assert.Equal(gerantRole.IdRole, ur.IdRole);

            var typeTerrestre = await ctx.TypeVehicules
                .SingleAsync(t => t.IdSociete == result.Societe.IdSociete && t.Libelle == "Terrestre");
            Assert.True(typeTerrestre.Statut);
        }

        [Fact]
        public async Task CreateWithBootstrapAsync_seeds_default_type_vehicule_terrestre_idempotent()
        {
            var db = nameof(CreateWithBootstrapAsync_seeds_default_type_vehicule_terrestre_idempotent);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var svc = new SocieteService(ctx, EmailMock().Object, NullLogger<SocieteService>.Instance);

            var result = await svc.CreateWithBootstrapAsync(SampleDto("type"));

            var seedMethod = typeof(SocieteService).GetMethod(
                "SeedDefaultTypeVehiculeForSocieteAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(seedMethod);
            await (Task)seedMethod!.Invoke(svc, new object[] { result.Societe.IdSociete })!;

            var count = await ctx.TypeVehicules.CountAsync(t =>
                t.IdSociete == result.Societe.IdSociete && t.Libelle == "Terrestre");
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CreateWithBootstrapAsync_throws_when_site_contact_equals_societe_contact()
        {
            var db = nameof(CreateWithBootstrapAsync_throws_when_site_contact_equals_societe_contact);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var svc = new SocieteService(ctx, EmailMock().Object, NullLogger<SocieteService>.Instance);

            var dto = SampleDto("b");
            dto.Site.Email = dto.Societe.EmailContact!;

            var ex = await Assert.ThrowsAsync<SocieteBootstrapConflictException>(() => svc.CreateWithBootstrapAsync(dto));
            Assert.Equal(SocieteBootstrapConflictReason.GerantEmailSameAsSocieteContact, ex.Reason);
        }

        [Fact]
        public async Task CreateWithBootstrapAsync_throws_when_site_contact_already_used_by_utilisateur()
        {
            var db = nameof(CreateWithBootstrapAsync_throws_when_site_contact_already_used_by_utilisateur);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var svc = new SocieteService(ctx, EmailMock().Object, NullLogger<SocieteService>.Instance);

            var taken = "taken@test.local";
            ctx.Utilisateurs.Add(new Utilisateur
            {
                NomComplet = "X",
                Email = taken,
                MotDePasseHash = "x",
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var dto = SampleDto("c");
            dto.Site.Email = taken;

            var ex = await Assert.ThrowsAsync<SocieteBootstrapConflictException>(() => svc.CreateWithBootstrapAsync(dto));
            Assert.Equal(SocieteBootstrapConflictReason.GerantEmailAlreadyExists, ex.Reason);
        }

        [Fact]
        public async Task CreateWithBootstrapAsync_throws_when_site_contact_already_used_by_agent()
        {
            var db = nameof(CreateWithBootstrapAsync_throws_when_site_contact_already_used_by_agent);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var svc = new SocieteService(ctx, EmailMock().Object, NullLogger<SocieteService>.Instance);

            var other = new Societe
            {
                Nom = "ExistingSoc",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Societes.Add(other);
            await ctx.SaveChangesAsync();

            var dupEmail = "dup_on_agent@test.local";
            ctx.Agents.Add(new Agent
            {
                IdSociete = other.IdSociete,
                NomComplet = "Existing agent",
                EmailAgent = dupEmail,
                DateNaissance = DateTime.UtcNow.AddYears(-30),
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var dto = SampleDto("d");
            dto.Site.Email = dupEmail;

            var ex = await Assert.ThrowsAsync<SocieteBootstrapConflictException>(() => svc.CreateWithBootstrapAsync(dto));
            Assert.Equal(SocieteBootstrapConflictReason.AgentGerantEmailAlreadyExists, ex.Reason);
        }

        [Fact]
        public async Task CreateWithBootstrapAsync_uses_phone_when_site_email_missing()
        {
            var db = nameof(CreateWithBootstrapAsync_uses_phone_when_site_email_missing);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var svc = new SocieteService(ctx, EmailMock().Object, NullLogger<SocieteService>.Instance);

            var dto = SampleDto("e");
            dto.Site.Email = null;
            dto.Site.Telephone = "+243999888";

            var result = await svc.CreateWithBootstrapAsync(dto);

            Assert.Equal("+243999888", result.GerantUtilisateur.Email);
            Assert.Equal("+243999888", result.GerantAgent.EmailAgent);
            Assert.False(result.GerantWelcomeEmailQueued);
        }

        [Fact]
        public async Task CreateWithBootstrapAsync_allows_multiple_societes_without_email_contact()
        {
            var db = nameof(CreateWithBootstrapAsync_allows_multiple_societes_without_email_contact);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var svc = new SocieteService(ctx, EmailMock().Object, NullLogger<SocieteService>.Instance);

            var dto1 = SampleDto("no_email_1");
            dto1.Societe.EmailContact = "";
            dto1.Site.Email = null;
            dto1.Site.Telephone = "+243700001";

            var dto2 = SampleDto("no_email_2");
            dto2.Societe.EmailContact = null;
            dto2.Site.Email = "   ";
            dto2.Site.Telephone = "+243700002";

            var result1 = await svc.CreateWithBootstrapAsync(dto1);
            var result2 = await svc.CreateWithBootstrapAsync(dto2);

            Assert.Null(result1.AdminUtilisateur!.Email);
            Assert.Null(result2.AdminUtilisateur!.Email);
            Assert.Null(result1.Societe.EmailContact);
            Assert.Null(result2.Societe.EmailContact);
            Assert.Equal("+243700001", result1.GerantUtilisateur.Email);
            Assert.Equal("+243700002", result2.GerantUtilisateur.Email);
        }

        [Fact]
        public async Task CreateWithBootstrapAsync_throws_when_site_email_and_phone_missing()
        {
            var db = nameof(CreateWithBootstrapAsync_throws_when_site_email_and_phone_missing);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var svc = new SocieteService(ctx, EmailMock().Object, NullLogger<SocieteService>.Instance);

            var dto = SampleDto("f");
            dto.Site.Email = null;
            dto.Site.Telephone = null;

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateWithBootstrapAsync(dto));
            Assert.Contains("Site.Email", ex.Message, StringComparison.Ordinal);
            Assert.Contains("Site.Telephone", ex.Message, StringComparison.Ordinal);
        }
    }
}
