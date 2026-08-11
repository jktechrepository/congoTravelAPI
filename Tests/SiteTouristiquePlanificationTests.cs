using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.Enums;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.SiteTouristique;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiquePlanificationTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static SiteTouristiquePlanificationService CreatePlanifService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<SiteTouristiquePlanificationService>.Instance);

        private static SiteTouristiqueJourneeGenerationService CreateGenerationService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new SiteTouristiqueJourneeService(ctx, NullLogger<SiteTouristiqueJourneeService>.Instance),
                NullLogger<SiteTouristiqueJourneeGenerationService>.Instance);

        private static async Task<(int IdSociete, int IdSiteTouristique, int PlanifId)> SeedPlanifGlobalAsync(
            CongoTravelDbContext ctx,
            List<int> joursSemaine,
            int capacite = 100,
            decimal prix = 15m)
        {
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx);

            var lieuService = new SiteTouristiqueLieuService(ctx, NullLogger<SiteTouristiqueLieuService>.Instance);
            var lieu = await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = $"PL-{Guid.NewGuid():N}"[..10],
                Nom = "Lieu Planif",
                IdSite = idSite
            }, idSociete);
            await lieuService.PublishAsync(lieu.IdSiteTouristique, idSociete);

            var planif = await CreatePlanifService(ctx).CreateAsync(new SiteTouristiqueCreatePlanificationRequestDto
            {
                Libelle = "Visites lundi",
                IdSiteTouristique = lieu.IdSiteTouristique,
                JoursSemaine = joursSemaine,
                InventoryMode = SiteTouristiqueInventoryMode.GlobalQuota,
                CodeDevise = "USD",
                GlobalQuota = new SiteTouristiqueCreatePlanificationGlobalQuotaDto
                {
                    CapaciteTotale = capacite,
                    PrixUnitaire = prix
                },
                Statut = true
            }, idSociete);

            return (idSociete, lieu.IdSiteTouristique, planif.IdSiteTouristiquePlanification);
        }

        [Fact]
        public async Task Generer_PeriodePersonnalisee_cree_Draft_pour_jours_selectionnes()
        {
            await using var ctx = BuildDb($"{nameof(SiteTouristiquePlanificationTests)}_{nameof(Generer_PeriodePersonnalisee_cree_Draft_pour_jours_selectionnes)}");
            var seed = await SeedPlanifGlobalAsync(ctx, new List<int> { (int)DayOfWeek.Monday });

            var result = await CreateGenerationService(ctx).GenererAsync(
                seed.PlanifId,
                new GenererSiteTouristiquePlanificationDto
                {
                    Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                    DateDebut = new DateTime(2026, 6, 1),
                    DateFin = new DateTime(2026, 6, 30)
                });

            Assert.Equal(5, result.Resume.Creees);
            Assert.All(
                result.Details.Where(d => d.Statut == PlanificationGenerationItemStatut.Cree),
                d => Assert.NotNull(d.IdJournee));

            var journees = await ctx.SiteTouristiqueJournees
                .Where(j => j.IdSiteTouristiquePlanification == seed.PlanifId)
                .ToListAsync();
            Assert.Equal(5, journees.Count);
            Assert.All(journees, j => Assert.Equal(SiteTouristiqueStatus.Draft, j.Status));
            Assert.All(journees, j => Assert.Equal(DayOfWeek.Monday, j.DateVisite.DayOfWeek));
            Assert.Equal(0, result.Resume.Publiees);
            Assert.All(result.Details, d => Assert.False(d.Publiee));
        }

        [Fact]
        public async Task Generer_avec_publierApresGeneration_publie_journees_creees()
        {
            await using var ctx = BuildDb($"{nameof(SiteTouristiquePlanificationTests)}_{nameof(Generer_avec_publierApresGeneration_publie_journees_creees)}");
            var seed = await SeedPlanifGlobalAsync(ctx, new List<int> { (int)DayOfWeek.Monday });

            var result = await CreateGenerationService(ctx).GenererAsync(
                seed.PlanifId,
                new GenererSiteTouristiquePlanificationDto
                {
                    Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                    DateDebut = new DateTime(2026, 6, 1),
                    DateFin = new DateTime(2026, 6, 30),
                    PublierApresGeneration = true
                });

            Assert.Equal(5, result.Resume.Creees);
            Assert.Equal(5, result.Resume.Publiees);
            Assert.All(
                result.Details.Where(d => d.Statut == PlanificationGenerationItemStatut.Cree),
                d => Assert.True(d.Publiee));

            var journees = await ctx.SiteTouristiqueJournees
                .Where(j => j.IdSiteTouristiquePlanification == seed.PlanifId)
                .ToListAsync();
            Assert.Equal(5, journees.Count);
            Assert.All(journees, j => Assert.Equal(SiteTouristiqueStatus.Published, j.Status));
        }

        [Fact]
        public async Task Generer_publierApresGeneration_lieu_Draft_garde_journees_Draft()
        {
            await using var ctx = BuildDb($"{nameof(SiteTouristiquePlanificationTests)}_{nameof(Generer_publierApresGeneration_lieu_Draft_garde_journees_Draft)}");
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx);

            var lieuService = new SiteTouristiqueLieuService(ctx, NullLogger<SiteTouristiqueLieuService>.Instance);
            var lieu = await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = $"PL-{Guid.NewGuid():N}"[..10],
                Nom = "Lieu Draft Planif",
                IdSite = idSite
            }, idSociete);
            // Intentionnellement non publié

            var planif = await CreatePlanifService(ctx).CreateAsync(new SiteTouristiqueCreatePlanificationRequestDto
            {
                Libelle = "Visites lundi lieu draft",
                IdSiteTouristique = lieu.IdSiteTouristique,
                JoursSemaine = new List<int> { (int)DayOfWeek.Monday },
                InventoryMode = SiteTouristiqueInventoryMode.GlobalQuota,
                CodeDevise = "USD",
                GlobalQuota = new SiteTouristiqueCreatePlanificationGlobalQuotaDto
                {
                    CapaciteTotale = 50,
                    PrixUnitaire = 10m
                },
                Statut = true
            }, idSociete);

            var result = await CreateGenerationService(ctx).GenererAsync(
                planif.IdSiteTouristiquePlanification,
                new GenererSiteTouristiquePlanificationDto
                {
                    Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                    DateDebut = new DateTime(2026, 6, 1),
                    DateFin = new DateTime(2026, 6, 7),
                    PublierApresGeneration = true
                });

            Assert.True(result.Resume.Creees >= 1);
            Assert.Equal(0, result.Resume.Publiees);
            Assert.All(
                result.Details.Where(d => d.Statut == PlanificationGenerationItemStatut.Cree),
                d =>
                {
                    Assert.False(d.Publiee);
                    Assert.NotNull(d.Message);
                    Assert.Contains("publish échoué", d.Message, StringComparison.OrdinalIgnoreCase);
                });

            var journees = await ctx.SiteTouristiqueJournees
                .Where(j => j.IdSiteTouristiquePlanification == planif.IdSiteTouristiquePlanification)
                .ToListAsync();
            Assert.All(journees, j => Assert.Equal(SiteTouristiqueStatus.Draft, j.Status));
        }

        [Fact]
        public async Task Generer_deuxieme_fois_ignore_dates_existantes()
        {
            await using var ctx = BuildDb($"{nameof(SiteTouristiquePlanificationTests)}_{nameof(Generer_deuxieme_fois_ignore_dates_existantes)}");
            var seed = await SeedPlanifGlobalAsync(ctx, new List<int> { (int)DayOfWeek.Monday });

            var gen = CreateGenerationService(ctx);
            var request = new GenererSiteTouristiquePlanificationDto
            {
                Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                DateDebut = new DateTime(2026, 6, 1),
                DateFin = new DateTime(2026, 6, 30)
            };

            var first = await gen.GenererAsync(seed.PlanifId, request);
            var second = await gen.GenererAsync(seed.PlanifId, request);

            Assert.Equal(5, first.Resume.Creees);
            Assert.Equal(0, second.Resume.Creees);
            Assert.Equal(5, second.Resume.Ignorees);

            var count = await ctx.SiteTouristiqueJournees
                .CountAsync(j => j.IdSiteTouristiquePlanification == seed.PlanifId);
            Assert.Equal(5, count);
        }

        [Fact]
        public async Task Update_template_capacite_ne_change_pas_journee_deja_generee()
        {
            await using var ctx = BuildDb($"{nameof(SiteTouristiquePlanificationTests)}_{nameof(Update_template_capacite_ne_change_pas_journee_deja_generee)}");
            var seed = await SeedPlanifGlobalAsync(ctx, new List<int> { (int)DayOfWeek.Monday }, capacite: 100, prix: 15m);

            await CreateGenerationService(ctx).GenererAsync(
                seed.PlanifId,
                new GenererSiteTouristiquePlanificationDto
                {
                    Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                    DateDebut = new DateTime(2026, 6, 1),
                    DateFin = new DateTime(2026, 6, 7)
                });

            await CreatePlanifService(ctx).UpdateAsync(new SiteTouristiqueUpdatePlanificationRequestDto
            {
                IdSiteTouristiquePlanification = seed.PlanifId,
                Libelle = "Modifié",
                IdSiteTouristique = seed.IdSiteTouristique,
                JoursSemaine = new List<int> { (int)DayOfWeek.Monday },
                InventoryMode = SiteTouristiqueInventoryMode.GlobalQuota,
                CodeDevise = "USD",
                GlobalQuota = new SiteTouristiqueCreatePlanificationGlobalQuotaDto
                {
                    CapaciteTotale = 999,
                    PrixUnitaire = 99m
                },
                Statut = true
            }, seed.IdSociete);

            var journee = await ctx.SiteTouristiqueJournees
                .Include(j => j.GlobalQuota)
                .FirstAsync(j => j.IdSiteTouristiquePlanification == seed.PlanifId);

            Assert.Equal(100, journee.GlobalQuota!.CapaciteTotale);
            Assert.Equal(15m, journee.GlobalQuota.PrixUnitaire);
        }
    }
}
