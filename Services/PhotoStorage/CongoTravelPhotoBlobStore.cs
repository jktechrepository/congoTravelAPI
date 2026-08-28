using CongoTravel.Configuration;
using CongoTravel.Helpers;
using Microsoft.Extensions.Options;

namespace CongoTravel.Services.PhotoStorage
{
    public interface ICongoTravelPhotoBlobStore
    {
        /// <summary>Upload sous le préfixe congotravel/photos. Retourne la clé complète à persister.</summary>
        Task<string> UploadAsync(
            string entityFolder,
            int parentId,
            int ordre,
            byte[] content,
            string contentType,
            string? originalFileName = null,
            CancellationToken cancellationToken = default);

        Task<byte[]> GetBytesAsync(string storageKey, CancellationToken cancellationToken = default);

        /// <summary>Suppression best-effort (log + false si échec).</summary>
        Task<bool> TryDeleteAsync(string? storageKey, CancellationToken cancellationToken = default);
    }

    public class CongoTravelPhotoBlobStore : ICongoTravelPhotoBlobStore
    {
        private readonly IFileStorageService _fileStorage;
        private readonly PhotoStorageOptions _options;
        private readonly ILogger<CongoTravelPhotoBlobStore> _logger;

        public CongoTravelPhotoBlobStore(
            IFileStorageService fileStorage,
            IOptions<PhotoStorageOptions> options,
            ILogger<CongoTravelPhotoBlobStore> logger)
        {
            _fileStorage = fileStorage;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> UploadAsync(
            string entityFolder,
            int parentId,
            int ordre,
            byte[] content,
            string contentType,
            string? originalFileName = null,
            CancellationToken cancellationToken = default)
        {
            var extension = ResolveExtension(contentType, originalFileName);
            var relative = CongoTravelPhotoStorageKeys.BuildRelativeKey(entityFolder, parentId, ordre, extension);
            var fullKey = CongoTravelPhotoStorageKeys.CombinePrefix(_options.PhotoKeyPrefix, relative);

            var result = await _fileStorage.UploadBytesAsync(
                content,
                fullKey,
                contentType,
                originalFileName,
                cancellationToken);

            return result.FilePath;
        }

        public Task<byte[]> GetBytesAsync(string storageKey, CancellationToken cancellationToken = default) =>
            _fileStorage.GetFileBytesAsync(storageKey, cancellationToken);

        public async Task<bool> TryDeleteAsync(string? storageKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
                return false;

            try
            {
                return await _fileStorage.DeleteFileAsync(storageKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec suppression objet photo {StorageKey}", storageKey);
                return false;
            }
        }

        private static string ResolveExtension(string contentType, string? originalFileName)
        {
            if (!string.IsNullOrWhiteSpace(originalFileName))
            {
                var ext = Path.GetExtension(originalFileName);
                if (!string.IsNullOrEmpty(ext))
                    return ext;
            }

            return contentType switch
            {
                "image/png" => ".png",
                _ => ".jpg"
            };
        }
    }
}
