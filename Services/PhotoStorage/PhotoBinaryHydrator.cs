using CongoTravel.Models;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Services.PhotoStorage
{
    public interface IPhotoBinaryHydrator
    {
        Task HydrateAsync(
            IEnumerable<(string? StorageKey, byte[]? PhotoData, Action<byte[]> SetPhotoData)> items,
            CancellationToken cancellationToken = default);

        Task HydratePhotoVehiculesAsync(
            IEnumerable<PhotoVehicule>? photos,
            CancellationToken cancellationToken = default);

        Task HydrateVehiculesAsync(
            IEnumerable<Vehicule?>? vehicules,
            CancellationToken cancellationToken = default);

        Task HydrateVoyagesAsync(
            IEnumerable<Voyage>? voyages,
            CancellationToken cancellationToken = default);

        Task HydrateEvenementSessionPhotosAsync(
            IEnumerable<EvenementSessionPhoto>? photos,
            CancellationToken cancellationToken = default);

        Task HydrateEvenementSessionsAsync(
            IEnumerable<EvenementSession>? sessions,
            CancellationToken cancellationToken = default);

        Task HydrateRestaurantPhotosAsync(
            IEnumerable<RestaurantPhoto>? photos,
            CancellationToken cancellationToken = default);

        Task HydrateRestaurantsAsync(
            IEnumerable<CongoTravel.Models.Restaurant.Restaurant>? restaurants,
            CancellationToken cancellationToken = default);

        Task HydrateHotelPhotosAsync(
            IEnumerable<HotelPhoto>? photos,
            CancellationToken cancellationToken = default);

        Task HydrateHotelsAsync(
            IEnumerable<CongoTravel.Models.Hotel.Hotel>? hotels,
            CancellationToken cancellationToken = default);

        Task HydrateSiteTouristiquePhotosAsync(
            IEnumerable<SiteTouristiqueLieuPhoto>? photos,
            CancellationToken cancellationToken = default);

        Task HydrateSiteTouristiqueLieuxAsync(
            IEnumerable<SiteTouristiqueLieu>? lieux,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Charge PhotoData depuis le blob store lorsque StorageKey est présent et PhotoData vide (post-backfill).
    /// </summary>
    public class PhotoBinaryHydrator : IPhotoBinaryHydrator
    {
        private readonly ICongoTravelPhotoBlobStore _blobStore;
        private readonly ILogger<PhotoBinaryHydrator> _logger;

        public PhotoBinaryHydrator(
            ICongoTravelPhotoBlobStore blobStore,
            ILogger<PhotoBinaryHydrator> logger)
        {
            _blobStore = blobStore;
            _logger = logger;
        }

        public async Task HydrateAsync(
            IEnumerable<(string? StorageKey, byte[]? PhotoData, Action<byte[]> SetPhotoData)> items,
            CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                if (item.PhotoData != null && item.PhotoData.Length > 0)
                    continue;

                if (string.IsNullOrWhiteSpace(item.StorageKey))
                    continue;

                try
                {
                    var bytes = await _blobStore.GetBytesAsync(item.StorageKey, cancellationToken);
                    item.SetPhotoData(bytes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Impossible de hydrater la photo depuis {StorageKey}",
                        item.StorageKey);
                }
            }
        }

        public Task HydratePhotoVehiculesAsync(
            IEnumerable<PhotoVehicule>? photos,
            CancellationToken cancellationToken = default)
        {
            if (photos == null)
                return Task.CompletedTask;

            return HydrateAsync(
                photos.Select(p => (
                    p.StorageKey,
                    (byte[]?)p.PhotoData,
                    (Action<byte[]>)(bytes => p.PhotoData = bytes))),
                cancellationToken);
        }

        public Task HydrateVehiculesAsync(
            IEnumerable<Vehicule?>? vehicules,
            CancellationToken cancellationToken = default)
        {
            if (vehicules == null)
                return Task.CompletedTask;

            var photos = vehicules
                .Where(v => v?.Photos != null)
                .SelectMany(v => v!.Photos!);

            return HydratePhotoVehiculesAsync(photos, cancellationToken);
        }

        public Task HydrateVoyagesAsync(
            IEnumerable<Voyage>? voyages,
            CancellationToken cancellationToken = default)
        {
            if (voyages == null)
                return Task.CompletedTask;

            return HydrateVehiculesAsync(voyages.Select(v => v.Vehicule), cancellationToken);
        }

        public Task HydrateEvenementSessionPhotosAsync(
            IEnumerable<EvenementSessionPhoto>? photos,
            CancellationToken cancellationToken = default)
        {
            if (photos == null)
                return Task.CompletedTask;

            return HydrateAsync(
                photos.Select(p => (
                    p.StorageKey,
                    (byte[]?)p.PhotoData,
                    (Action<byte[]>)(bytes => p.PhotoData = bytes))),
                cancellationToken);
        }

        public Task HydrateEvenementSessionsAsync(
            IEnumerable<EvenementSession>? sessions,
            CancellationToken cancellationToken = default)
        {
            if (sessions == null)
                return Task.CompletedTask;

            return HydrateEvenementSessionPhotosAsync(
                sessions.Where(s => s.Photos != null).SelectMany(s => s.Photos!),
                cancellationToken);
        }

        public Task HydrateRestaurantPhotosAsync(
            IEnumerable<RestaurantPhoto>? photos,
            CancellationToken cancellationToken = default)
        {
            if (photos == null)
                return Task.CompletedTask;

            return HydrateAsync(
                photos.Select(p => (
                    p.StorageKey,
                    (byte[]?)p.PhotoData,
                    (Action<byte[]>)(bytes => p.PhotoData = bytes))),
                cancellationToken);
        }

        public Task HydrateRestaurantsAsync(
            IEnumerable<CongoTravel.Models.Restaurant.Restaurant>? restaurants,
            CancellationToken cancellationToken = default)
        {
            if (restaurants == null)
                return Task.CompletedTask;

            return HydrateRestaurantPhotosAsync(
                restaurants.Where(r => r.Photos != null).SelectMany(r => r.Photos!),
                cancellationToken);
        }

        public Task HydrateHotelPhotosAsync(
            IEnumerable<HotelPhoto>? photos,
            CancellationToken cancellationToken = default)
        {
            if (photos == null)
                return Task.CompletedTask;

            return HydrateAsync(
                photos.Select(p => (
                    p.StorageKey,
                    (byte[]?)p.PhotoData,
                    (Action<byte[]>)(bytes => p.PhotoData = bytes))),
                cancellationToken);
        }

        public Task HydrateHotelsAsync(
            IEnumerable<CongoTravel.Models.Hotel.Hotel>? hotels,
            CancellationToken cancellationToken = default)
        {
            if (hotels == null)
                return Task.CompletedTask;

            return HydrateHotelPhotosAsync(
                hotels.Where(h => h.Photos != null).SelectMany(h => h.Photos!),
                cancellationToken);
        }

        public Task HydrateSiteTouristiquePhotosAsync(
            IEnumerable<SiteTouristiqueLieuPhoto>? photos,
            CancellationToken cancellationToken = default)
        {
            if (photos == null)
                return Task.CompletedTask;

            return HydrateAsync(
                photos.Select(p => (
                    p.StorageKey,
                    (byte[]?)p.PhotoData,
                    (Action<byte[]>)(bytes => p.PhotoData = bytes))),
                cancellationToken);
        }

        public Task HydrateSiteTouristiqueLieuxAsync(
            IEnumerable<SiteTouristiqueLieu>? lieux,
            CancellationToken cancellationToken = default)
        {
            if (lieux == null)
                return Task.CompletedTask;

            return HydrateSiteTouristiquePhotosAsync(
                lieux.Where(l => l.Photos != null).SelectMany(l => l.Photos!),
                cancellationToken);
        }
    }
}
