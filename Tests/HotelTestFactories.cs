using CongoTravel.Data;
using CongoTravel.Services.Hotel;
using Microsoft.Extensions.Logging.Abstractions;

namespace CongoTravel.Tests
{
    internal static class HotelTestFactories
    {
        public static HotelPhotoService CreatePhotoService(CongoTravelDbContext context)
        {
            var store = PhotoStorageTestFactory.CreateBlobStoreMock().Object;
            return new HotelPhotoService(context, store, PhotoStorageTestFactory.CreateHydrator(store),
                NullLogger<HotelPhotoService>.Instance);
        }

        public static HotelEtablissementService CreateEtablissementService(CongoTravelDbContext context)
        {
            var store = PhotoStorageTestFactory.CreateBlobStoreMock().Object;
            var photos = new HotelPhotoService(context, store, PhotoStorageTestFactory.CreateHydrator(store),
                NullLogger<HotelPhotoService>.Instance);
            return new HotelEtablissementService(context, photos, PhotoStorageTestFactory.CreateHydrator(store),
                NullLogger<HotelEtablissementService>.Instance);
        }

        public static HotelRoomTypeService CreateRoomTypeService(CongoTravelDbContext context) =>
            new(context, NullLogger<HotelRoomTypeService>.Instance);

        public static HotelRoomService CreateRoomService(CongoTravelDbContext context) =>
            new(context, NullLogger<HotelRoomService>.Instance);

        public static HotelExtraService CreateExtraService(CongoTravelDbContext context) =>
            new(context, NullLogger<HotelExtraService>.Instance);

        public static HotelAllotmentService CreateAllotmentService(CongoTravelDbContext context) =>
            new(context, NullLogger<HotelAllotmentService>.Instance);

        public static HotelNightService CreateNightService(CongoTravelDbContext context) =>
            new(context, NullLogger<HotelNightService>.Instance);

        public static HotelAvailabilityService CreateAvailabilityService(CongoTravelDbContext context) =>
            new(context);
    }
}
