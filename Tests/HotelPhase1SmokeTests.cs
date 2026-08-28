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
    public class HotelPhase1SmokeTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public void AddHotelReservations_registers_services()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<CongoTravelDbContext>(o => o.UseInMemoryDatabase(nameof(AddHotelReservations_registers_services)));
            var store = PhotoStorageTestFactory.CreateBlobStoreMock().Object;
            services.AddSingleton<ICongoTravelPhotoBlobStore>(store);
            services.AddSingleton<IPhotoBinaryHydrator>(
                PhotoStorageTestFactory.CreateHydrator(store));
            services.AddHotelReservations();
            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHotelEtablissementService>());
            Assert.NotNull(provider.GetService<IHotelRoomTypeService>());
            Assert.NotNull(provider.GetService<IHotelPhotoService>());
        }

        [Fact]
        public async Task CreateDraft_and_publish_hotel()
        {
            await using var context = BuildDb(nameof(CreateDraft_and_publish_hotel));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(context, "Hotel Smoke");
            var service = HotelTestFactories.CreateEtablissementService(context);
            var draft = await service.CreateDraftAsync(new()
            {
                CodeHotel = "HOT-SMOKE", Nom = "Hôtel Fleuve", IdSite = idSite, AcomptePourcentDefaut = 20m
            }, idSociete);
            Assert.Equal("Draft", draft.Status);
            var published = await service.PublishAsync(draft.IdHotel, idSociete);
            Assert.Equal("Published", published.Status);
        }

        [Fact]
        public async Task CreateDraft_and_publish_room_type()
        {
            await using var context = BuildDb(nameof(CreateDraft_and_publish_room_type));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(context, "Hotel Room");
            var hotels = HotelTestFactories.CreateEtablissementService(context);
            var hotel = await hotels.PublishAsync((await hotels.CreateDraftAsync(new()
            {
                CodeHotel = "HOT-ROOM", Nom = "Hôtel Room", IdSite = idSite
            }, idSociete)).IdHotel, idSociete);
            var roomTypes = HotelTestFactories.CreateRoomTypeService(context);
            var draft = await roomTypes.CreateDraftAsync(new()
            {
                IdHotel = hotel.IdHotel, Code = "DOUBLE", Libelle = "Chambre double",
                CapacitePersonnesMax = 2, PrixNuitReference = 100m, CodeDevise = "USD"
            }, idSociete);
            Assert.Equal("Draft", draft.Status);
            Assert.Equal("Published", (await roomTypes.PublishAsync(draft.IdHotelRoomType, idSociete)).Status);
        }

        [Fact]
        public async Task Publish_room_type_refuses_draft_hotel()
        {
            await using var context = BuildDb(nameof(Publish_room_type_refuses_draft_hotel));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(context, "Hotel Refuse");
            var hotel = await HotelTestFactories.CreateEtablissementService(context).CreateDraftAsync(new()
            {
                CodeHotel = "HOT-DRAFT", Nom = "Hôtel Draft", IdSite = idSite
            }, idSociete);
            var roomTypes = HotelTestFactories.CreateRoomTypeService(context);
            var room = await roomTypes.CreateDraftAsync(new()
            {
                IdHotel = hotel.IdHotel, Code = "SUITE", Libelle = "Suite"
            }, idSociete);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                roomTypes.PublishAsync(room.IdHotelRoomType, idSociete));
            Assert.Contains("parent", exception.Message);
        }
    }
}
