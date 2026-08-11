using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Enums;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Restaurant;
using Xunit;

namespace CongoTravel.Tests
{
    public class RestaurantPlanificationTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static RestaurantPlanificationService CreatePlanifService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<RestaurantPlanificationService>.Instance);

        private static RestaurantCreneauGenerationService CreateGenerationService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new RestaurantCreneauService(ctx, NullLogger<RestaurantCreneauService>.Instance),
                NullLogger<RestaurantCreneauGenerationService>.Instance);

        private static async Task<(int IdSociete, int IdRestaurant, int PlanifId)> SeedPlanifGlobalTwoPlagesAsync(
            CongoTravelDbContext ctx,
            List<int> joursSemaine,
            int capacite = 40,
            decimal prix = 25m)
        {
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx);

            var etablissementService = new RestaurantEtablissementService(
                ctx, NullLogger<RestaurantEtablissementService>.Instance);
            var etablissement = await etablissementService.PublishAsync(
                (await etablissementService.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
                {
                    CodeRestaurant = $"REST-PL-{Guid.NewGuid():N}"[..12],
                    Nom = "Restaurant Planif",
                    IdSite = idSite,
                    AcomptePourcentDefaut = 20m
                }, idSociete)).IdRestaurant,
                idSociete);

            var planif = await CreatePlanifService(ctx).CreateAsync(new RestaurantCreatePlanificationRequestDto
            {
                Libelle = "Service lundi",
                IdRestaurant = etablissement.IdRestaurant,
                JoursSemaine = joursSemaine,
                InventoryMode = RestaurantInventoryMode.GlobalQuota,
                CodeDevise = "USD",
                Statut = true,
                Plages = new List<RestaurantCreatePlanificationPlageDto>
                {
                    new()
                    {
                        Ordre = 0,
                        Libelle = "Midi",
                        StartTime = new TimeOnly(12, 0),
                        EndTime = new TimeOnly(14, 0),
                        GlobalQuota = new RestaurantCreatePlanificationGlobalQuotaDto
                        {
                            CapaciteTotale = capacite,
                            PrixUnitaire = prix
                        }
                    },
                    new()
                    {
                        Ordre = 1,
                        Libelle = "Soir",
                        StartTime = new TimeOnly(19, 0),
                        EndTime = new TimeOnly(22, 0),
                        GlobalQuota = new RestaurantCreatePlanificationGlobalQuotaDto
                        {
                            CapaciteTotale = capacite,
                            PrixUnitaire = prix
                        }
                    }
                }
            }, idSociete);

            return (idSociete, etablissement.IdRestaurant, planif.IdRestaurantPlanification);
        }

        [Fact]
        public async Task Generer_PeriodePersonnalisee_cree_Draft_pour_chaque_date_x_plage()
        {
            await using var ctx = BuildDb($"{nameof(RestaurantPlanificationTests)}_{nameof(Generer_PeriodePersonnalisee_cree_Draft_pour_chaque_date_x_plage)}");
            var seed = await SeedPlanifGlobalTwoPlagesAsync(ctx, new List<int> { (int)DayOfWeek.Monday });

            // Semaine du 1er juin 2026 (lundi) → 1 lundi × 2 plages = 2
            var result = await CreateGenerationService(ctx).GenererAsync(
                seed.PlanifId,
                new GenererRestaurantPlanificationDto
                {
                    Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                    DateDebut = new DateTime(2026, 6, 1),
                    DateFin = new DateTime(2026, 6, 7)
                });

            Assert.Equal(2, result.Resume.Creees);
            Assert.Equal(0, result.Resume.Publiees);
            Assert.All(
                result.Details.Where(d => d.Statut == PlanificationGenerationItemStatut.Cree),
                d => Assert.NotNull(d.IdCreneau));

            var creneaux = await ctx.RestaurantCreneaux
                .Where(c => c.IdRestaurantPlanification == seed.PlanifId)
                .ToListAsync();
            Assert.Equal(2, creneaux.Count);
            Assert.All(creneaux, c => Assert.Equal(RestaurantStatus.Draft, c.Status));
            Assert.All(creneaux, c => Assert.Equal(DayOfWeek.Monday, c.DateService.DayOfWeek));
            Assert.All(result.Details, d => Assert.False(d.Publiee));
        }

        [Fact]
        public async Task Generer_avec_publierApresGeneration_publie_creneaux_crees()
        {
            await using var ctx = BuildDb($"{nameof(RestaurantPlanificationTests)}_{nameof(Generer_avec_publierApresGeneration_publie_creneaux_crees)}");
            var seed = await SeedPlanifGlobalTwoPlagesAsync(ctx, new List<int> { (int)DayOfWeek.Monday });

            var result = await CreateGenerationService(ctx).GenererAsync(
                seed.PlanifId,
                new GenererRestaurantPlanificationDto
                {
                    Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                    DateDebut = new DateTime(2026, 6, 1),
                    DateFin = new DateTime(2026, 6, 7),
                    PublierApresGeneration = true
                });

            Assert.Equal(2, result.Resume.Creees);
            Assert.Equal(2, result.Resume.Publiees);
            Assert.All(
                result.Details.Where(d => d.Statut == PlanificationGenerationItemStatut.Cree),
                d => Assert.True(d.Publiee));

            var creneaux = await ctx.RestaurantCreneaux
                .Where(c => c.IdRestaurantPlanification == seed.PlanifId)
                .ToListAsync();
            Assert.Equal(2, creneaux.Count);
            Assert.All(creneaux, c => Assert.Equal(RestaurantStatus.Published, c.Status));
        }

        [Fact]
        public async Task Generer_deuxieme_fois_ignore_creneaux_existants()
        {
            await using var ctx = BuildDb($"{nameof(RestaurantPlanificationTests)}_{nameof(Generer_deuxieme_fois_ignore_creneaux_existants)}");
            var seed = await SeedPlanifGlobalTwoPlagesAsync(ctx, new List<int> { (int)DayOfWeek.Monday });

            var gen = CreateGenerationService(ctx);
            var request = new GenererRestaurantPlanificationDto
            {
                Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                DateDebut = new DateTime(2026, 6, 1),
                DateFin = new DateTime(2026, 6, 7)
            };

            var first = await gen.GenererAsync(seed.PlanifId, request);
            var second = await gen.GenererAsync(seed.PlanifId, request);

            Assert.Equal(2, first.Resume.Creees);
            Assert.Equal(0, second.Resume.Creees);
            Assert.Equal(2, second.Resume.Ignorees);

            var count = await ctx.RestaurantCreneaux
                .CountAsync(c => c.IdRestaurantPlanification == seed.PlanifId);
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task Create_plages_chevauchantes_leve_ArgumentException()
        {
            await using var ctx = BuildDb($"{nameof(RestaurantPlanificationTests)}_{nameof(Create_plages_chevauchantes_leve_ArgumentException)}");
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx);

            var etablissementService = new RestaurantEtablissementService(
                ctx, NullLogger<RestaurantEtablissementService>.Instance);
            var etablissement = await etablissementService.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
            {
                CodeRestaurant = $"REST-OV-{Guid.NewGuid():N}"[..12],
                Nom = "Restaurant Overlap",
                IdSite = idSite
            }, idSociete);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                CreatePlanifService(ctx).CreateAsync(new RestaurantCreatePlanificationRequestDto
                {
                    Libelle = "Overlap",
                    IdRestaurant = etablissement.IdRestaurant,
                    JoursSemaine = new List<int> { (int)DayOfWeek.Friday },
                    InventoryMode = RestaurantInventoryMode.GlobalQuota,
                    CodeDevise = "USD",
                    Plages = new List<RestaurantCreatePlanificationPlageDto>
                    {
                        new()
                        {
                            StartTime = new TimeOnly(12, 0),
                            EndTime = new TimeOnly(15, 0),
                            GlobalQuota = new RestaurantCreatePlanificationGlobalQuotaDto
                            {
                                CapaciteTotale = 10,
                                PrixUnitaire = 10m
                            }
                        },
                        new()
                        {
                            StartTime = new TimeOnly(14, 0),
                            EndTime = new TimeOnly(17, 0),
                            GlobalQuota = new RestaurantCreatePlanificationGlobalQuotaDto
                            {
                                CapaciteTotale = 10,
                                PrixUnitaire = 10m
                            }
                        }
                    }
                }, idSociete));

            Assert.Contains("chevauch", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Update_template_capacite_ne_change_pas_creneau_deja_genere()
        {
            await using var ctx = BuildDb($"{nameof(RestaurantPlanificationTests)}_{nameof(Update_template_capacite_ne_change_pas_creneau_deja_genere)}");
            var seed = await SeedPlanifGlobalTwoPlagesAsync(ctx, new List<int> { (int)DayOfWeek.Monday }, capacite: 40, prix: 25m);

            await CreateGenerationService(ctx).GenererAsync(
                seed.PlanifId,
                new GenererRestaurantPlanificationDto
                {
                    Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                    DateDebut = new DateTime(2026, 6, 1),
                    DateFin = new DateTime(2026, 6, 7)
                });

            await CreatePlanifService(ctx).UpdateAsync(new RestaurantUpdatePlanificationRequestDto
            {
                IdRestaurantPlanification = seed.PlanifId,
                Libelle = "Modifié",
                IdRestaurant = seed.IdRestaurant,
                JoursSemaine = new List<int> { (int)DayOfWeek.Monday },
                InventoryMode = RestaurantInventoryMode.GlobalQuota,
                CodeDevise = "USD",
                Statut = true,
                Plages = new List<RestaurantCreatePlanificationPlageDto>
                {
                    new()
                    {
                        Ordre = 0,
                        Libelle = "Midi",
                        StartTime = new TimeOnly(12, 0),
                        EndTime = new TimeOnly(14, 0),
                        GlobalQuota = new RestaurantCreatePlanificationGlobalQuotaDto
                        {
                            CapaciteTotale = 999,
                            PrixUnitaire = 99m
                        }
                    },
                    new()
                    {
                        Ordre = 1,
                        Libelle = "Soir",
                        StartTime = new TimeOnly(19, 0),
                        EndTime = new TimeOnly(22, 0),
                        GlobalQuota = new RestaurantCreatePlanificationGlobalQuotaDto
                        {
                            CapaciteTotale = 999,
                            PrixUnitaire = 99m
                        }
                    }
                }
            }, seed.IdSociete);

            var creneau = await ctx.RestaurantCreneaux
                .Include(c => c.GlobalQuota)
                .FirstAsync(c => c.IdRestaurantPlanification == seed.PlanifId);

            Assert.Equal(40, creneau.GlobalQuota!.CapaciteTotale);
            Assert.Equal(25m, creneau.GlobalQuota.PrixUnitaire);
        }
    }
}
