using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Hotel.Strategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CongoTravel.Tests
{
    public class HotelPhase3CashSmokeTests
    {
        private static CongoTravelDbContext Db(string name) => new(
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

        private static async Task<(int societe, int hotel, int room, DateTime from, DateTime to)> SeedAsync(
            CongoTravelDbContext db, string suffix, int capacity = 2)
        {
            var (societe, site) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(db, "H3 " + suffix);
            var hotels = HotelTestFactories.CreateEtablissementService(db);
            var hotel = await hotels.PublishAsync((await hotels.CreateDraftAsync(new()
            {
                CodeHotel = "H3-" + suffix, Nom = "Hotel " + suffix, IdSite = site,
                AcomptePourcentDefaut = 25m
            }, societe)).IdHotel, societe);
            var rooms = HotelTestFactories.CreateRoomTypeService(db);
            var room = await rooms.PublishAsync((await rooms.CreateDraftAsync(new()
            {
                IdHotel = hotel.IdHotel, Code = "STD", Libelle = "Standard",
                PrixNuitReference = 100m, CodeDevise = "USD"
            }, societe)).IdHotelRoomType, societe);
            var from = new DateTime(2026, 12, 10);
            var to = from.AddDays(2);
            var allotments = HotelTestFactories.CreateAllotmentService(db);
            var batch = await allotments.CreateDraftBatchAsync(new()
            {
                IdHotel = hotel.IdHotel, IdHotelRoomType = room.IdHotelRoomType,
                From = from, To = to, CapaciteTotale = capacity, PrixNuit = 100m,
                CodeDevise = "USD"
            }, societe);
            foreach (var row in batch.Created)
                await allotments.PublishAsync(row.IdHotelNightAllotment, societe);
            return (societe, hotel.IdHotel, room.IdHotelRoomType, from, to);
        }

        private static (HotelHoldService holds, HotelPaymentService payments,
            HotelReservationService reservations, HotelReservationWithPaiementService facade) Services(
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
            return (hold, payment, reservations,
                new HotelReservationWithPaiementService(db, hold, payment, reservations));
        }

        [Fact]
        public void DI_registers_phase3_services()
        {
            var services = new ServiceCollection().AddLogging();
            services.AddDbContext<CongoTravelDbContext>(o => o.UseInMemoryDatabase(nameof(DI_registers_phase3_services)));
            services.AddHotelReservations();
            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHotelHoldService>());
            Assert.NotNull(provider.GetService<IHotelPaymentService>());
            Assert.NotNull(provider.GetService<IHotelReservationService>());
            Assert.NotNull(provider.GetService<IHotelReservationWithPaiementService>());
            Assert.NotNull(provider.GetService<IHotelHoldExpirationRunner>());
            Assert.NotNull(provider.GetService<IHotelInventoryHoldStrategyFactory>());
            Assert.NotNull(provider.GetService<IHotelInventoryConfirmStrategyFactory>());
            Assert.NotNull(provider.GetService<IHotelInventoryCancelStrategyFactory>());
            Assert.IsType<HotelClassQuotaHoldStrategy>(
                provider.GetRequiredService<IHotelInventoryHoldStrategyFactory>()
                    .GetStrategy(HotelInventoryMode.ClassQuota));
            Assert.IsType<HotelGlobalQuotaHoldStrategy>(
                provider.GetRequiredService<IHotelInventoryHoldStrategyFactory>()
                    .GetStrategy(HotelInventoryMode.GlobalQuota));
        }

        [Fact]
        public async Task Cash_hold_confirm_moves_hold_to_sold()
        {
            await using var db = Db(nameof(Cash_hold_confirm_moves_hold_to_sold));
            var seed = await SeedAsync(db, "cash");
            var result = await Services(db).facade.CreateCashAsync(new()
            {
                IdHotel = seed.hotel, CheckInDate = seed.from, CheckOutDate = seed.to,
                Items = new() { new() { RoomTypeId = seed.room, Quantity = 1 } },
                Paiement = new() { MethodePaiement = "CASH" }
            });
            Assert.Equal("CONFIRMED", result.Reservation.Status);
            Assert.All(db.HotelNightAllotments, a => { Assert.Equal(0, a.QuantiteHold); Assert.Equal(1, a.QuantiteVendue); });
            Assert.Equal(50m, result.Payment.Montant);
        }

        [Fact]
        public async Task Oversell_rolls_back_without_reservation()
        {
            await using var db = Db(nameof(Oversell_rolls_back_without_reservation));
            var seed = await SeedAsync(db, "over", 1);
            var ex = await Assert.ThrowsAsync<Models.Hotel.HotelHoldConflictException>(() =>
                Services(db).facade.CreateCashAsync(new()
                {
                    IdHotel = seed.hotel, CheckInDate = seed.from, CheckOutDate = seed.to,
                    Items = new() { new() { RoomTypeId = seed.room, Quantity = 2 } },
                    Paiement = new() { MethodePaiement = "CASH" }
                }));
            Assert.Contains("Capacité", ex.Message);
            Assert.Empty(db.HotelReservations);
            Assert.All(db.HotelNightAllotments, a => Assert.Equal(0, a.QuantiteHold));
        }

        [Fact]
        public async Task Cancel_confirmed_restores_sold()
        {
            await using var db = Db(nameof(Cancel_confirmed_restores_sold));
            var seed = await SeedAsync(db, "cancel");
            var services = Services(db);
            var result = await services.facade.CreateCashAsync(new()
            {
                IdHotel = seed.hotel, CheckInDate = seed.from, CheckOutDate = seed.to,
                Items = new() { new() { RoomTypeId = seed.room, Quantity = 1 } },
                Paiement = new() { MethodePaiement = "CASH" }
            });
            await services.reservations.CancelAsync(result.Reservation.IdHotelReservation, seed.societe);
            Assert.All(db.HotelNightAllotments, a => Assert.Equal(0, a.QuantiteVendue));
        }

        [Fact]
        public async Task Expire_hold_restores_hold_stock()
        {
            await using var db = Db(nameof(Expire_hold_restores_hold_stock));
            var seed = await SeedAsync(db, "expire");
            var hold = await Services(db).holds.CreateHoldAsync(seed.hotel, seed.societe, new()
            {
                CheckInDate = seed.from, CheckOutDate = seed.to,
                Items = new() { new() { RoomTypeId = seed.room, Quantity = 1 } }
            });
            var reservation = await db.HotelReservations.FindAsync(hold.IdHotelReservation);
            reservation!.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
            await new HotelHoldExpirationRunner(NullLogger<HotelHoldExpirationRunner>.Instance)
                .ExpireHoldsAsync(db);
            Assert.Equal(HotelReservationStatus.EXPIRED, reservation.Status);
            Assert.All(db.HotelNightAllotments, a => Assert.Equal(0, a.QuantiteHold));
        }
    }
}
