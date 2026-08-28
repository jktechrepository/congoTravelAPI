using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.SiteTouristique;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.PhotoStorage
{
    public interface IPhotoS3BackfillService
    {
        Task<PhotoS3BackfillResult> BackfillVehiculesAsync(
            bool clearPhotoDataAfterUpload = true,
            int batchSize = 50,
            CancellationToken cancellationToken = default);

        Task<PhotoS3BackfillResult> BackfillEvenementSessionsAsync(
            bool clearPhotoDataAfterUpload = true,
            int batchSize = 50,
            CancellationToken cancellationToken = default);

        Task<PhotoS3BackfillResult> BackfillRestaurantsAsync(
            bool clearPhotoDataAfterUpload = true,
            int batchSize = 50,
            CancellationToken cancellationToken = default);

        Task<PhotoS3BackfillResult> BackfillSitesTouristiquesAsync(
            bool clearPhotoDataAfterUpload = true,
            int batchSize = 50,
            CancellationToken cancellationToken = default);

        Task<PhotoS3BackfillResult> BackfillAllAsync(
            bool clearPhotoDataAfterUpload = true,
            int batchSize = 50,
            CancellationToken cancellationToken = default);
    }

    public class PhotoS3BackfillResult
    {
        public int Migrated { get; set; }
        public int SkippedAlreadyMigratedOrEmpty { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// One-shot : MEDIUMBLOB → objet storage, StorageKey, optionnellement null PhotoData.
    /// </summary>
    public class PhotoS3BackfillService : IPhotoS3BackfillService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ICongoTravelPhotoBlobStore _blobStore;
        private readonly ILogger<PhotoS3BackfillService> _logger;

        public PhotoS3BackfillService(
            CongoTravelDbContext context,
            ICongoTravelPhotoBlobStore blobStore,
            ILogger<PhotoS3BackfillService> logger)
        {
            _context = context;
            _blobStore = blobStore;
            _logger = logger;
        }

        public async Task<PhotoS3BackfillResult> BackfillAllAsync(
            bool clearPhotoDataAfterUpload = true,
            int batchSize = 50,
            CancellationToken cancellationToken = default)
        {
            var aggregate = new PhotoS3BackfillResult();
            foreach (var partial in new[]
                     {
                         await BackfillVehiculesAsync(clearPhotoDataAfterUpload, batchSize, cancellationToken),
                         await BackfillEvenementSessionsAsync(clearPhotoDataAfterUpload, batchSize, cancellationToken),
                         await BackfillRestaurantsAsync(clearPhotoDataAfterUpload, batchSize, cancellationToken),
                         await BackfillSitesTouristiquesAsync(clearPhotoDataAfterUpload, batchSize, cancellationToken)
                     })
            {
                aggregate.Migrated += partial.Migrated;
                aggregate.SkippedAlreadyMigratedOrEmpty += partial.SkippedAlreadyMigratedOrEmpty;
                aggregate.Failed += partial.Failed;
                aggregate.Errors.AddRange(partial.Errors);
            }

            return aggregate;
        }

        public async Task<PhotoS3BackfillResult> BackfillVehiculesAsync(
            bool clearPhotoDataAfterUpload = true,
            int batchSize = 50,
            CancellationToken cancellationToken = default)
        {
            var result = new PhotoS3BackfillResult();
            batchSize = Math.Clamp(batchSize, 1, 200);

            while (true)
            {
                var batch = await _context.PhotoVehicules
                    .Where(p => p.StorageKey == null && p.PhotoData != null && p.PhotoData.Length > 0)
                    .OrderBy(p => p.IdPhotoVehicule)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var photo in batch)
                {
                    try
                    {
                        await MigrateOneAsync(
                            CongoTravelPhotoStorageKeys.EntityVehicules,
                            photo.IdVehicule,
                            photo.Ordre,
                            photo.PhotoData!,
                            photo.TypeMIME,
                            photo.OriginalFileName,
                            key => photo.StorageKey = key,
                            () => { if (clearPhotoDataAfterUpload) photo.PhotoData = null; },
                            cancellationToken);
                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        var msg = $"PhotoVehicule#{photo.IdPhotoVehicule}: {ex.Message}";
                        result.Errors.Add(msg);
                        _logger.LogError(ex, "Backfill véhicule échoué — {Message}", msg);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            result.SkippedAlreadyMigratedOrEmpty = await _context.PhotoVehicules.CountAsync(
                p => p.StorageKey != null || p.PhotoData == null || p.PhotoData.Length == 0,
                cancellationToken);

            LogDone(nameof(PhotoVehicule), result);
            return result;
        }

        public async Task<PhotoS3BackfillResult> BackfillEvenementSessionsAsync(
            bool clearPhotoDataAfterUpload = true,
            int batchSize = 50,
            CancellationToken cancellationToken = default)
        {
            var result = new PhotoS3BackfillResult();
            batchSize = Math.Clamp(batchSize, 1, 200);

            while (true)
            {
                var batch = await _context.EvenementSessionPhotos
                    .Where(p => p.StorageKey == null && p.PhotoData != null && p.PhotoData.Length > 0)
                    .OrderBy(p => p.IdEvenementSessionPhoto)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var photo in batch)
                {
                    try
                    {
                        await MigrateOneAsync(
                            CongoTravelPhotoStorageKeys.EntityEvenementSessions,
                            photo.IdEvenementSession,
                            photo.Ordre,
                            photo.PhotoData!,
                            photo.TypeMIME,
                            photo.OriginalFileName,
                            key => photo.StorageKey = key,
                            () => { if (clearPhotoDataAfterUpload) photo.PhotoData = null; },
                            cancellationToken);
                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        var msg = $"EvenementSessionPhoto#{photo.IdEvenementSessionPhoto}: {ex.Message}";
                        result.Errors.Add(msg);
                        _logger.LogError(ex, "Backfill session événement échoué — {Message}", msg);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            result.SkippedAlreadyMigratedOrEmpty = await _context.EvenementSessionPhotos.CountAsync(
                p => p.StorageKey != null || p.PhotoData == null || p.PhotoData.Length == 0,
                cancellationToken);

            LogDone(nameof(EvenementSessionPhoto), result);
            return result;
        }

        public async Task<PhotoS3BackfillResult> BackfillRestaurantsAsync(
            bool clearPhotoDataAfterUpload = true,
            int batchSize = 50,
            CancellationToken cancellationToken = default)
        {
            var result = new PhotoS3BackfillResult();
            batchSize = Math.Clamp(batchSize, 1, 200);

            while (true)
            {
                var batch = await _context.RestaurantPhotos
                    .Where(p => p.StorageKey == null && p.PhotoData != null && p.PhotoData.Length > 0)
                    .OrderBy(p => p.IdRestaurantPhoto)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var photo in batch)
                {
                    try
                    {
                        await MigrateOneAsync(
                            CongoTravelPhotoStorageKeys.EntityRestaurants,
                            photo.IdRestaurant,
                            photo.Ordre,
                            photo.PhotoData!,
                            photo.TypeMIME,
                            photo.OriginalFileName,
                            key => photo.StorageKey = key,
                            () => { if (clearPhotoDataAfterUpload) photo.PhotoData = null; },
                            cancellationToken);
                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        var msg = $"RestaurantPhoto#{photo.IdRestaurantPhoto}: {ex.Message}";
                        result.Errors.Add(msg);
                        _logger.LogError(ex, "Backfill restaurant échoué — {Message}", msg);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            result.SkippedAlreadyMigratedOrEmpty = await _context.RestaurantPhotos.CountAsync(
                p => p.StorageKey != null || p.PhotoData == null || p.PhotoData.Length == 0,
                cancellationToken);

            LogDone(nameof(RestaurantPhoto), result);
            return result;
        }

        public async Task<PhotoS3BackfillResult> BackfillSitesTouristiquesAsync(
            bool clearPhotoDataAfterUpload = true,
            int batchSize = 50,
            CancellationToken cancellationToken = default)
        {
            var result = new PhotoS3BackfillResult();
            batchSize = Math.Clamp(batchSize, 1, 200);

            while (true)
            {
                var batch = await _context.SiteTouristiqueLieuPhotos
                    .Where(p => p.StorageKey == null && p.PhotoData != null && p.PhotoData.Length > 0)
                    .OrderBy(p => p.IdSiteTouristiqueLieuPhoto)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var photo in batch)
                {
                    try
                    {
                        await MigrateOneAsync(
                            CongoTravelPhotoStorageKeys.EntitySitesTouristiques,
                            photo.IdSiteTouristique,
                            photo.Ordre,
                            photo.PhotoData!,
                            photo.TypeMIME,
                            photo.OriginalFileName,
                            key => photo.StorageKey = key,
                            () => { if (clearPhotoDataAfterUpload) photo.PhotoData = null; },
                            cancellationToken);
                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        var msg = $"SiteTouristiqueLieuPhoto#{photo.IdSiteTouristiqueLieuPhoto}: {ex.Message}";
                        result.Errors.Add(msg);
                        _logger.LogError(ex, "Backfill site touristique échoué — {Message}", msg);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            result.SkippedAlreadyMigratedOrEmpty = await _context.SiteTouristiqueLieuPhotos.CountAsync(
                p => p.StorageKey != null || p.PhotoData == null || p.PhotoData.Length == 0,
                cancellationToken);

            LogDone(nameof(SiteTouristiqueLieuPhoto), result);
            return result;
        }

        private async Task MigrateOneAsync(
            string entityFolder,
            int parentId,
            int ordre,
            byte[] bytes,
            string? typeMime,
            string? originalFileName,
            Action<string> setStorageKey,
            Action clearPhotoData,
            CancellationToken cancellationToken)
        {
            var contentType = string.IsNullOrWhiteSpace(typeMime) ? "image/jpeg" : typeMime!;
            var key = await _blobStore.UploadAsync(
                entityFolder,
                parentId,
                ordre,
                bytes,
                contentType,
                originalFileName,
                cancellationToken);
            setStorageKey(key);
            clearPhotoData();
        }

        private void LogDone(string entity, PhotoS3BackfillResult result)
        {
            _logger.LogInformation(
                "Backfill {Entity} terminé — Migrated={Migrated}, Failed={Failed}, AlreadyOkOrEmpty={Skipped}",
                entity,
                result.Migrated,
                result.Failed,
                result.SkippedAlreadyMigratedOrEmpty);
        }
    }
}
