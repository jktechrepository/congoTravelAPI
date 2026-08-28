using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Enums;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.PhotoStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CongoTravel.Tests
{
    public class HotelPlanificationTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static HotelPlanificationService CreatePlanifService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<HotelPlanificationService>.Instance);

        private static HotelAllotmentGenerationService CreateGenerationService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                HotelTestFactories.CreateAllotmentService(ctx),
                HotelTestFactories.CreateNightService(ctx),
                NullLogger<HotelAllotmentGenerationService>.Instance);

        private static async Task<(int IdSociete, int IdHotel, int IdRoomType, int PlanifId)> SeedPlanifAsync(
            CongoTravelDbContext ctx,
            List<int> joursSemaine,
            int capacite = 5,
            decimal prix = 80m,
            string suffix = "P7a")
        {
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(
                ctx, $"Hotel Planif {suffix}");
            var hotels = HotelTestFactories.CreateEtablissementService(ctx);
            var hotel = await hotels.PublishAsync((await hotels.CreateDraftAsync(new()
            {
                CodeHotel = $"HOT-{suffix}", Nom = $"Hôtel {suffix}", IdSite = idSite
            }, idSociete)).IdHotel, idSociete);
            var roomTypes = HotelTestFactories.CreateRoomTypeService(ctx);
            var room = await roomTypes.PublishAsync((await roomTypes.CreateDraftAsync(new()
            {
                IdHotel = hotel.IdHotel, Code = "STD", Libelle = "Standard",
                PrixNuitReference = prix, CodeDevise = "USD"
            }, idSociete)).IdHotelRoomType, idSociete);

            var planif = await CreatePlanifService(ctx).CreateAsync(new HotelCreatePlanificationRequestDto
            {
                Libelle = "Week-end STD",
                IdHotel = hotel.IdHotel,
                JoursSemaine = joursSemaine,
                CodeDevise = "USD",
                Statut = true,
                Lignes = new List<HotelCreatePlanificationLigneDto>
                {
                    new()
                    {
                        IdHotelRoomType = room.IdHotelRoomType,
                        CapaciteTotale = capacite,
                        PrixNuit = prix
                    }
                }
            }, idSociete);

            return (idSociete, hotel.IdHotel, room.IdHotelRoomType, planif.IdHotelPlanification);
        }

        [Fact]
        public void AddHotelReservations_registers_planification_services()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<CongoTravelDbContext>(o =>
                o.UseInMemoryDatabase(nameof(AddHotelReservations_registers_planification_services)));
            var store = PhotoStorageTestFactory.CreateBlobStoreMock().Object;
            services.AddSingleton<ICongoTravelPhotoBlobStore>(store);
            services.AddSingleton<IPhotoBinaryHydrator>(PhotoStorageTestFactory.CreateHydrator(store));
            services.AddHotelReservations();
            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHotelPlanificationService>());
            Assert.NotNull(provider.GetService<IHotelAllotmentGenerationService>());
            Assert.NotNull(provider.GetService<IHotelNightService>());
        }

        [Fact]
        public async Task Generer_PeriodePersonnalisee_cree_Draft_pour_jours_selectionnes()
        {
            await using var ctx = BuildDb($"{nameof(HotelPlanificationTests)}_{nameof(Generer_PeriodePersonnalisee_cree_Draft_pour_jours_selectionnes)}");
            var seed = await SeedPlanifAsync(ctx, new List<int> { (int)DayOfWeek.Monday }, suffix: "Mon");

            var result = await CreateGenerationService(ctx).GenererAsync(
                seed.PlanifId,
                new GenererHotelPlanificationDto
                {
                    Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                    DateDebut = new DateTime(2026, 6, 1),
                    DateFin = new DateTime(2026, 6, 30)
                });

            Assert.Equal(5, result.Resume.Creees);
            Assert.All(
                result.Details.Where(d => d.Statut == PlanificationGenerationItemStatut.Cree),
                d => Assert.NotNull(d.IdHotelNightAllotment));

            var allotments = await ctx.HotelNightAllotments
                .Where(a => a.IdHotelPlanification == seed.PlanifId)
                .ToListAsync();
            Assert.Equal(5, allotments.Count);
            Assert.All(allotments, a => Assert.Equal(HotelStatus.Draft, a.Status));
            Assert.All(allotments, a => Assert.Equal(DayOfWeek.Monday, a.NightDate.DayOfWeek));
            Assert.Equal(0, result.Resume.Publiees);
        }

        [Fact]
        public async Task Generer_avec_publierApresGeneration_publie_allotments_crees()
        {
            await using var ctx = BuildDb($"{nameof(HotelPlanificationTests)}_{nameof(Generer_avec_publierApresGeneration_publie_allotments_crees)}");
            var seed = await SeedPlanifAsync(ctx, new List<int> { (int)DayOfWeek.Monday }, suffix: "Pub");

            var result = await CreateGenerationService(ctx).GenererAsync(
                seed.PlanifId,
                new GenererHotelPlanificationDto
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

            var allotments = await ctx.HotelNightAllotments
                .Where(a => a.IdHotelPlanification == seed.PlanifId)
                .ToListAsync();
            Assert.All(allotments, a => Assert.Equal(HotelStatus.Published, a.Status));
        }

        [Fact]
        public async Task Generer_deuxieme_fois_ignore_nuits_existantes()
        {
            await using var ctx = BuildDb($"{nameof(HotelPlanificationTests)}_{nameof(Generer_deuxieme_fois_ignore_nuits_existantes)}");
            var seed = await SeedPlanifAsync(ctx, new List<int> { (int)DayOfWeek.Monday }, suffix: "Idem");

            var gen = CreateGenerationService(ctx);
            var request = new GenererHotelPlanificationDto
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

            var count = await ctx.HotelNightAllotments
                .CountAsync(a => a.IdHotelPlanification == seed.PlanifId);
            Assert.Equal(5, count);
        }

        [Fact]
        public async Task Update_template_capacite_ne_change_pas_allotment_deja_genere()
        {
            await using var ctx = BuildDb($"{nameof(HotelPlanificationTests)}_{nameof(Update_template_capacite_ne_change_pas_allotment_deja_genere)}");
            var seed = await SeedPlanifAsync(ctx, new List<int> { (int)DayOfWeek.Monday }, capacite: 5, prix: 80m, suffix: "Upd");

            await CreateGenerationService(ctx).GenererAsync(
                seed.PlanifId,
                new GenererHotelPlanificationDto
                {
                    Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                    DateDebut = new DateTime(2026, 6, 1),
                    DateFin = new DateTime(2026, 6, 7)
                });

            await CreatePlanifService(ctx).UpdateAsync(new HotelUpdatePlanificationRequestDto
            {
                IdHotelPlanification = seed.PlanifId,
                Libelle = "Modifié",
                IdHotel = seed.IdHotel,
                JoursSemaine = new List<int> { (int)DayOfWeek.Monday },
                CodeDevise = "USD",
                Statut = true,
                Lignes = new List<HotelCreatePlanificationLigneDto>
                {
                    new()
                    {
                        IdHotelRoomType = seed.IdRoomType,
                        CapaciteTotale = 999,
                        PrixNuit = 199m
                    }
                }
            }, seed.IdSociete);

            var allotment = await ctx.HotelNightAllotments
                .FirstAsync(a => a.IdHotelPlanification == seed.PlanifId);

            Assert.Equal(5, allotment.CapaciteTotale);
            Assert.Equal(80m, allotment.PrixNuit);
        }
    }
}
