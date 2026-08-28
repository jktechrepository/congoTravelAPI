using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Models.DTOs.Hotel;
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
    public class HotelPhase7eExtrasTests
    {
        private static CongoTravelDbContext Db(string name) => new(
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

        private static async Task<(int societe, int hotel, int roomType, DateTime from, DateTime to)> SeedClassAsync(
            CongoTravelDbContext db, string suffix, int capacity = 2)
        {
            var (societe, site) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(db, "H7e " + suffix);
            var hotels = HotelTestFactories.CreateEtablissementService(db);
            var hotel = await hotels.PublishAsync((await hotels.CreateDraftAsync(new()
            {
                CodeHotel = "H7e-" + suffix, Nom = "Hotel 7e " + suffix, IdSite = site,
                AcomptePourcentDefaut = 25m
            }, societe)).IdHotel, societe);
            var roomTypes = HotelTestFactories.CreateRoomTypeService(db);
            var roomType = await roomTypes.PublishAsync((await roomTypes.CreateDraftAsync(new()
            {
                IdHotel = hotel.IdHotel, Code = "STD", Libelle = "Standard",
                PrixNuitReference = 100m, CodeDevise = "USD"
            }, societe)).IdHotelRoomType, societe);
            var from = new DateTime(2026, 10, 1);
            var to = from.AddDays(3);
            var allotments = HotelTestFactories.CreateAllotmentService(db);
            var batch = await allotments.CreateDraftBatchAsync(new()
            {
                IdHotel = hotel.IdHotel, IdHotelRoomType = roomType.IdHotelRoomType,
                From = from, To = to, CapaciteTotale = capacity, PrixNuit = 100m, CodeDevise = "USD"
            }, societe);
            foreach (var row in batch.Created)
                await allotments.PublishAsync(row.IdHotelNightAllotment, societe);
            return (societe, hotel.IdHotel, roomType.IdHotelRoomType, from, to);
        }

        private static (HotelReservationService reservations, HotelReservationWithPaiementService facade) Services(
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
            return (reservations, new HotelReservationWithPaiementService(db, hold, payment, reservations));
        }

        private static async Task<HotelReservationWithPaiementResponseDto> ConfirmOneAsync(
            CongoTravelDbContext db, int societe, int hotel, int roomType, DateTime from, DateTime to)
        {
            var (_, facade) = Services(db);
            return await facade.CreateCashAsync(new HotelReservationWithPaiementRequestDto
            {
                IdHotel = hotel, CheckInDate = from, CheckOutDate = to,
                Items = new() { new() { RoomTypeId = roomType, Quantity = 1 } },
                Paiement = new() { MethodePaiement = "CASH" }
            });
        }

        [Fact]
        public void DI_registers_phase7e_extra_service()
        {
            var services = new ServiceCollection().AddLogging();
            services.AddDbContext<CongoTravelDbContext>(o =>
                o.UseInMemoryDatabase(nameof(DI_registers_phase7e_extra_service)));
            var store = PhotoStorageTestFactory.CreateBlobStoreMock().Object;
            services.AddSingleton<ICongoTravelPhotoBlobStore>(store);
            services.AddSingleton<IPhotoBinaryHydrator>(PhotoStorageTestFactory.CreateHydrator(store));
            services.AddHotelReservations();
            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHotelExtraService>());
        }

        [Fact]
        public async Task Create_extra_rejects_duplicate_code()
        {
            await using var db = Db(nameof(Create_extra_rejects_duplicate_code));
            var (societe, hotel, _, _, _) = await SeedClassAsync(db, "UQ");
            var extras = HotelTestFactories.CreateExtraService(db);
            await extras.CreateAsync(new HotelCreateExtraRequestDto
            {
                IdHotel = hotel, Code = "PDJ", Libelle = "Petit-déjeuner",
                PrixUnitaire = 15m, CodeDevise = "USD", PricingUnit = HotelExtraPricingUnit.PerStay
            }, societe);

            await Assert.ThrowsAsync<HotelExtraConflictException>(() =>
                extras.CreateAsync(new HotelCreateExtraRequestDto
                {
                    IdHotel = hotel, Code = "PDJ", Libelle = "Autre PDJ",
                    PrixUnitaire = 20m, PricingUnit = HotelExtraPricingUnit.PerStay
                }, societe));
        }

        [Fact]
        public async Task Set_extras_per_stay_on_confirmed_ok()
        {
            await using var db = Db(nameof(Set_extras_per_stay_on_confirmed_ok));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "PerStay");
            var extra = await HotelTestFactories.CreateExtraService(db).CreateAsync(new()
            {
                IdHotel = hotel, Code = "PARK", Libelle = "Parking",
                PrixUnitaire = 10m, CodeDevise = "USD", PricingUnit = HotelExtraPricingUnit.PerStay
            }, societe);

            var cash = await ConfirmOneAsync(db, societe, hotel, roomType, from, to);
            var montantSejourBefore = cash.Reservation.MontantSejour;
            var (reservations, _) = Services(db);

            var updated = await reservations.SetExtrasAsync(
                cash.Reservation.IdHotelReservation, societe,
                new HotelSetReservationExtrasRequestDto
                {
                    Items = new() { new() { IdHotelExtra = extra.IdHotelExtra, Quantity = 2 } }
                });

            Assert.Equal(20m, updated.MontantExtras);
            Assert.Single(updated.Extras);
            Assert.Equal(20m, updated.Extras[0].MontantLigne);
            Assert.Equal(montantSejourBefore, updated.MontantSejour);
        }

        [Fact]
        public async Task Set_extras_per_night_multiplies_by_nights()
        {
            await using var db = Db(nameof(Set_extras_per_night_multiplies_by_nights));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "PerNight");
            var extra = await HotelTestFactories.CreateExtraService(db).CreateAsync(new()
            {
                IdHotel = hotel, Code = "PDJ", Libelle = "Petit-déjeuner",
                PrixUnitaire = 5m, CodeDevise = "USD", PricingUnit = HotelExtraPricingUnit.PerNight
            }, societe);

            var cash = await ConfirmOneAsync(db, societe, hotel, roomType, from, to);
            var (reservations, _) = Services(db);
            var nuits = cash.Reservation.NombreNuits;

            var updated = await reservations.SetExtrasAsync(
                cash.Reservation.IdHotelReservation, societe,
                new HotelSetReservationExtrasRequestDto
                {
                    Items = new() { new() { IdHotelExtra = extra.IdHotelExtra, Quantity = 1 } }
                });

            Assert.Equal(5m * nuits, updated.MontantExtras);
            Assert.Equal(5m * nuits, updated.Extras[0].MontantLigne);
        }

        [Fact]
        public async Task Set_extras_rejects_hold()
        {
            await using var db = Db(nameof(Set_extras_rejects_hold));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "Hold");
            var extra = await HotelTestFactories.CreateExtraService(db).CreateAsync(new()
            {
                IdHotel = hotel, Code = "SPA", Libelle = "Spa",
                PrixUnitaire = 50m, PricingUnit = HotelExtraPricingUnit.PerStay
            }, societe);

            var holdFactory = new HotelInventoryHoldStrategyFactory(
                new HotelGlobalQuotaHoldStrategy(db), new HotelClassQuotaHoldStrategy(db));
            var hold = new HotelHoldService(db, holdFactory, NullLogger<HotelHoldService>.Instance);
            var holdResult = await hold.CreateHoldAsync(hotel, societe, new HotelHoldRequestDto
            {
                CheckInDate = from, CheckOutDate = to,
                Items = new() { new() { RoomTypeId = roomType, Quantity = 1 } }
            });

            var (reservations, _) = Services(db);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                reservations.SetExtrasAsync(holdResult.IdHotelReservation, societe,
                    new HotelSetReservationExtrasRequestDto
                    {
                        Items = new() { new() { IdHotelExtra = extra.IdHotelExtra, Quantity = 1 } }
                    }));
        }

        [Fact]
        public async Task Cancel_clears_extras()
        {
            await using var db = Db(nameof(Cancel_clears_extras));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "Cancel");
            var extra = await HotelTestFactories.CreateExtraService(db).CreateAsync(new()
            {
                IdHotel = hotel, Code = "LATE", Libelle = "Late checkout",
                PrixUnitaire = 30m, PricingUnit = HotelExtraPricingUnit.PerStay
            }, societe);

            var cash = await ConfirmOneAsync(db, societe, hotel, roomType, from, to);
            var (reservations, _) = Services(db);
            await reservations.SetExtrasAsync(cash.Reservation.IdHotelReservation, societe,
                new HotelSetReservationExtrasRequestDto
                {
                    Items = new() { new() { IdHotelExtra = extra.IdHotelExtra, Quantity = 1 } }
                });

            await reservations.CancelAsync(cash.Reservation.IdHotelReservation, societe);
            Assert.False(await db.HotelReservationExtras.AnyAsync());
        }

        [Fact]
        public async Task Set_extras_replace_all_clears_previous_lines()
        {
            await using var db = Db(nameof(Set_extras_replace_all_clears_previous_lines));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "Replace");
            var extras = HotelTestFactories.CreateExtraService(db);
            var a = await extras.CreateAsync(new()
            {
                IdHotel = hotel, Code = "A", Libelle = "Extra A",
                PrixUnitaire = 10m, PricingUnit = HotelExtraPricingUnit.PerStay
            }, societe);
            var b = await extras.CreateAsync(new()
            {
                IdHotel = hotel, Code = "B", Libelle = "Extra B",
                PrixUnitaire = 20m, PricingUnit = HotelExtraPricingUnit.PerStay
            }, societe);

            var cash = await ConfirmOneAsync(db, societe, hotel, roomType, from, to);
            var (reservations, _) = Services(db);
            await reservations.SetExtrasAsync(cash.Reservation.IdHotelReservation, societe,
                new HotelSetReservationExtrasRequestDto
                {
                    Items = new() { new() { IdHotelExtra = a.IdHotelExtra, Quantity = 1 } }
                });

            var replaced = await reservations.SetExtrasAsync(cash.Reservation.IdHotelReservation, societe,
                new HotelSetReservationExtrasRequestDto
                {
                    Items = new() { new() { IdHotelExtra = b.IdHotelExtra, Quantity = 2 } }
                });

            Assert.Single(replaced.Extras);
            Assert.Equal(b.IdHotelExtra, replaced.Extras[0].IdHotelExtra);
            Assert.Equal(40m, replaced.MontantExtras);
        }
    }
}
