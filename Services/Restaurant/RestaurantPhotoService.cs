using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Services.PhotoStorage;
using Microsoft.AspNetCore.Http;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantPhotoService : IRestaurantPhotoService
    {
        public const int MaxPhotosPerRestaurant = 3;

        private readonly CongoTravelDbContext _context;
        private readonly ICongoTravelPhotoBlobStore _blobStore;
        private readonly IPhotoBinaryHydrator _hydrator;
        private readonly ILogger<RestaurantPhotoService> _logger;

        public RestaurantPhotoService(
            CongoTravelDbContext context,
            ICongoTravelPhotoBlobStore blobStore,
            IPhotoBinaryHydrator hydrator,
            ILogger<RestaurantPhotoService> logger)
        {
            _context = context;
            _blobStore = blobStore;
            _hydrator = hydrator;
            _logger = logger;
        }

        public async Task<IReadOnlyList<RestaurantPhoto>> GetByRestaurantIdAsync(
            int idRestaurant,
            int idSociete,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false)
        {
            await EnsureRestaurantExistsAsync(idRestaurant, idSociete, cancellationToken);

            var photos = await _context.RestaurantPhotos
                .AsNoTracking()
                .Where(p => p.IdRestaurant == idRestaurant && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);

            if (includePhotoBase64)
                await _hydrator.HydrateRestaurantPhotosAsync(photos, cancellationToken);

            return photos;
        }

        public async Task<PhotoContentPayload?> GetContentAsync(
            int idRestaurant,
            int idSociete,
            int idRestaurantPhoto,
            CancellationToken cancellationToken = default)
        {
            await EnsureRestaurantExistsAsync(idRestaurant, idSociete, cancellationToken);

            var photo = await _context.RestaurantPhotos
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.IdRestaurantPhoto == idRestaurantPhoto
                         && p.IdRestaurant == idRestaurant
                         && p.Statut,
                    cancellationToken);

            if (photo == null)
                return null;

            return await ResolveContentAsync(
                photo.PhotoData,
                photo.StorageKey,
                photo.TypeMIME,
                photo.OriginalFileName,
                cancellationToken);
        }

        public async Task AddPhotosOnCreateAsync(
            int idRestaurant,
            int idSociete,
            IReadOnlyList<AddRestaurantPhotoDto>? photos,
            CancellationToken cancellationToken = default)
        {
            if (photos == null || photos.Count == 0)
                return;

            await EnsureRestaurantExistsAsync(idRestaurant, idSociete, cancellationToken);
            ValidatePhotoBatch(photos);

            var uploadedKeys = new List<string>();
            try
            {
                var active = new List<RestaurantPhoto>();
                var entities = new List<RestaurantPhoto>();
                foreach (var dto in photos)
                {
                    var entity = await BuildPhotoEntityAsync(idRestaurant, dto, active, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(entity.StorageKey))
                        uploadedKeys.Add(entity.StorageKey);
                    entities.Add(entity);
                    active.Add(entity);
                }

                _context.RestaurantPhotos.AddRange(entities);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Photos restaurant créées en lot — RestaurantId={RestaurantId}, Nombre={Count}",
                    idRestaurant,
                    entities.Count);
            }
            catch
            {
                await CompensateUploadedKeysAsync(uploadedKeys, cancellationToken);
                throw;
            }
        }

        public async Task<IReadOnlyList<RestaurantPhoto>> ReplaceAllFromFilesAsync(
            int idRestaurant,
            int idSociete,
            IReadOnlyList<IFormFile> files,
            IReadOnlyList<int>? ordres = null,
            CancellationToken cancellationToken = default)
        {
            files ??= Array.Empty<IFormFile>();
            ValidateFileBatch(files, ordres);
            await EnsureRestaurantExistsAsync(idRestaurant, idSociete, cancellationToken);

            var existing = await _context.RestaurantPhotos
                .Where(p => p.IdRestaurant == idRestaurant)
                .ToListAsync(cancellationToken);
            var keysToDeleteAfterCommit = existing
                .Where(p => !string.IsNullOrWhiteSpace(p.StorageKey))
                .Select(p => p.StorageKey!)
                .ToList();

            var uploadedKeys = new List<string>();
            var entities = new List<RestaurantPhoto>();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (existing.Count > 0)
                {
                    _context.RestaurantPhotos.RemoveRange(existing);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                if (files.Count > 0)
                {
                    var active = new List<RestaurantPhoto>();
                    for (var i = 0; i < files.Count; i++)
                    {
                        var file = files[i];
                        var fileName = file.FileName;
                        var (bytes, _, contentType) = await VehiculePhotoBase64Helper.ParseAndValidateFileAsync(
                            file,
                            fileName,
                            cancellationToken);
                        var ordre = ordres != null && i < ordres.Count ? ordres[i] : (int?)null;
                        var entity = await BuildPhotoEntityFromBytesAsync(
                            idRestaurant,
                            bytes,
                            contentType,
                            fileName,
                            ordre,
                            active,
                            cancellationToken);
                        if (!string.IsNullOrWhiteSpace(entity.StorageKey))
                            uploadedKeys.Add(entity.StorageKey);
                        entities.Add(entity);
                        active.Add(entity);
                    }

                    _context.RestaurantPhotos.AddRange(entities);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                foreach (var key in keysToDeleteAfterCommit)
                    await _blobStore.TryDeleteAsync(key, cancellationToken);

                _logger.LogInformation(
                    "Photos restaurant remplacées (multipart) — RestaurantId={RestaurantId}, Nombre={Count}",
                    idRestaurant,
                    entities.Count);

                return entities;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                await CompensateUploadedKeysAsync(uploadedKeys, cancellationToken);
                throw;
            }
        }

        public async Task<RestaurantPhoto> AddPhotoAsync(
            int idRestaurant,
            int idSociete,
            AddRestaurantPhotoDto dto,
            CancellationToken cancellationToken = default)
        {
            await EnsureRestaurantExistsAsync(idRestaurant, idSociete, cancellationToken);

            var activePhotos = await _context.RestaurantPhotos
                .Where(p => p.IdRestaurant == idRestaurant && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);

            if (activePhotos.Count >= MaxPhotosPerRestaurant)
            {
                throw new InvalidOperationException(
                    $"Un établissement restaurant ne peut pas avoir plus de {MaxPhotosPerRestaurant} photos.");
            }

            var photo = await BuildPhotoEntityAsync(idRestaurant, dto, activePhotos, cancellationToken);

            if (activePhotos.Any(p => p.Ordre == photo.Ordre))
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw new InvalidOperationException(
                    $"La position {photo.Ordre} est déjà occupée pour cet établissement.");
            }

            try
            {
                _context.RestaurantPhotos.Add(photo);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw;
            }

            _logger.LogInformation(
                "Photo restaurant ajoutée — RestaurantId={RestaurantId}, PhotoId={PhotoId}, Ordre={Ordre}, StorageKey={StorageKey}, Taille={FileSize}",
                idRestaurant,
                photo.IdRestaurantPhoto,
                photo.Ordre,
                photo.StorageKey,
                photo.FileSize);

            return photo;
        }

        public async Task<RestaurantPhoto> AddPhotoFromFileAsync(
            int idRestaurant,
            int idSociete,
            IFormFile file,
            int? ordre = null,
            string? fileName = null,
            CancellationToken cancellationToken = default)
        {
            var resolvedFileName = string.IsNullOrWhiteSpace(fileName) ? file.FileName : fileName;
            var (bytes, _, contentType) = await VehiculePhotoBase64Helper.ParseAndValidateFileAsync(
                file,
                resolvedFileName,
                cancellationToken);

            await EnsureRestaurantExistsAsync(idRestaurant, idSociete, cancellationToken);

            var activePhotos = await _context.RestaurantPhotos
                .Where(p => p.IdRestaurant == idRestaurant && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);

            if (activePhotos.Count >= MaxPhotosPerRestaurant)
            {
                throw new InvalidOperationException(
                    $"Un établissement restaurant ne peut pas avoir plus de {MaxPhotosPerRestaurant} photos.");
            }

            var photo = await BuildPhotoEntityFromBytesAsync(
                idRestaurant,
                bytes,
                contentType,
                resolvedFileName,
                ordre,
                activePhotos,
                cancellationToken);

            if (activePhotos.Any(p => p.Ordre == photo.Ordre))
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw new InvalidOperationException(
                    $"La position {photo.Ordre} est déjà occupée pour cet établissement.");
            }

            try
            {
                _context.RestaurantPhotos.Add(photo);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw;
            }

            _logger.LogInformation(
                "Photo restaurant ajoutée (multipart) — RestaurantId={RestaurantId}, PhotoId={PhotoId}, Ordre={Ordre}, StorageKey={StorageKey}, Taille={FileSize}",
                idRestaurant,
                photo.IdRestaurantPhoto,
                photo.Ordre,
                photo.StorageKey,
                photo.FileSize);

            return photo;
        }

        public async Task<RestaurantPhoto?> UpdateOrdreAsync(
            int idRestaurant,
            int idSociete,
            int idRestaurantPhoto,
            int ordre,
            CancellationToken cancellationToken = default)
        {
            if (ordre < 1 || ordre > MaxPhotosPerRestaurant)
            {
                throw new ArgumentException(
                    $"L'ordre doit être compris entre 1 et {MaxPhotosPerRestaurant}.");
            }

            await EnsureRestaurantExistsAsync(idRestaurant, idSociete, cancellationToken);

            var photo = await _context.RestaurantPhotos
                .FirstOrDefaultAsync(
                    p => p.IdRestaurantPhoto == idRestaurantPhoto
                         && p.IdRestaurant == idRestaurant
                         && p.Statut,
                    cancellationToken);

            if (photo == null)
                return null;

            var conflict = await _context.RestaurantPhotos
                .AnyAsync(
                    p => p.IdRestaurant == idRestaurant
                         && p.Ordre == ordre
                         && p.IdRestaurantPhoto != idRestaurantPhoto
                         && p.Statut,
                    cancellationToken);

            if (conflict)
            {
                throw new InvalidOperationException(
                    $"La position {ordre} est déjà occupée pour cet établissement.");
            }

            photo.Ordre = ordre;
            photo.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return photo;
        }

        public async Task<bool> DeletePhotoAsync(
            int idRestaurant,
            int idSociete,
            int idRestaurantPhoto,
            CancellationToken cancellationToken = default)
        {
            await EnsureRestaurantExistsAsync(idRestaurant, idSociete, cancellationToken);

            var photo = await _context.RestaurantPhotos
                .FirstOrDefaultAsync(
                    p => p.IdRestaurantPhoto == idRestaurantPhoto
                         && p.IdRestaurant == idRestaurant,
                    cancellationToken);

            if (photo == null)
                return false;

            var storageKey = photo.StorageKey;
            _context.RestaurantPhotos.Remove(photo);
            await _context.SaveChangesAsync(cancellationToken);
            await _blobStore.TryDeleteAsync(storageKey, cancellationToken);
            return true;
        }

        private async Task<PhotoContentPayload?> ResolveContentAsync(
            byte[]? photoData,
            string? storageKey,
            string? typeMime,
            string? originalFileName,
            CancellationToken cancellationToken)
        {
            byte[]? bytes = null;
            if (photoData != null && photoData.Length > 0)
                bytes = photoData;
            else if (!string.IsNullOrWhiteSpace(storageKey))
                bytes = await _blobStore.GetBytesAsync(storageKey, cancellationToken);

            if (bytes == null || bytes.Length == 0)
                return null;

            return new PhotoContentPayload
            {
                Content = bytes,
                ContentType = PhotoContentHelper.ResolveContentType(typeMime),
                FileName = originalFileName
            };
        }

        private async Task EnsureRestaurantExistsAsync(
            int idRestaurant,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var exists = await _context.Restaurants
                .AsNoTracking()
                .AnyAsync(
                    r => r.IdRestaurant == idRestaurant && r.IdSociete == idSociete,
                    cancellationToken);

            if (!exists)
            {
                throw new KeyNotFoundException(
                    $"Établissement restaurant {idRestaurant} introuvable pour la société {idSociete}.");
            }
        }

        private static void ValidatePhotoBatch(IReadOnlyList<AddRestaurantPhotoDto> photos)
        {
            if (photos.Count > MaxPhotosPerRestaurant)
            {
                throw new InvalidOperationException(
                    $"Un établissement restaurant ne peut pas avoir plus de {MaxPhotosPerRestaurant} photos.");
            }

            var specifiedOrdres = photos
                .Where(p => p.Ordre.HasValue)
                .Select(p => p.Ordre!.Value)
                .ToList();

            if (specifiedOrdres.Any(o => o < 1 || o > MaxPhotosPerRestaurant))
            {
                throw new ArgumentException(
                    $"L'ordre doit être compris entre 1 et {MaxPhotosPerRestaurant}.");
            }

            if (specifiedOrdres.Count != specifiedOrdres.Distinct().Count())
                throw new ArgumentException("Chaque photo doit avoir un ordre unique (1, 2 ou 3).");

            foreach (var dto in photos)
            {
                if (string.IsNullOrWhiteSpace(dto.PhotoBase64))
                    throw new ArgumentException("Chaque photo doit contenir un photoBase64 non vide.");
            }
        }

        private static void ValidateFileBatch(IReadOnlyList<IFormFile> files, IReadOnlyList<int>? ordres)
        {
            if (files.Count > MaxPhotosPerRestaurant)
            {
                throw new InvalidOperationException(
                    $"Un établissement restaurant ne peut pas avoir plus de {MaxPhotosPerRestaurant} photos.");
            }

            if (ordres != null && ordres.Count > 0 && ordres.Count != files.Count)
                throw new ArgumentException("Le nombre d'ordres doit correspondre au nombre de fichiers.");

            if (ordres != null && ordres.Count > 0)
            {
                if (ordres.Any(o => o < 1 || o > MaxPhotosPerRestaurant))
                {
                    throw new ArgumentException(
                        $"L'ordre doit être compris entre 1 et {MaxPhotosPerRestaurant}.");
                }

                if (ordres.Count != ordres.Distinct().Count())
                    throw new ArgumentException("Chaque photo doit avoir un ordre unique (1, 2 ou 3).");
            }

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("Chaque fichier photo doit être non vide.");
            }
        }

        private async Task<RestaurantPhoto> BuildPhotoEntityAsync(
            int idRestaurant,
            AddRestaurantPhotoDto dto,
            IReadOnlyList<RestaurantPhoto> activePhotos,
            CancellationToken cancellationToken)
        {
            var (bytes, _, contentType) = VehiculePhotoBase64Helper.ParseAndValidate(
                dto.PhotoBase64,
                dto.FileName);

            return await BuildPhotoEntityFromBytesAsync(
                idRestaurant,
                bytes,
                contentType,
                dto.FileName,
                dto.Ordre,
                activePhotos,
                cancellationToken);
        }

        private async Task<RestaurantPhoto> BuildPhotoEntityFromBytesAsync(
            int idRestaurant,
            byte[] bytes,
            string contentType,
            string? fileName,
            int? requestedOrdre,
            IReadOnlyList<RestaurantPhoto> activePhotos,
            CancellationToken cancellationToken)
        {
            var ordre = ResolveOrdre(requestedOrdre, activePhotos);

            string? storageKey = null;
            try
            {
                storageKey = await _blobStore.UploadAsync(
                    CongoTravelPhotoStorageKeys.EntityRestaurants,
                    idRestaurant,
                    ordre,
                    bytes,
                    contentType,
                    fileName,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Upload photo restaurant échoué — fallback BLOB. RestaurantId={RestaurantId}, Ordre={Ordre}",
                    idRestaurant,
                    ordre);
            }

            return new RestaurantPhoto
            {
                IdRestaurant = idRestaurant,
                PhotoData = bytes,
                StorageKey = storageKey,
                Ordre = ordre,
                OriginalFileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim(),
                TypeMIME = contentType,
                FileSize = bytes.Length,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
        }

        private async Task CompensateUploadedKeysAsync(
            IEnumerable<string> keys,
            CancellationToken cancellationToken)
        {
            foreach (var key in keys)
                await _blobStore.TryDeleteAsync(key, cancellationToken);
        }

        private static int ResolveOrdre(int? requestedOrdre, IReadOnlyList<RestaurantPhoto> activePhotos)
        {
            if (requestedOrdre.HasValue)
            {
                if (requestedOrdre.Value < 1 || requestedOrdre.Value > MaxPhotosPerRestaurant)
                {
                    throw new ArgumentException(
                        $"L'ordre doit être compris entre 1 et {MaxPhotosPerRestaurant}.");
                }

                return requestedOrdre.Value;
            }

            var used = activePhotos.Select(p => p.Ordre).ToHashSet();
            for (var i = 1; i <= MaxPhotosPerRestaurant; i++)
            {
                if (!used.Contains(i))
                    return i;
            }

            throw new InvalidOperationException(
                $"Aucune position libre (maximum {MaxPhotosPerRestaurant} photos).");
        }
    }
}
