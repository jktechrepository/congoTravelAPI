using System.Text.Json;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Hotel.Strategies;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class HotelPhase4FlexPayTests
    {
        private static CongoTravelDbContext Db(string name) => new(
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

        [Fact]
        public void PlanA_references_use_hotel_command_prefixes()
        {
            var id = Guid.NewGuid();
            Assert.StartsWith("HC", HotelFlexPayReferenceHelper.BuildMerchantReferenceForCommande(id));
            Assert.StartsWith("PENDING-HC-", HotelFlexPayReferenceHelper.BuildPendingOrderNumberForCommande(id));
        }

        [Fact]
        public async Task Callback_success_materializes_confirmed_reservation_and_sold_nights()
        {
            await using var db = Db(nameof(Callback_success_materializes_confirmed_reservation_and_sold_nights));
            var pending = await SeedPendingAsync(db, "ok");
            var notifier = new Mock<IFlexPayRealtimeNotifier>();
            var service = new HotelFlexPayCallbackService(db, Mock.Of<IFlexPayService>(),
                CommandService(db), notifier.Object, NullLogger<HotelFlexPayCallbackService>.Instance);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0", OrderNumber = pending.order, Amount = "50", Currency = "USD"
            });

            Assert.True(result.Success);
            Assert.Equal(HotelReservationStatus.CONFIRMED,
                await db.HotelReservations.Select(r => r.Status).SingleAsync());
            Assert.Equal(HotelPaymentStatus.SUCCEEDED,
                await db.HotelPayments.Select(p => p.Status).SingleAsync());
            Assert.Empty(db.HotelCommandesEnAttente);
            Assert.All(db.HotelNightAllotments, a =>
            {
                Assert.Equal(0, a.QuantiteHold);
                Assert.Equal(1, a.QuantiteVendue);
            });
            notifier.Verify(n => n.NotifyPaymentConfirmedAsync(
                42, pending.order, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Callback_failure_releases_all_nights_and_keeps_failed_audit_payment()
        {
            await using var db = Db(nameof(Callback_failure_releases_all_nights_and_keeps_failed_audit_payment));
            var pending = await SeedPendingAsync(db, "fail");
            var notifier = new Mock<IFlexPayRealtimeNotifier>();
            var service = new HotelFlexPayCallbackService(db, Mock.Of<IFlexPayService>(),
                CommandService(db), notifier.Object, NullLogger<HotelFlexPayCallbackService>.Instance);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "1", OrderNumber = pending.order
            });

            Assert.True(result.Success);
            Assert.Empty(db.HotelReservations);
            Assert.Empty(db.HotelCommandesEnAttente);
            Assert.Equal(HotelPaymentStatus.FAILED,
                await db.HotelPayments.Select(p => p.Status).SingleAsync());
            Assert.All(db.HotelNightAllotments, a => Assert.Equal(0, a.QuantiteHold));
            notifier.Verify(n => n.NotifyPaymentFailedAsync(
                42, pending.order, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Expiration_runner_fails_planA_command_releases_stock_and_notifies()
        {
            await using var db = Db(nameof(Expiration_runner_fails_planA_command_releases_stock_and_notifies));
            var pending = await SeedPendingAsync(db, "expire");
            var command = await db.HotelCommandesEnAttente.SingleAsync();
            command.DateExpiration = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
            var notifier = new Mock<IFlexPayRealtimeNotifier>();
            var services = new ServiceCollection()
                .AddSingleton<IHotelCommandeFlexPayService>(CommandService(db))
                .AddSingleton(notifier.Object)
                .BuildServiceProvider();

            await new HotelHoldExpirationRunner(
                NullLogger<HotelHoldExpirationRunner>.Instance, services).ExpireHoldsAsync(db);

            Assert.Empty(db.HotelCommandesEnAttente);
            Assert.Equal(HotelPaymentStatus.FAILED,
                await db.HotelPayments.Select(p => p.Status).SingleAsync());
            Assert.All(db.HotelNightAllotments, a => Assert.Equal(0, a.QuantiteHold));
            notifier.Verify(n => n.NotifyPaymentFailedAsync(
                42, pending.order, HotelFlexPayCallbackService.HoldExpiredMessage,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        private static HotelCommandeFlexPayService CommandService(CongoTravelDbContext db) =>
            new(db,
                new HotelInventoryHoldStrategyFactory(
                    new HotelGlobalQuotaHoldStrategy(db), new HotelClassQuotaHoldStrategy(db)),
                new HotelInventoryCancelStrategyFactory(
                    new HotelGlobalQuotaCancelStrategy(db), new HotelClassQuotaCancelStrategy(db)),
                new HotelReservationConfirmationService(
                    new HotelInventoryConfirmStrategyFactory(
                        new HotelGlobalQuotaConfirmStrategy(db), new HotelClassQuotaConfirmStrategy(db))),
                Mock.Of<IConfigSocieteRepository>(), Mock.Of<IFlexPayService>(),
                new HttpContextAccessor(), Options.Create(new FlexPayOptions { Enabled = true }),
                Mock.Of<IInfoPaiementResolutionService>(), Mock.Of<IDeviseMontantConverter>(),
                NullLogger<HotelCommandeFlexPayService>.Instance);

        private static async Task<(string order, int company)> SeedPendingAsync(
            CongoTravelDbContext db, string suffix)
        {
            var (company, site) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(db, "H4 " + suffix);
            var hotels = HotelTestFactories.CreateEtablissementService(db);
            var hotel = await hotels.PublishAsync((await hotels.CreateDraftAsync(new()
            {
                CodeHotel = "H4-" + suffix, Nom = "Hotel " + suffix, IdSite = site,
                AcomptePourcentDefaut = 25m
            }, company)).IdHotel, company);
            var rooms = HotelTestFactories.CreateRoomTypeService(db);
            var room = await rooms.PublishAsync((await rooms.CreateDraftAsync(new()
            {
                IdHotel = hotel.IdHotel, Code = "STD", Libelle = "Standard",
                PrixNuitReference = 100m, CodeDevise = "USD"
            }, company)).IdHotelRoomType, company);
            var from = new DateTime(2026, 12, 20);
            var to = from.AddDays(2);
            var allotments = HotelTestFactories.CreateAllotmentService(db);
            var batch = await allotments.CreateDraftBatchAsync(new()
            {
                IdHotel = hotel.IdHotel, IdHotelRoomType = room.IdHotelRoomType,
                From = from, To = to, CapaciteTotale = 2, PrixNuit = 100m, CodeDevise = "USD"
            }, company);
            foreach (var row in batch.Created) await allotments.PublishAsync(row.IdHotelNightAllotment, company);
            var held = await new HotelClassQuotaHoldStrategy(db).ReserveHoldAsync(
                hotel.IdHotel, company, from, to,
                new[] { new HotelHoldItemRequestDto { RoomTypeId = room.IdHotelRoomType, Quantity = 1 } });
            var command = new HotelCommandeEnAttente
            {
                IdSociete = company, IdHotel = hotel.IdHotel, IdSite = site, IdUtilisateur = 42,
                MethodePaiement = "MOBILE_MONEY", MontantTarif = 50m, CodeDeviseTarif = "USD",
                MontantFlexPay = 50m, CodeDevisePaiement = "USD",
                DateExpiration = DateTime.UtcNow.AddMinutes(15),
                PayloadMetierJson = JsonSerializer.Serialize(new HotelCommandeSnapshotDto
                {
                    Request = new HotelReservationWithPaiementRequestDto
                    {
                        IdHotel = hotel.IdHotel, CheckInDate = from, CheckOutDate = to,
                        Items = new() { new() { RoomTypeId = room.IdHotelRoomType, Quantity = 1 } },
                        Paiement = new() { MethodePaiement = "MOBILE_MONEY", Phone = "243900000001", IdSite = site }
                    },
                    ReferenceReservation = "HTL-H4-" + suffix,
                    MontantSejour = held.MontantSejour, MontantSousTotal = 50m, CodeDevise = "USD",
                    Lines = held.Lines.Select(l => new HotelCommandeSnapshotLineDto
                    {
                        LineType = l.LineType.ToString(),
                        IdHotelRoomType = l.IdHotelRoomType,
                        IdHotelNight = l.IdHotelNight,
                        Quantity = l.Quantity,
                        PrixSejourUnitaire = l.PrixSejourUnitaire, MontantLigne = l.MontantLigne,
                        CodeDevise = l.CodeDevise
                    }).ToList(),
                    InventoryMode = HotelInventoryMode.ClassQuota
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
            db.HotelCommandesEnAttente.Add(command);
            await db.SaveChangesAsync();
            var order = "FP-HOTEL-" + suffix;
            var payment = new HotelPayment
            {
                IdHotelCommandeEnAttente = command.IdHotelCommandeEnAttente, IdSite = site,
                ReferencePaiement = "PAY-H4-" + suffix, Provider = HotelFlexPayConstants.Provider,
                ProviderTxRef = order, Status = HotelPaymentStatus.PENDING,
                Montant = 50m, CodeDevise = "USD", MontantTarif = 50m, CodeDeviseTarif = "USD"
            };
            db.HotelPayments.Add(payment);
            await db.SaveChangesAsync();
            command.IdPaiementEnAttente = payment.IdHotelPayment;
            command.OrderNumberFlexPay = order;
            await db.SaveChangesAsync();
            return (order, company);
        }
    }
}
