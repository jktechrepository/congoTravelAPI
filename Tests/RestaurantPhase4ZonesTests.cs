using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Services;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class RestaurantPhase4ZonesTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockClientUser(int jwtSocieteId = 999)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(u => u.IsStaff).Returns(false);
            mock.SetupGet(u => u.IsSuperAdmin).Returns(false);
            mock.SetupGet(u => u.SocieteId).Returns(jwtSocieteId);
            mock.SetupGet(u => u.UserId).Returns(0);
            return mock;
        }

        private static RestaurantReservationWithPaiementService CreateWithPaiementService(CongoTravelDbContext ctx)
        {
            var hold = RestaurantTestFactories.CreateHoldService(ctx);
            var confirmation = RestaurantTestFactories.CreateConfirmationService(ctx);
            var payment = new RestaurantPaymentService(
                ctx, confirmation, NullLogger<RestaurantPaymentService>.Instance);
            var reservation = RestaurantTestFactories.CreateReservationService(ctx);
            var flexInit = RestaurantTestFactories.CreateFlexPayInitiationService(
                ctx, Mock.Of<IFlexPayService>(), enabled: false);

            return new RestaurantReservationWithPaiementService(
                ctx,
                hold,
                payment,
                flexInit,
                reservation,
                MockClientUser().Object,
                NullLogger<RestaurantReservationWithPaiementService>.Instance);
        }

        private static async Task<(
            int IdSociete,
            int IdSite,
            int IdRestaurant,
            int IdZoneTerrasse,
            int IdZoneSalle,
            int IdCreneau)> SeedPublishedClassQuotaCreneauAsync(
            CongoTravelDbContext ctx,
            string suffix,
            int capaciteTerrasse = 10,
            int capaciteSalle = 20,
            decimal prixTerrasse = 80m,
            decimal prixSalle = 50m,
            decimal acomptePourcent = 20m)
        {
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(
                ctx, $"Resto P4 {suffix}");

            var etablissementService = RestaurantTestFactories.CreateEtablissementService(ctx);
            var zoneService = new RestaurantZoneService(
                ctx, NullLogger<RestaurantZoneService>.Instance);
            var creneauService = new RestaurantCreneauService(
                ctx, NullLogger<RestaurantCreneauService>.Instance);

            var etablissement = await etablissementService.PublishAsync(
                (await etablissementService.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
                {
                    CodeRestaurant = $"REST-P4-{suffix}",
                    Nom = $"Restaurant P4 {suffix}",
                    IdSite = idSite,
                    AcomptePourcentDefaut = acomptePourcent
                }, idSociete)).IdRestaurant,
                idSociete);

            var terrasse = await zoneService.CreateAsync(new RestaurantCreateZoneRequestDto
            {
                IdRestaurant = etablissement.IdRestaurant,
                Code = "TERR",
                Libelle = "Terrasse"
            }, idSociete);

            var salle = await zoneService.CreateAsync(new RestaurantCreateZoneRequestDto
            {
                IdRestaurant = etablissement.IdRestaurant,
                Code = "SALLE",
                Libelle = "Salle"
            }, idSociete);

            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(19);
            var draft = await creneauService.CreateDraftAsync(new RestaurantCreateCreneauRequestDto
            {
                IdRestaurant = etablissement.IdRestaurant,
                DateService = DateOnly.FromDateTime(start),
                StartAtUtc = start,
                EndAtUtc = start.AddHours(2),
                InventoryMode = "ClassQuota",
                CodeDevise = "USD",
                ZoneQuotas = new List<RestaurantCreateCreneauZoneQuotaDto>
                {
                    new()
                    {
                        IdRestaurantZone = terrasse.IdRestaurantZone,
                        CapaciteTotale = capaciteTerrasse,
                        PrixUnitaire = prixTerrasse
                    },
                    new()
                    {
                        IdRestaurantZone = salle.IdRestaurantZone,
                        CapaciteTotale = capaciteSalle,
                        PrixUnitaire = prixSalle
                    }
                }
            }, idSociete);

            var published = await creneauService.PublishAsync(draft.IdRestaurantCreneau, idSociete);
            return (
                idSociete,
                idSite,
                etablissement.IdRestaurant,
                terrasse.IdRestaurantZone,
                salle.IdRestaurantZone,
                published.IdRestaurantCreneau);
        }

        [Fact]
        public void AddRestaurantReservations_registers_zone_and_classquota_strategies()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<CongoTravelDbContext>(options =>
                options.UseInMemoryDatabase(nameof(AddRestaurantReservations_registers_zone_and_classquota_strategies)));
            services.AddScoped<IConfigSocieteRepository, ConfigSocieteService>();
            services.AddSingleton(Mock.Of<ICurrentUserService>());
            services.AddSingleton(Mock.Of<IFlexPayRealtimeNotifier>());
            services.AddSingleton(Mock.Of<IFlexPayService>());
            services.AddSingleton(Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>());
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(
                new CongoTravel.Configuration.FlexPayOptions { Enabled = true }));
            services.AddScoped<IInfoPaiementResolutionService, InfoPaiementResolutionService>();
            services.AddScoped<IDeviseMontantConverter, DeviseMontantConverter>();
            services.AddRestaurantReservations();

            using var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IRestaurantZoneService>());
            Assert.NotNull(provider.GetService<CongoTravel.Services.Restaurant.Strategies.RestaurantClassQuotaHoldStrategy>());
            Assert.NotNull(provider.GetService<CongoTravel.Services.Restaurant.Strategies.RestaurantClassQuotaConfirmStrategy>());
            Assert.NotNull(provider.GetService<CongoTravel.Services.Restaurant.Strategies.RestaurantClassQuotaCancelStrategy>());
        }

        [Fact]
        public async Task Create_zone_and_ClassQuota_creneau_draft_publish()
        {
            await using var ctx = BuildDb(nameof(Create_zone_and_ClassQuota_creneau_draft_publish));
            var (idSociete, _, idRestaurant, idZoneTerrasse, idZoneSalle, idCreneau) =
                await SeedPublishedClassQuotaCreneauAsync(ctx, "DRAFT");

            var creneau = await ctx.RestaurantCreneaux
                .Include(c => c.ZoneQuotas)
                .SingleAsync(c => c.IdRestaurantCreneau == idCreneau);

            Assert.Equal(Models.Restaurant.Enums.RestaurantInventoryMode.ClassQuota, creneau.InventoryMode);
            Assert.Equal(Models.Restaurant.Enums.RestaurantStatus.Published, creneau.Status);
            Assert.Equal(2, creneau.ZoneQuotas.Count);
            Assert.Contains(creneau.ZoneQuotas, q => q.IdRestaurantZone == idZoneTerrasse);
            Assert.Contains(creneau.ZoneQuotas, q => q.IdRestaurantZone == idZoneSalle);

            var zones = await ctx.RestaurantZones
                .Where(z => z.IdRestaurant == idRestaurant && z.IdSociete == idSociete)
                .ToListAsync();
            Assert.Equal(2, zones.Count);
            Assert.All(zones, z => Assert.True(z.Actif));
        }

        [Fact]
        public async Task CASH_with_paiement_two_zones_updates_vendue_per_zone()
        {
            await using var ctx = BuildDb(nameof(CASH_with_paiement_two_zones_updates_vendue_per_zone));
            // Terrasse 20%*80=16 → 2 couverts = 32 ; Salle 20%*50=10 → 3 = 30 ; total 62
            var (_, _, _, idZoneTerrasse, idZoneSalle, idCreneau) =
                await SeedPublishedClassQuotaCreneauAsync(ctx, "CASH2Z");

            var service = CreateWithPaiementService(ctx);
            var result = await service.CreateCashAsync(new RestaurantReservationWithPaiementRequestDto
            {
                IdRestaurantCreneau = idCreneau,
                CustomerRef = "TABLE-Z",
                Items = new List<RestaurantHoldItemRequestDto>
                {
                    new() { ZoneId = idZoneTerrasse, Quantity = 2 },
                    new() { ZoneId = idZoneSalle, Quantity = 3 }
                },
                Paiement = new RestaurantReservationPaiementBlockDto
                {
                    MethodePaiement = "CASH",
                    ReferenceTransaction = "CAISSE-Z1"
                }
            });

            Assert.Equal("Succes", result.TransactionStatut);
            Assert.Equal("CONFIRMED", result.Reservation.Status);
            Assert.Equal(5, result.Reservation.NombreCouverts);
            Assert.Equal(62m, result.Reservation.MontantSousTotal);
            Assert.Equal(2, result.Reservation.Lines.Count);
            Assert.All(result.Reservation.Lines, l => Assert.Equal("ClassQuota", l.LineType));

            var terrasseQuota = await ctx.RestaurantCreneauZoneQuotas
                .SingleAsync(q => q.IdRestaurantCreneau == idCreneau && q.IdRestaurantZone == idZoneTerrasse);
            Assert.Equal(0, terrasseQuota.QuantiteHold);
            Assert.Equal(2, terrasseQuota.QuantiteVendue);

            var salleQuota = await ctx.RestaurantCreneauZoneQuotas
                .SingleAsync(q => q.IdRestaurantCreneau == idCreneau && q.IdRestaurantZone == idZoneSalle);
            Assert.Equal(0, salleQuota.QuantiteHold);
            Assert.Equal(3, salleQuota.QuantiteVendue);
        }

        [Fact]
        public async Task Insufficient_zone_capacity_throws_conflict()
        {
            await using var ctx = BuildDb(nameof(Insufficient_zone_capacity_throws_conflict));
            var (_, _, _, idZoneTerrasse, _, idCreneau) =
                await SeedPublishedClassQuotaCreneauAsync(ctx, "CONFLICT", capaciteTerrasse: 2, capaciteSalle: 20);

            var service = CreateWithPaiementService(ctx);

            await Assert.ThrowsAsync<RestaurantHoldConflictException>(() =>
                service.CreateCashAsync(new RestaurantReservationWithPaiementRequestDto
                {
                    IdRestaurantCreneau = idCreneau,
                    Items = new List<RestaurantHoldItemRequestDto>
                    {
                        new() { ZoneId = idZoneTerrasse, Quantity = 5 }
                    },
                    Paiement = new RestaurantReservationPaiementBlockDto { MethodePaiement = "CASH" }
                }));

            var quota = await ctx.RestaurantCreneauZoneQuotas
                .SingleAsync(q => q.IdRestaurantCreneau == idCreneau && q.IdRestaurantZone == idZoneTerrasse);
            Assert.Equal(0, quota.QuantiteHold);
            Assert.Equal(0, quota.QuantiteVendue);
        }

        [Fact]
        public async Task GlobalQuota_still_works_regression()
        {
            await using var ctx = BuildDb(nameof(GlobalQuota_still_works_regression));
            var (idSociete, idSite, idCreneau) = await RestaurantTestFactories.SeedPublishedCreneauAsync(
                ctx, "GQ-REG", capacite: 20, prixUnitaire: 50m, acomptePourcent: 20m);

            var service = CreateWithPaiementService(ctx);
            var result = await service.CreateCashAsync(new RestaurantReservationWithPaiementRequestDto
            {
                IdRestaurantCreneau = idCreneau,
                Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 2 } },
                Paiement = new RestaurantReservationPaiementBlockDto { MethodePaiement = "CASH" }
            });

            Assert.Equal("CONFIRMED", result.Reservation.Status);
            Assert.Equal(idSociete, result.Reservation.IdSociete);
            Assert.Equal(idSite, result.Reservation.IdSite);
            Assert.Equal(20m, result.Reservation.MontantSousTotal);

            var quota = await ctx.RestaurantCreneauGlobalQuotas
                .SingleAsync(q => q.IdRestaurantCreneau == idCreneau);
            Assert.Equal(0, quota.QuantiteHold);
            Assert.Equal(2, quota.QuantiteVendue);
        }

        [Fact]
        public async Task Availability_Mode_B_shows_restantes_per_zone()
        {
            await using var ctx = BuildDb(nameof(Availability_Mode_B_shows_restantes_per_zone));
            var (idSociete, _, _, idZoneTerrasse, idZoneSalle, idCreneau) =
                await SeedPublishedClassQuotaCreneauAsync(
                    ctx, "AVAIL", capaciteTerrasse: 10, capaciteSalle: 20, prixTerrasse: 80m, prixSalle: 50m);

            // Hold 3 terrasse without confirm to reduce restantes
            var holdService = RestaurantTestFactories.CreateHoldService(ctx);
            await holdService.CreateHoldAsync(
                idCreneau,
                idSociete,
                new RestaurantHoldRequestDto
                {
                    Items = new List<RestaurantHoldItemRequestDto>
                    {
                        new() { ZoneId = idZoneTerrasse, Quantity = 3 }
                    }
                });

            var availability = await new RestaurantAvailabilityService(
                ctx, NullLogger<RestaurantAvailabilityService>.Instance)
                .GetAvailabilityAsync(idCreneau, idSociete);

            Assert.NotNull(availability);
            Assert.Equal("ClassQuota", availability!.InventoryMode);
            Assert.NotNull(availability.ZoneQuotas);
            Assert.Equal(2, availability.ZoneQuotas!.Count);

            var terrasse = availability.ZoneQuotas.Single(z => z.IdRestaurantZone == idZoneTerrasse);
            Assert.Equal(10, terrasse.CapaciteTotale);
            Assert.Equal(3, terrasse.QuantiteHold);
            Assert.Equal(0, terrasse.QuantiteVendue);
            Assert.Equal(7, terrasse.QuantiteDisponible);
            Assert.Equal(16m, terrasse.MontantAcompteUnitaire); // 20% * 80

            var salle = availability.ZoneQuotas.Single(z => z.IdRestaurantZone == idZoneSalle);
            Assert.Equal(20, salle.QuantiteDisponible);
            Assert.Equal(10m, salle.MontantAcompteUnitaire); // 20% * 50
        }
    }
}
