using CongoTravel.Data;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.PhotoStorage;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.SiteTouristique;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CongoTravel.Tests
{
    /// <summary>Fabriques de services photo pour tests (blob store en mémoire, hydratation réelle).</summary>
    internal static class PhotoStorageTestFactory
    {
        public static Mock<ICongoTravelPhotoBlobStore> CreateBlobStoreMock()
        {
            var store = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            var mock = new Mock<ICongoTravelPhotoBlobStore>();

            mock.Setup(s => s.UploadAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((string entity, int parentId, int ordre, byte[] content, string ct, string? fn, CancellationToken _) =>
                {
                    var key = $"congotravel/photos/{entity}/{parentId}/{ordre}-{Guid.NewGuid():N}.jpg";
                    store[key] = content;
                    return key;
                });

            mock.Setup(s => s.GetBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string key, CancellationToken _) =>
                    store.TryGetValue(key, out var bytes)
                        ? bytes
                        : throw new FileNotFoundException(key));

            mock.Setup(s => s.TryDeleteAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string? key, CancellationToken _) =>
                {
                    if (string.IsNullOrWhiteSpace(key))
                        return false;
                    return store.Remove(key);
                });

            return mock;
        }

        public static IPhotoBinaryHydrator CreateHydrator(ICongoTravelPhotoBlobStore blobStore) =>
            new PhotoBinaryHydrator(blobStore, NullLogger<PhotoBinaryHydrator>.Instance);

        public static EvenementSessionPhotoService CreateEvenementPhotoService(
            CongoTravelDbContext ctx,
            ICongoTravelPhotoBlobStore? blobStore = null)
        {
            var store = blobStore ?? CreateBlobStoreMock().Object;
            return new EvenementSessionPhotoService(
                ctx,
                store,
                CreateHydrator(store),
                NullLogger<EvenementSessionPhotoService>.Instance);
        }

        public static EvenementSessionService CreateEvenementSessionService(
            CongoTravelDbContext ctx,
            ICongoTravelPhotoBlobStore? blobStore = null)
        {
            var store = blobStore ?? CreateBlobStoreMock().Object;
            var photos = CreateEvenementPhotoService(ctx, store);
            return new EvenementSessionService(
                ctx,
                photos,
                CreateHydrator(store),
                NullLogger<EvenementSessionService>.Instance);
        }

        public static RestaurantPhotoService CreateRestaurantPhotoService(
            CongoTravelDbContext ctx,
            ICongoTravelPhotoBlobStore? blobStore = null)
        {
            var store = blobStore ?? CreateBlobStoreMock().Object;
            return new RestaurantPhotoService(
                ctx,
                store,
                CreateHydrator(store),
                NullLogger<RestaurantPhotoService>.Instance);
        }

        public static RestaurantEtablissementService CreateRestaurantEtablissementService(
            CongoTravelDbContext ctx,
            ICongoTravelPhotoBlobStore? blobStore = null)
        {
            var store = blobStore ?? CreateBlobStoreMock().Object;
            return new RestaurantEtablissementService(
                ctx,
                CreateRestaurantPhotoService(ctx, store),
                CreateHydrator(store),
                NullLogger<RestaurantEtablissementService>.Instance);
        }

        public static SiteTouristiqueLieuPhotoService CreateSitePhotoService(
            CongoTravelDbContext ctx,
            ICongoTravelPhotoBlobStore? blobStore = null)
        {
            var store = blobStore ?? CreateBlobStoreMock().Object;
            return new SiteTouristiqueLieuPhotoService(
                ctx,
                store,
                CreateHydrator(store),
                NullLogger<SiteTouristiqueLieuPhotoService>.Instance);
        }

        public static SiteTouristiqueLieuService CreateSiteLieuService(
            CongoTravelDbContext ctx,
            ICongoTravelPhotoBlobStore? blobStore = null)
        {
            var store = blobStore ?? CreateBlobStoreMock().Object;
            return new SiteTouristiqueLieuService(
                ctx,
                CreateSitePhotoService(ctx, store),
                CreateHydrator(store),
                NullLogger<SiteTouristiqueLieuService>.Instance);
        }
    }
}
