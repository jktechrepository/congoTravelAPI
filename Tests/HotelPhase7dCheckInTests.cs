using CongoTravel.Data;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Hotel.Strategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CongoTravel.Tests
{
    public class HotelPhase7dCheckInTests
    {
        private static CongoTravelDbContext Db(string name) => new(
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

        private static async Task<(int societe, int hotel, int roomType, DateTime from, DateTime to)> SeedClassAsync(
            CongoTravelDbContext db, string suffix, int capacity = 2)
        {
            var (societe, site) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(db, "H7d " + suffix);
            var hotels = HotelTestFactories.CreateEtablissementService(db);
            var hotel = await hotels.PublishAsync((await hotels.CreateDraftAsync(new()
            {
                CodeHotel = "H7d-" + suffix, Nom = "Hotel 7d " + suffix, IdSite = site,
                AcomptePourcentDefaut = 25m
            }, societe)).IdHotel, societe);
            var roomTypes = HotelTestFactories.CreateRoomTypeService(db);
            var roomType = await roomTypes.PublishAsync((await roomTypes.CreateDraftAsync(new()
            {
                IdHotel = hotel.IdHotel, Code = "STD", Libelle = "Standard",
                PrixNuitReference = 100m, CodeDevise = "USD"
            }, societe)).IdHotelRoomType, societe);
            var from = new DateTime(2026, 10, 5);
            var to = from.AddDays(2);
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

        private static async Task<HotelReservationWithPaiementResponseDto> ConfirmCashAsync(
            CongoTravelDbContext db, int hotel, int roomType, DateTime from, DateTime to) =>
            await Services(db).facade.CreateCashAsync(new HotelReservationWithPaiementRequestDto
            {
                IdHotel = hotel, CheckInDate = from, CheckOutDate = to,
                Items = new() { new() { RoomTypeId = roomType, Quantity = 1 } },
                Paiement = new() { MethodePaiement = "CASH" }
            });

        [Fact]
        public async Task Check_in_on_confirmed_ok()
        {
            await using var db = Db(nameof(Check_in_on_confirmed_ok));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "CheckIn");
            var cash = await ConfirmCashAsync(db, hotel, roomType, from, to);
            var reservations = Services(db).reservations;

            var result = await reservations.CheckInAsync(cash.Reservation.IdHotelReservation, societe);
            Assert.NotNull(result.CheckedInAtUtc);
            Assert.Null(result.CheckedOutAtUtc);
            Assert.Equal("CONFIRMED", result.Status);
        }

        [Fact]
        public async Task Check_in_rejects_hold()
        {
            await using var db = Db(nameof(Check_in_rejects_hold));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "Hold");
            var holdFactory = new HotelInventoryHoldStrategyFactory(
                new HotelGlobalQuotaHoldStrategy(db), new HotelClassQuotaHoldStrategy(db));
            var hold = new HotelHoldService(db, holdFactory, NullLogger<HotelHoldService>.Instance);
            var holdResult = await hold.CreateHoldAsync(hotel, societe, new HotelHoldRequestDto
            {
                CheckInDate = from, CheckOutDate = to,
                Items = new() { new() { RoomTypeId = roomType, Quantity = 1 } }
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Services(db).reservations.CheckInAsync(holdResult.IdHotelReservation, societe));
        }

        [Fact]
        public async Task Check_out_after_check_in_ok()
        {
            await using var db = Db(nameof(Check_out_after_check_in_ok));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "CheckOut");
            var cash = await ConfirmCashAsync(db, hotel, roomType, from, to);
            var reservations = Services(db).reservations;
            var id = cash.Reservation.IdHotelReservation;

            await reservations.CheckInAsync(id, societe);
            var result = await reservations.CheckOutAsync(id, societe);
            Assert.NotNull(result.CheckedInAtUtc);
            Assert.NotNull(result.CheckedOutAtUtc);
        }

        [Fact]
        public async Task Check_out_rejects_without_check_in()
        {
            await using var db = Db(nameof(Check_out_rejects_without_check_in));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "NoCheckIn");
            var cash = await ConfirmCashAsync(db, hotel, roomType, from, to);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Services(db).reservations.CheckOutAsync(cash.Reservation.IdHotelReservation, societe));
        }

        [Fact]
        public async Task Check_in_idempotent()
        {
            await using var db = Db(nameof(Check_in_idempotent));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "IdempotentIn");
            var cash = await ConfirmCashAsync(db, hotel, roomType, from, to);
            var reservations = Services(db).reservations;
            var id = cash.Reservation.IdHotelReservation;

            var first = await reservations.CheckInAsync(id, societe);
            var second = await reservations.CheckInAsync(id, societe);
            Assert.Equal(first.CheckedInAtUtc, second.CheckedInAtUtc);
        }

        [Fact]
        public async Task Cancel_clears_check_in_timestamps()
        {
            await using var db = Db(nameof(Cancel_clears_check_in_timestamps));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "Cancel");
            var cash = await ConfirmCashAsync(db, hotel, roomType, from, to);
            var reservations = Services(db).reservations;
            var id = cash.Reservation.IdHotelReservation;

            await reservations.CheckInAsync(id, societe);
            await reservations.CancelAsync(id, societe);

            var row = await db.HotelReservations.FindAsync(id);
            Assert.Null(row!.CheckedInAtUtc);
            Assert.Null(row.CheckedOutAtUtc);
        }

        [Fact]
        public async Task Assign_without_check_in_ok()
        {
            await using var db = Db(nameof(Assign_without_check_in_ok));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "AssignNoCheckIn");
            var physical = await HotelTestFactories.CreateRoomService(db).CreateAsync(new HotelCreateRoomRequestDto
            {
                IdHotel = hotel, IdHotelRoomType = roomType, Numero = "701"
            }, societe);
            var cash = await ConfirmCashAsync(db, hotel, roomType, from, to);
            var reservations = Services(db).reservations;

            var assigned = await reservations.AssignRoomsAsync(cash.Reservation.IdHotelReservation, societe,
                new HotelAssignRoomsRequestDto
                {
                    Items = new()
                    {
                        new()
                        {
                            IdHotelReservationLine = cash.Reservation.Lines[0].IdHotelReservationLine,
                            IdHotelRoom = physical.IdHotelRoom
                        }
                    }
                });

            Assert.Null(assigned.CheckedInAtUtc);
            Assert.Single(assigned.RoomAssignments);
        }
    }
}
