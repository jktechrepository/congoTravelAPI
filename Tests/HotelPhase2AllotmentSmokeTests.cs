using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.PhotoStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CongoTravel.Tests
{
    public class HotelPhase2AllotmentSmokeTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static async Task<(int idSociete, int idHotel, int idRoomType)> SeedPublishedHotelAsync(
            CongoTravelDbContext context,
            string suffix)
        {
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(
                context, $"Hotel P2 {suffix}");
            var hotels = HotelTestFactories.CreateEtablissementService(context);
            var hotel = await hotels.PublishAsync((await hotels.CreateDraftAsync(new()
            {
                CodeHotel = $"HOT-{suffix}", Nom = $"Hôtel {suffix}", IdSite = idSite
            }, idSociete)).IdHotel, idSociete);
            var roomTypes = HotelTestFactories.CreateRoomTypeService(context);
            var room = await roomTypes.PublishAsync((await roomTypes.CreateDraftAsync(new()
            {
                IdHotel = hotel.IdHotel, Code = "STD", Libelle = "Standard",
                PrixNuitReference = 50m, CodeDevise = "USD"
            }, idSociete)).IdHotelRoomType, idSociete);
            return (idSociete, hotel.IdHotel, room.IdHotelRoomType);
        }

        [Fact]
        public void AddHotelReservations_registers_phase2_services()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<CongoTravelDbContext>(o =>
                o.UseInMemoryDatabase(nameof(AddHotelReservations_registers_phase2_services)));
            var store = PhotoStorageTestFactory.CreateBlobStoreMock().Object;
            services.AddSingleton<ICongoTravelPhotoBlobStore>(store);
            services.AddSingleton<IPhotoBinaryHydrator>(PhotoStorageTestFactory.CreateHydrator(store));
            services.AddHotelReservations();
            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHotelAllotmentService>());
            Assert.NotNull(provider.GetService<IHotelAvailabilityService>());
        }

        [Fact]
        public async Task CreateDraft_and_publish_allotment()
        {
            await using var context = BuildDb(nameof(CreateDraft_and_publish_allotment));
            var (idSociete, idHotel, idRoomType) = await SeedPublishedHotelAsync(context, "A1");
            var allotments = HotelTestFactories.CreateAllotmentService(context);
            var draft = await allotments.CreateDraftAsync(new HotelCreateAllotmentRequestDto
            {
                IdHotel = idHotel,
                IdHotelRoomType = idRoomType,
                NightDate = new DateTime(2026, 9, 10),
                CapaciteTotale = 5,
                PrixNuit = 80m,
                CodeDevise = "USD"
            }, idSociete);
            Assert.Equal("Draft", draft.Status);
            Assert.Equal(5, draft.QuantiteDisponible);
            var published = await allotments.PublishAsync(draft.IdHotelNightAllotment, idSociete);
            Assert.Equal("Published", published.Status);
        }

        [Fact]
        public async Task Publish_allotment_refuses_draft_hotel_or_room_type()
        {
            await using var context = BuildDb(nameof(Publish_allotment_refuses_draft_hotel_or_room_type));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(
                context, "Hotel P2 Refuse");
            var hotels = HotelTestFactories.CreateEtablissementService(context);
            var hotelDraft = await hotels.CreateDraftAsync(new()
            {
                CodeHotel = "HOT-REF", Nom = "Draft Hotel", IdSite = idSite
            }, idSociete);
            var roomTypes = HotelTestFactories.CreateRoomTypeService(context);
            var roomDraft = await roomTypes.CreateDraftAsync(new()
            {
                IdHotel = hotelDraft.IdHotel, Code = "STD", Libelle = "Standard"
            }, idSociete);
            var allotments = HotelTestFactories.CreateAllotmentService(context);
            var allotment = await allotments.CreateDraftAsync(new()
            {
                IdHotel = hotelDraft.IdHotel,
                IdHotelRoomType = roomDraft.IdHotelRoomType,
                NightDate = new DateTime(2026, 9, 11),
                CapaciteTotale = 3,
                PrixNuit = 40m
            }, idSociete);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                allotments.PublishAsync(allotment.IdHotelNightAllotment, idSociete));
            Assert.Contains("hôtel parent", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Batch_creates_nights_and_skips_existing()
        {
            await using var context = BuildDb(nameof(Batch_creates_nights_and_skips_existing));
            var (idSociete, idHotel, idRoomType) = await SeedPublishedHotelAsync(context, "B1");
            var allotments = HotelTestFactories.CreateAllotmentService(context);
            var from = new DateTime(2026, 10, 1);
            var to = new DateTime(2026, 10, 4);
            var first = await allotments.CreateDraftBatchAsync(new HotelCreateAllotmentBatchRequestDto
            {
                IdHotel = idHotel,
                IdHotelRoomType = idRoomType,
                From = from,
                To = to,
                CapaciteTotale = 4,
                PrixNuit = 60m,
                CodeDevise = "CDF",
                SkipExisting = true
            }, idSociete);
            Assert.Equal(3, first.CreatedCount);
            Assert.Equal(0, first.SkippedCount);

            var second = await allotments.CreateDraftBatchAsync(new HotelCreateAllotmentBatchRequestDto
            {
                IdHotel = idHotel,
                IdHotelRoomType = idRoomType,
                From = from,
                To = to,
                CapaciteTotale = 4,
                PrixNuit = 60m,
                SkipExisting = true
            }, idSociete);
            Assert.Equal(0, second.CreatedCount);
            Assert.Equal(3, second.SkippedCount);
        }

        [Fact]
        public async Task Availability_returns_capacity_and_minDisponible()
        {
            await using var context = BuildDb(nameof(Availability_returns_capacity_and_minDisponible));
            var (idSociete, idHotel, idRoomType) = await SeedPublishedHotelAsync(context, "C1");
            var allotments = HotelTestFactories.CreateAllotmentService(context);
            var from = new DateTime(2026, 11, 1);
            var to = new DateTime(2026, 11, 4);
            var batch = await allotments.CreateDraftBatchAsync(new HotelCreateAllotmentBatchRequestDto
            {
                IdHotel = idHotel,
                IdHotelRoomType = idRoomType,
                From = from,
                To = to,
                CapaciteTotale = 10,
                PrixNuit = 100m,
                CodeDevise = "USD"
            }, idSociete);
            foreach (var row in batch.Created)
                await allotments.PublishAsync(row.IdHotelNightAllotment, idSociete);

            // Reduce capacity on middle night after publish
            var middle = batch.Created.OrderBy(c => c.NightDate).Skip(1).First();
            await allotments.UpdateAsync(middle.IdHotelNightAllotment, new HotelUpdateAllotmentRequestDto
            {
                CapaciteTotale = 2,
                PrixNuit = 100m,
                CodeDevise = "USD"
            }, idSociete);

            var availability = await HotelTestFactories.CreateAvailabilityService(context)
                .GetAvailabilityAsync(idHotel, from, to, idRoomType, idSociete, publishedOnly: true);
            Assert.Equal(3, availability.Nights.Count);
            Assert.Equal(2, availability.MinDisponible);
            Assert.All(availability.Nights, n => Assert.True(n.QuantiteDisponible <= 10));
            Assert.Contains(availability.Nights, n => n.QuantiteDisponible == 2);
        }
    }
}
