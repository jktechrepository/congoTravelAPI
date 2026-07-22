using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class AgentCreateTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static AgentService CreateService(CongoTravelDbContext ctx)
        {
            var emailMock = new Mock<IEmailService>();
            emailMock
                .Setup(e => e.SendWelcomeEmailAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            return new AgentService(
                ctx,
                Mock.Of<IUsernameGeneratorService>(),
                emailMock.Object,
                Mock.Of<IUtilisateurRepository>(),
                NullLogger<AgentService>.Instance);
        }

        private static async Task SeedSocieteAndRoleAsync(CongoTravelDbContext ctx)
        {
            if (!await ctx.Societes.AnyAsync())
            {
                ctx.Societes.Add(new Societe
                {
                    IdSociete = 1,
                    Nom = "Societe Test",
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
            }

            if (!await ctx.Roles.AnyAsync())
            {
                ctx.Roles.Add(new Role
                {
                    IdRole = 1,
                    Nom = "Caissier",
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
            }

            if (!await ctx.Sites.AnyAsync())
            {
                ctx.Sites.Add(new Site
                {
                    IdSite = 5,
                    IdSociete = 1,
                    CodeSite = "SAT",
                    NomSite = "Site satellite",
                    NomResponsableSite = "Responsable",
                    Genre = "Masculin",
                    Statut = true
                });
                ctx.Sites.Add(new Site
                {
                    IdSite = 7,
                    IdSociete = 1,
                    CodeSite = "SAT2",
                    NomSite = "Site satellite 2",
                    NomResponsableSite = "Responsable 2",
                    Genre = "Masculin",
                    Statut = true
                });
            }

            await ctx.SaveChangesAsync();
        }

        private static Agent SampleAgent(string? email = null, string? telephone = "+243900000001", int? idSite = null)
        {
            return new Agent
            {
                NomComplet = "Agent Test",
                DateNaissance = new DateTime(1990, 1, 1),
                IdSociete = 1,
                IdSite = idSite,
                TelephoneAgent = telephone,
                EmailAgent = email,
                Statut = true
            };
        }

        [Fact]
        public async Task CreateAsync_refuse_si_telephone_manquant()
        {
            await using var ctx = BuildDb(nameof(CreateAsync_refuse_si_telephone_manquant));
            await SeedSocieteAndRoleAsync(ctx);
            var svc = CreateService(ctx);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.CreateAsync(SampleAgent(telephone: null)));

            Assert.Contains("téléphone", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateAsync_refuse_si_telephone_vide()
        {
            await using var ctx = BuildDb(nameof(CreateAsync_refuse_si_telephone_vide));
            await SeedSocieteAndRoleAsync(ctx);
            var svc = CreateService(ctx);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                svc.CreateAsync(SampleAgent(telephone: "   ")));

            Assert.Contains("téléphone", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateAsync_sans_email_cree_agent_et_utilisateur()
        {
            await using var ctx = BuildDb(nameof(CreateAsync_sans_email_cree_agent_et_utilisateur));
            await SeedSocieteAndRoleAsync(ctx);
            var svc = CreateService(ctx);

            var created = await svc.CreateAsync(SampleAgent(email: null));

            Assert.True(created.IdAgent > 0);
            Assert.Null(created.EmailAgent);

            var utilisateur = await ctx.Utilisateurs.FirstOrDefaultAsync(u => u.IdAgent == created.IdAgent);
            Assert.NotNull(utilisateur);
            Assert.Null(utilisateur!.Email);
            Assert.Equal("+243900000001", utilisateur.Telephone);
        }

        [Fact]
        public async Task CreateAsync_deux_agents_sans_email_creent_deux_utilisateurs()
        {
            await using var ctx = BuildDb(nameof(CreateAsync_deux_agents_sans_email_creent_deux_utilisateurs));
            await SeedSocieteAndRoleAsync(ctx);
            var svc = CreateService(ctx);

            var first = SampleAgent(email: null, telephone: "+243900000101");
            first.NomComplet = "Agent Un";
            var second = SampleAgent(email: null, telephone: "+243900000102");
            second.NomComplet = "Agent Deux";

            await svc.CreateAsync(first);
            await svc.CreateAsync(second);

            var utilisateurs = await ctx.Utilisateurs
                .Where(u => u.IdAgent == first.IdAgent || u.IdAgent == second.IdAgent)
                .ToListAsync();

            Assert.Equal(2, utilisateurs.Count);
            Assert.All(utilisateurs, u => Assert.Null(u.Email));
        }

        [Fact]
        public async Task CreateAsync_normalise_email_vide_en_null()
        {
            await using var ctx = BuildDb(nameof(CreateAsync_normalise_email_vide_en_null));
            await SeedSocieteAndRoleAsync(ctx);
            var svc = CreateService(ctx);

            var created = await svc.CreateAsync(SampleAgent(email: "  ", telephone: "+243900000201"));

            Assert.Null(created.EmailAgent);
        }

        [Fact]
        public async Task CreateAsync_avec_idSite_propage_idSite_sur_utilisateur()
        {
            await using var ctx = BuildDb(nameof(CreateAsync_avec_idSite_propage_idSite_sur_utilisateur));
            await SeedSocieteAndRoleAsync(ctx);
            var svc = CreateService(ctx);

            var created = await svc.CreateAsync(SampleAgent(idSite: 5));

            Assert.Equal(5, created.IdSite);

            var utilisateur = await ctx.Utilisateurs.FirstOrDefaultAsync(u => u.IdAgent == created.IdAgent);
            Assert.NotNull(utilisateur);
            Assert.Equal(5, utilisateur!.IdSite);
            Assert.Equal(1, utilisateur.IdSociete);
        }

        [Fact]
        public async Task CreateAsync_utilisateur_existant_par_telephone_met_a_jour_idSite()
        {
            await using var ctx = BuildDb(nameof(CreateAsync_utilisateur_existant_par_telephone_met_a_jour_idSite));
            await SeedSocieteAndRoleAsync(ctx);
            var svc = CreateService(ctx);

            const string sharedPhone = "+243900000999";

            var first = SampleAgent(telephone: sharedPhone, idSite: 5);
            first.NomComplet = "Agent Site 5";
            await svc.CreateAsync(first);

            var userAfterFirst = await ctx.Utilisateurs.FirstAsync(u => u.Telephone == sharedPhone);
            Assert.Equal(5, userAfterFirst.IdSite);
            Assert.Equal(first.IdAgent, userAfterFirst.IdAgent);

            var second = SampleAgent(telephone: sharedPhone, idSite: 7);
            second.NomComplet = "Agent Site 7";
            await svc.CreateAsync(second);

            var userAfterSecond = await ctx.Utilisateurs.FirstAsync(u => u.Telephone == sharedPhone);
            Assert.Equal(7, userAfterSecond.IdSite);
            Assert.Equal(second.IdAgent, userAfterSecond.IdAgent);
        }
    }
}
