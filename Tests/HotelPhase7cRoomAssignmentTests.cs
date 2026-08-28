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
    public class HotelPhase7cRoomAssignmentTests
    {
        private static CongoTravelDbContext Db(string name) => new(
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

        private static async Task<(int societe, int hotel, int roomType, DateTime from, DateTime to)> SeedClassAsync(
            CongoTravelDbContext db, string suffix, int capacity = 2)
        {
            var (societe, site) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(db, "H7c " + suffix);
            var hotels = HotelTestFactories.CreateEtablissementService(db);
            var hotel = await hotels.PublishAsync((await hotels.CreateDraftAsync(new()
            {
                CodeHotel = "H7c-" + suffix, Nom = "Hotel 7c " + suffix, IdSite = site,
                AcomptePourcentDefaut = 25m
            }, societe)).IdHotel, societe);
            var roomTypes = HotelTestFactories.CreateRoomTypeService(db);
            var roomType = await roomTypes.PublishAsync((await roomTypes.CreateDraftAsync(new()
            {
                IdHotel = hotel.IdHotel, Code = "STD", Libelle = "Standard",
                PrixNuitReference = 100m, CodeDevise = "USD"
            }, societe)).IdHotelRoomType, societe);
            var from = new DateTime(2026, 9, 10);
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

        [Fact]
        public void DI_registers_phase7c_room_service()
        {
            var services = new ServiceCollection().AddLogging();
            services.AddDbContext<CongoTravelDbContext>(o =>
                o.UseInMemoryDatabase(nameof(DI_registers_phase7c_room_service)));
            var store = PhotoStorageTestFactory.CreateBlobStoreMock().Object;
            services.AddSingleton<ICongoTravelPhotoBlobStore>(store);
            services.AddSingleton<IPhotoBinaryHydrator>(PhotoStorageTestFactory.CreateHydrator(store));
            services.AddHotelReservations();
            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHotelRoomService>());
        }

        [Fact]
        public async Task Create_room_rejects_duplicate_numero()
        {
            await using var db = Db(nameof(Create_room_rejects_duplicate_numero));
            var (societe, hotel, roomType, _, _) = await SeedClassAsync(db, "UQ", capacity: 1);
            var rooms = HotelTestFactories.CreateRoomService(db);
            await rooms.CreateAsync(new HotelCreateRoomRequestDto
            {
                IdHotel = hotel, IdHotelRoomType = roomType, Numero = "101"
            }, societe);

            await Assert.ThrowsAsync<HotelRoomConflictException>(() =>
                rooms.CreateAsync(new HotelCreateRoomRequestDto
                {
                    IdHotel = hotel, IdHotelRoomType = roomType, Numero = "101"
                }, societe));
        }

        [Fact]
        public async Task Assign_rooms_on_confirmed_ok()
        {
            await using var db = Db(nameof(Assign_rooms_on_confirmed_ok));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "AssignOk");
            var physical = await HotelTestFactories.CreateRoomService(db).CreateAsync(new()
            {
                IdHotel = hotel, IdHotelRoomType = roomType, Numero = "201", Etage = "2"
            }, societe);

            var (reservations, facade) = Services(db);
            var cash = await facade.CreateCashAsync(new HotelReservationWithPaiementRequestDto
            {
                IdHotel = hotel, CheckInDate = from, CheckOutDate = to,
                Items = new() { new() { RoomTypeId = roomType, Quantity = 1 } },
                Paiement = new() { MethodePaiement = "CASH" }
            });

            var lineId = cash.Reservation.Lines[0].IdHotelReservationLine;
            var assigned = await reservations.AssignRoomsAsync(
                cash.Reservation.IdHotelReservation, societe,
                new HotelAssignRoomsRequestDto
                {
                    Items = new()
                    {
                        new() { IdHotelReservationLine = lineId, IdHotelRoom = physical.IdHotelRoom }
                    }
                });

            Assert.Single(assigned.RoomAssignments);
            Assert.Equal(physical.IdHotelRoom, assigned.RoomAssignments[0].IdHotelRoom);
            Assert.Equal("201", assigned.RoomAssignments[0].Numero);
        }

        [Fact]
        public async Task Assign_rooms_rejects_hold()
        {
            await using var db = Db(nameof(Assign_rooms_rejects_hold));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "Hold");
            var physical = await HotelTestFactories.CreateRoomService(db).CreateAsync(new()
            {
                IdHotel = hotel, IdHotelRoomType = roomType, Numero = "301"
            }, societe);

            var holdFactory = new HotelInventoryHoldStrategyFactory(
                new HotelGlobalQuotaHoldStrategy(db), new HotelClassQuotaHoldStrategy(db));
            var hold = new HotelHoldService(db, holdFactory, NullLogger<HotelHoldService>.Instance);
            var holdResult = await hold.CreateHoldAsync(hotel, societe, new HotelHoldRequestDto
            {
                CheckInDate = from, CheckOutDate = to,
                Items = new() { new() { RoomTypeId = roomType, Quantity = 1 } }
            });

            var lineId = (await db.HotelReservationLines
                .Where(l => l.IdHotelReservation == holdResult.IdHotelReservation)
                .Select(l => l.IdHotelReservationLine)
                .FirstAsync());

            var reservations = Services(db).reservations;
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                reservations.AssignRoomsAsync(holdResult.IdHotelReservation, societe,
                    new HotelAssignRoomsRequestDto
                    {
                        Items = new()
                        {
                            new() { IdHotelReservationLine = lineId, IdHotelRoom = physical.IdHotelRoom }
                        }
                    }));
        }

        [Fact]
        public async Task Assign_rooms_rejects_overlap()
        {
            await using var db = Db(nameof(Assign_rooms_rejects_overlap));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "Overlap", capacity: 2);
            var physical = await HotelTestFactories.CreateRoomService(db).CreateAsync(new()
            {
                IdHotel = hotel, IdHotelRoomType = roomType, Numero = "401"
            }, societe);

            var (reservations, facade) = Services(db);
            var first = await facade.CreateCashAsync(new HotelReservationWithPaiementRequestDto
            {
                IdHotel = hotel, CheckInDate = from, CheckOutDate = to,
                Items = new() { new() { RoomTypeId = roomType, Quantity = 1 } },
                Paiement = new() { MethodePaiement = "CASH" }
            });
            await reservations.AssignRoomsAsync(first.Reservation.IdHotelReservation, societe,
                new HotelAssignRoomsRequestDto
                {
                    Items = new()
                    {
                        new()
                        {
                            IdHotelReservationLine = first.Reservation.Lines[0].IdHotelReservationLine,
                            IdHotelRoom = physical.IdHotelRoom
                        }
                    }
                });

            var second = await facade.CreateCashAsync(new HotelReservationWithPaiementRequestDto
            {
                IdHotel = hotel, CheckInDate = from, CheckOutDate = to,
                Items = new() { new() { RoomTypeId = roomType, Quantity = 1 } },
                Paiement = new() { MethodePaiement = "CASH" }
            });

            await Assert.ThrowsAsync<HotelRoomAssignmentConflictException>(() =>
                reservations.AssignRoomsAsync(second.Reservation.IdHotelReservation, societe,
                    new HotelAssignRoomsRequestDto
                    {
                        Items = new()
                        {
                            new()
                            {
                                IdHotelReservationLine = second.Reservation.Lines[0].IdHotelReservationLine,
                                IdHotelRoom = physical.IdHotelRoom
                            }
                        }
                    }));
        }

        [Fact]
        public async Task Cancel_clears_room_assignments()
        {
            await using var db = Db(nameof(Cancel_clears_room_assignments));
            var (societe, hotel, roomType, from, to) = await SeedClassAsync(db, "Cancel");
            var physical = await HotelTestFactories.CreateRoomService(db).CreateAsync(new()
            {
                IdHotel = hotel, IdHotelRoomType = roomType, Numero = "501"
            }, societe);

            var (reservations, facade) = Services(db);
            var cash = await facade.CreateCashAsync(new HotelReservationWithPaiementRequestDto
            {
                IdHotel = hotel, CheckInDate = from, CheckOutDate = to,
                Items = new() { new() { RoomTypeId = roomType, Quantity = 1 } },
                Paiement = new() { MethodePaiement = "CASH" }
            });
            await reservations.AssignRoomsAsync(cash.Reservation.IdHotelReservation, societe,
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

            Assert.Equal(1, await db.HotelRoomAssignments.CountAsync());
            await reservations.CancelAsync(cash.Reservation.IdHotelReservation, societe);
            Assert.Equal(0, await db.HotelRoomAssignments.CountAsync());
        }
    }
}
