using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Enums;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Hotel.Strategies;
using CongoTravel.Services.PhotoStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CongoTravel.Tests
{
    public class HotelPhase7bGlobalQuotaTests
    {
        private static CongoTravelDbContext Db(string name) => new(
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

        private static async Task<(int societe, int hotel, DateTime from, DateTime to)> SeedPublishedNightsAsync(
            CongoTravelDbContext db, string suffix, int capacity = 2, decimal prix = 100m)
        {
            var (societe, site) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(db, "H7b " + suffix);
            var hotels = HotelTestFactories.CreateEtablissementService(db);
            var hotel = await hotels.PublishAsync((await hotels.CreateDraftAsync(new()
            {
                CodeHotel = "H7b-" + suffix, Nom = "Hotel GQ " + suffix, IdSite = site,
                AcomptePourcentDefaut = 25m
            }, societe)).IdHotel, societe);

            var from = new DateTime(2026, 11, 10);
            var to = from.AddDays(2);
            var nights = HotelTestFactories.CreateNightService(db);
            var batch = await nights.CreateDraftBatchAsync(new HotelCreateNightBatchRequestDto
            {
                IdHotel = hotel.IdHotel,
                From = from,
                To = to,
                CapaciteTotale = capacity,
                PrixNuit = prix,
                CodeDevise = "USD"
            }, societe);
            foreach (var row in batch.Created)
                await nights.PublishAsync(row.IdHotelNight, societe);

            return (societe, hotel.IdHotel, from, to);
        }

        private static (HotelHoldService holds, HotelReservationWithPaiementService facade) Services(
            CongoTravelDbContext db)
        {
            var holdFactory = new HotelInventoryHoldStrategyFactory(
                new HotelGlobalQuotaHoldStrategy(db), new HotelClassQuotaHoldStrategy(db));
            var confirmFactory = new HotelInventoryConfirmStrategyFactory(
                new HotelGlobalQuotaConfirmStrategy(db), new HotelClassQuotaConfirmStrategy(db));
            var cancelFactory = new HotelInventoryCancelStrategyFactory(
                new HotelGlobalQuotaCancelStrategy(db), new HotelClassQuotaCancelStrategy(db));
            var hold = new HotelHoldService(db, holdFactory, NullLogger<HotelHoldService>.Instance);
            var confirmation = new HotelReservationConfirmationService(confirmFactory);
            var payment = new HotelPaymentService(db, confirmation);
            var reservations = new HotelReservationService(db, cancelFactory);
            return (hold, new HotelReservationWithPaiementService(db, hold, payment, reservations));
        }

        [Fact]
        public void DI_registers_phase7b_services()
        {
            var services = new ServiceCollection().AddLogging();
            services.AddDbContext<CongoTravelDbContext>(o =>
                o.UseInMemoryDatabase(nameof(DI_registers_phase7b_services)));
            var store = PhotoStorageTestFactory.CreateBlobStoreMock().Object;
            services.AddSingleton<ICongoTravelPhotoBlobStore>(store);
            services.AddSingleton<IPhotoBinaryHydrator>(PhotoStorageTestFactory.CreateHydrator(store));
            services.AddHotelReservations();
            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHotelNightService>());
            Assert.NotNull(provider.GetService<IHotelInventoryHoldStrategyFactory>());
            Assert.NotNull(provider.GetService<HotelGlobalQuotaHoldStrategy>());
            Assert.IsType<HotelGlobalQuotaHoldStrategy>(
                provider.GetRequiredService<IHotelInventoryHoldStrategyFactory>()
                    .GetStrategy(HotelInventoryMode.GlobalQuota));
        }

        [Fact]
        public async Task CreateDraft_and_publish_night()
        {
            await using var db = Db(nameof(CreateDraft_and_publish_night));
            var (societe, site) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(db, "H7b Night");
            var hotels = HotelTestFactories.CreateEtablissementService(db);
            var hotel = await hotels.PublishAsync((await hotels.CreateDraftAsync(new()
            {
                CodeHotel = "H7b-N1", Nom = "Night Hotel", IdSite = site
            }, societe)).IdHotel, societe);

            var nights = HotelTestFactories.CreateNightService(db);
            var draft = await nights.CreateDraftAsync(new HotelCreateNightRequestDto
            {
                IdHotel = hotel.IdHotel,
                NightDate = new DateTime(2026, 11, 1),
                CapaciteTotale = 5,
                PrixNuit = 80m,
                CodeDevise = "USD"
            }, societe);
            Assert.Equal("Draft", draft.Status);

            var published = await nights.PublishAsync(draft.IdHotelNight, societe);
            Assert.Equal("Published", published.Status);
            Assert.Equal(5, published.QuantiteDisponible);
        }

        [Fact]
        public async Task Cash_hold_confirm_global_multi_nuit()
        {
            await using var db = Db(nameof(Cash_hold_confirm_global_multi_nuit));
            var (_, hotel, from, to) = await SeedPublishedNightsAsync(db, "Cash", capacity: 3);
            var result = await Services(db).facade.CreateCashAsync(new HotelReservationWithPaiementRequestDto
            {
                IdHotel = hotel,
                CheckInDate = from,
                CheckOutDate = to,
                Items = new List<HotelHoldItemRequestDto> { new() { Quantity = 2 } },
                Paiement = new HotelReservationPaiementBlockDto { MethodePaiement = "CASH" }
            });

            Assert.Equal("CONFIRMED", result.Reservation.Status);
            Assert.Equal(nameof(HotelInventoryMode.GlobalQuota), result.Reservation.InventoryMode);

            var nights = await db.HotelNights.Where(n => n.IdHotel == hotel).ToListAsync();
            Assert.All(nights, n =>
            {
                Assert.Equal(0, n.QuantiteHold);
                Assert.Equal(2, n.QuantiteVendue);
            });
        }

        [Fact]
        public async Task Hold_global_oversell_throws_conflict()
        {
            await using var db = Db(nameof(Hold_global_oversell_throws_conflict));
            var (societe, hotel, from, to) = await SeedPublishedNightsAsync(db, "Over", capacity: 1);
            var (hold, _) = Services(db);

            await hold.CreateHoldAsync(hotel, societe, new HotelHoldRequestDto
            {
                CheckInDate = from,
                CheckOutDate = to,
                Items = new List<HotelHoldItemRequestDto> { new() { Quantity = 1 } }
            });

            await Assert.ThrowsAsync<HotelHoldConflictException>(() =>
                hold.CreateHoldAsync(hotel, societe, new HotelHoldRequestDto
                {
                    CheckInDate = from,
                    CheckOutDate = to,
                    Items = new List<HotelHoldItemRequestDto> { new() { Quantity = 1 } }
                }));
        }

        [Fact]
        public async Task Planif_global_generer_cree_HotelNight_Draft()
        {
            await using var db = Db(nameof(Planif_global_generer_cree_HotelNight_Draft));
            var (societe, site) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(db, "H7b Planif");
            var hotels = HotelTestFactories.CreateEtablissementService(db);
            var hotel = await hotels.PublishAsync((await hotels.CreateDraftAsync(new()
            {
                CodeHotel = "H7b-PG", Nom = "Planif GQ", IdSite = site
            }, societe)).IdHotel, societe);

            var planifSvc = new HotelPlanificationService(db, NullLogger<HotelPlanificationService>.Instance);
            var planif = await planifSvc.CreateAsync(new HotelCreatePlanificationRequestDto
            {
                Libelle = "Pool lundi",
                IdHotel = hotel.IdHotel,
                JoursSemaine = new List<int> { (int)DayOfWeek.Monday },
                InventoryMode = HotelInventoryMode.GlobalQuota,
                CodeDevise = "USD",
                Statut = true,
                GlobalQuota = new HotelCreatePlanificationGlobalQuotaDto
                {
                    CapaciteTotale = 4,
                    PrixNuit = 90m
                }
            }, societe);

            var gen = new HotelAllotmentGenerationService(
                db,
                HotelTestFactories.CreateAllotmentService(db),
                HotelTestFactories.CreateNightService(db),
                NullLogger<HotelAllotmentGenerationService>.Instance);

            var result = await gen.GenererAsync(planif.IdHotelPlanification, new GenererHotelPlanificationDto
            {
                Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                DateDebut = new DateTime(2026, 6, 1),
                DateFin = new DateTime(2026, 6, 30)
            });

            Assert.Equal(5, result.Resume.Creees);
            var nights = await db.HotelNights
                .Where(n => n.IdHotelPlanification == planif.IdHotelPlanification)
                .ToListAsync();
            Assert.Equal(5, nights.Count);
            Assert.All(nights, n => Assert.Equal(HotelStatus.Draft, n.Status));
            Assert.All(nights, n => Assert.Equal(DayOfWeek.Monday, n.NightDate.DayOfWeek));
        }

        [Fact]
        public async Task Availability_global_returns_inventoryMode()
        {
            await using var db = Db(nameof(Availability_global_returns_inventoryMode));
            var (_, hotel, from, to) = await SeedPublishedNightsAsync(db, "Avail", capacity: 3);
            var avail = HotelTestFactories.CreateAvailabilityService(db);
            var response = await avail.GetAvailabilityAsync(hotel, from, to);

            Assert.Equal(nameof(HotelInventoryMode.GlobalQuota), response.InventoryMode);
            Assert.Equal(3, response.MinDisponible);
            Assert.Equal(2, response.Nights.Count);
        }
    }
}
