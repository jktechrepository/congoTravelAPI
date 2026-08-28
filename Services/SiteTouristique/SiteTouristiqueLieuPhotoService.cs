using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Services.PhotoStorage;
using Microsoft.AspNetCore.Http;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueLieuPhotoService : ISiteTouristiqueLieuPhotoService
    {
        public const int MaxPhotosPerLieu = 3;

        private readonly CongoTravelDbContext _context;
        private readonly ICongoTravelPhotoBlobStore _blobStore;
        private readonly IPhotoBinaryHydrator _hydrator;
        private readonly ILogger<SiteTouristiqueLieuPhotoService> _logger;

        public SiteTouristiqueLieuPhotoService(
            CongoTravelDbContext context,
            ICongoTravelPhotoBlobStore blobStore,
            IPhotoBinaryHydrator hydrator,
            ILogger<SiteTouristiqueLieuPhotoService> logger)
        {
            _context = context;
            _blobStore = blobStore;
            _hydrator = hydrator;
            _logger = logger;
        }

        public async Task<IReadOnlyList<SiteTouristiqueLieuPhoto>> GetByLieuIdAsync(
            int idSiteTouristique,
            int idSociete,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false)
        {
            await EnsureLieuExistsAsync(idSiteTouristique, idSociete, cancellationToken);

            var photos = await _context.SiteTouristiqueLieuPhotos
                .AsNoTracking()
                .Where(p => p.IdSiteTouristique == idSiteTouristique && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);

            if (includePhotoBase64)
                await _hydrator.HydrateSiteTouristiquePhotosAsync(photos, cancellationToken);

            return photos;
        }

        public async Task<PhotoContentPayload?> GetContentAsync(
            int idSiteTouristique,
            int idSociete,
            int idSiteTouristiqueLieuPhoto,
            CancellationToken cancellationToken = default)
        {
            await EnsureLieuExistsAsync(idSiteTouristique, idSociete, cancellationToken);

            var photo = await _context.SiteTouristiqueLieuPhotos
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.IdSiteTouristiqueLieuPhoto == idSiteTouristiqueLieuPhoto
                         && p.IdSiteTouristique == idSiteTouristique
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
            int idSiteTouristique,
            int idSociete,
            IReadOnlyList<AddSiteTouristiqueLieuPhotoDto>? photos,
            CancellationToken cancellationToken = default)
        {
            if (photos == null || photos.Count == 0)
                return;

            await EnsureLieuExistsAsync(idSiteTouristique, idSociete, cancellationToken);
            ValidatePhotoBatch(photos);

            var uploadedKeys = new List<string>();
            try
            {
                var active = new List<SiteTouristiqueLieuPhoto>();
                var entities = new List<SiteTouristiqueLieuPhoto>();
                foreach (var dto in photos)
                {
                    var entity = await BuildPhotoEntityAsync(idSiteTouristique, dto, active, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(entity.StorageKey))
                        uploadedKeys.Add(entity.StorageKey);
                    entities.Add(entity);
                    active.Add(entity);
                }

                _context.SiteTouristiqueLieuPhotos.AddRange(entities);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Photos lieu touristique créées en lot — LieuId={LieuId}, Nombre={Count}",
                    idSiteTouristique,
                    entities.Count);
            }
            catch
            {
                await CompensateUploadedKeysAsync(uploadedKeys, cancellationToken);
                throw;
            }
        }

        public async Task<IReadOnlyList<SiteTouristiqueLieuPhoto>> ReplaceAllFromFilesAsync(
            int idSiteTouristique,
            int idSociete,
            IReadOnlyList<IFormFile> files,
            IReadOnlyList<int>? ordres = null,
            CancellationToken cancellationToken = default)
        {
            files ??= Array.Empty<IFormFile>();
            ValidateFileBatch(files, ordres);
            await EnsureLieuExistsAsync(idSiteTouristique, idSociete, cancellationToken);

            var existing = await _context.SiteTouristiqueLieuPhotos
                .Where(p => p.IdSiteTouristique == idSiteTouristique)
                .ToListAsync(cancellationToken);
            var keysToDeleteAfterCommit = existing
                .Where(p => !string.IsNullOrWhiteSpace(p.StorageKey))
                .Select(p => p.StorageKey!)
                .ToList();

            var uploadedKeys = new List<string>();
            var entities = new List<SiteTouristiqueLieuPhoto>();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (existing.Count > 0)
                {
                    _context.SiteTouristiqueLieuPhotos.RemoveRange(existing);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                if (files.Count > 0)
                {
                    var active = new List<SiteTouristiqueLieuPhoto>();
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
                            idSiteTouristique,
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

                    _context.SiteTouristiqueLieuPhotos.AddRange(entities);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                foreach (var key in keysToDeleteAfterCommit)
                    await _blobStore.TryDeleteAsync(key, cancellationToken);

                _logger.LogInformation(
                    "Photos lieu touristique remplacées (multipart) — LieuId={LieuId}, Nombre={Count}",
                    idSiteTouristique,
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

        public async Task<SiteTouristiqueLieuPhoto> AddPhotoAsync(
            int idSiteTouristique,
            int idSociete,
            AddSiteTouristiqueLieuPhotoDto dto,
            CancellationToken cancellationToken = default)
        {
            await EnsureLieuExistsAsync(idSiteTouristique, idSociete, cancellationToken);

            var activePhotos = await _context.SiteTouristiqueLieuPhotos
                .Where(p => p.IdSiteTouristique == idSiteTouristique && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);

            if (activePhotos.Count >= MaxPhotosPerLieu)
            {
                throw new InvalidOperationException(
                    $"Un lieu touristique ne peut pas avoir plus de {MaxPhotosPerLieu} photos.");
            }

            var photo = await BuildPhotoEntityAsync(idSiteTouristique, dto, activePhotos, cancellationToken);

            if (activePhotos.Any(p => p.Ordre == photo.Ordre))
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw new InvalidOperationException(
                    $"La position {photo.Ordre} est déjà occupée pour ce lieu.");
            }

            try
            {
                _context.SiteTouristiqueLieuPhotos.Add(photo);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw;
            }

            _logger.LogInformation(
                "Photo lieu touristique ajoutée — LieuId={LieuId}, PhotoId={PhotoId}, Ordre={Ordre}, StorageKey={StorageKey}, Taille={FileSize}",
                idSiteTouristique,
                photo.IdSiteTouristiqueLieuPhoto,
                photo.Ordre,
                photo.StorageKey,
                photo.FileSize);

            return photo;
        }

        public async Task<SiteTouristiqueLieuPhoto> AddPhotoFromFileAsync(
            int idSiteTouristique,
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

            await EnsureLieuExistsAsync(idSiteTouristique, idSociete, cancellationToken);

            var activePhotos = await _context.SiteTouristiqueLieuPhotos
                .Where(p => p.IdSiteTouristique == idSiteTouristique && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);

            if (activePhotos.Count >= MaxPhotosPerLieu)
            {
                throw new InvalidOperationException(
                    $"Un lieu touristique ne peut pas avoir plus de {MaxPhotosPerLieu} photos.");
            }

            var photo = await BuildPhotoEntityFromBytesAsync(
                idSiteTouristique,
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
                    $"La position {photo.Ordre} est déjà occupée pour ce lieu.");
            }

            try
            {
                _context.SiteTouristiqueLieuPhotos.Add(photo);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw;
            }

            _logger.LogInformation(
                "Photo lieu touristique ajoutée (multipart) — LieuId={LieuId}, PhotoId={PhotoId}, Ordre={Ordre}, StorageKey={StorageKey}, Taille={FileSize}",
                idSiteTouristique,
                photo.IdSiteTouristiqueLieuPhoto,
                photo.Ordre,
                photo.StorageKey,
                photo.FileSize);

            return photo;
        }

        public async Task<SiteTouristiqueLieuPhoto?> UpdateOrdreAsync(
            int idSiteTouristique,
            int idSociete,
            int idSiteTouristiqueLieuPhoto,
            int ordre,
            CancellationToken cancellationToken = default)
        {
            if (ordre < 1 || ordre > MaxPhotosPerLieu)
            {
                throw new ArgumentException(
                    $"L'ordre doit être compris entre 1 et {MaxPhotosPerLieu}.");
            }

            await EnsureLieuExistsAsync(idSiteTouristique, idSociete, cancellationToken);

            var photo = await _context.SiteTouristiqueLieuPhotos
                .FirstOrDefaultAsync(
                    p => p.IdSiteTouristiqueLieuPhoto == idSiteTouristiqueLieuPhoto
                         && p.IdSiteTouristique == idSiteTouristique
                         && p.Statut,
                    cancellationToken);

            if (photo == null)
                return null;

            var conflict = await _context.SiteTouristiqueLieuPhotos
                .AnyAsync(
                    p => p.IdSiteTouristique == idSiteTouristique
                         && p.Ordre == ordre
                         && p.IdSiteTouristiqueLieuPhoto != idSiteTouristiqueLieuPhoto
                         && p.Statut,
                    cancellationToken);

            if (conflict)
            {
                throw new InvalidOperationException(
                    $"La position {ordre} est déjà occupée pour ce lieu.");
            }

            photo.Ordre = ordre;
            photo.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return photo;
        }

        public async Task<bool> DeletePhotoAsync(
            int idSiteTouristique,
            int idSociete,
            int idSiteTouristiqueLieuPhoto,
            CancellationToken cancellationToken = default)
        {
            await EnsureLieuExistsAsync(idSiteTouristique, idSociete, cancellationToken);

            var photo = await _context.SiteTouristiqueLieuPhotos
                .FirstOrDefaultAsync(
                    p => p.IdSiteTouristiqueLieuPhoto == idSiteTouristiqueLieuPhoto
                         && p.IdSiteTouristique == idSiteTouristique,
                    cancellationToken);

            if (photo == null)
                return false;

            var storageKey = photo.StorageKey;
            _context.SiteTouristiqueLieuPhotos.Remove(photo);
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

        private async Task EnsureLieuExistsAsync(
            int idSiteTouristique,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var exists = await _context.SiteTouristiques
                .AsNoTracking()
                .AnyAsync(
                    l => l.IdSiteTouristique == idSiteTouristique && l.IdSociete == idSociete,
                    cancellationToken);

            if (!exists)
            {
                throw new KeyNotFoundException(
                    $"Lieu site touristique {idSiteTouristique} introuvable pour la société {idSociete}.");
            }
        }

        private static void ValidatePhotoBatch(IReadOnlyList<AddSiteTouristiqueLieuPhotoDto> photos)
        {
            if (photos.Count > MaxPhotosPerLieu)
            {
                throw new InvalidOperationException(
                    $"Un lieu touristique ne peut pas avoir plus de {MaxPhotosPerLieu} photos.");
            }

            var specifiedOrdres = photos
                .Where(p => p.Ordre.HasValue)
                .Select(p => p.Ordre!.Value)
                .ToList();

            if (specifiedOrdres.Any(o => o < 1 || o > MaxPhotosPerLieu))
            {
                throw new ArgumentException(
                    $"L'ordre doit être compris entre 1 et {MaxPhotosPerLieu}.");
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
            if (files.Count > MaxPhotosPerLieu)
            {
                throw new InvalidOperationException(
                    $"Un lieu touristique ne peut pas avoir plus de {MaxPhotosPerLieu} photos.");
            }

            if (ordres != null && ordres.Count > 0 && ordres.Count != files.Count)
                throw new ArgumentException("Le nombre d'ordres doit correspondre au nombre de fichiers.");

            if (ordres != null && ordres.Count > 0)
            {
                if (ordres.Any(o => o < 1 || o > MaxPhotosPerLieu))
                {
                    throw new ArgumentException(
                        $"L'ordre doit être compris entre 1 et {MaxPhotosPerLieu}.");
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

        private async Task<SiteTouristiqueLieuPhoto> BuildPhotoEntityAsync(
            int idSiteTouristique,
            AddSiteTouristiqueLieuPhotoDto dto,
            IReadOnlyList<SiteTouristiqueLieuPhoto> activePhotos,
            CancellationToken cancellationToken)
        {
            var (bytes, _, contentType) = VehiculePhotoBase64Helper.ParseAndValidate(
                dto.PhotoBase64,
                dto.FileName);

            return await BuildPhotoEntityFromBytesAsync(
                idSiteTouristique,
                bytes,
                contentType,
                dto.FileName,
                dto.Ordre,
                activePhotos,
                cancellationToken);
        }

        private async Task<SiteTouristiqueLieuPhoto> BuildPhotoEntityFromBytesAsync(
            int idSiteTouristique,
            byte[] bytes,
            string contentType,
            string? fileName,
            int? requestedOrdre,
            IReadOnlyList<SiteTouristiqueLieuPhoto> activePhotos,
            CancellationToken cancellationToken)
        {
            var ordre = ResolveOrdre(requestedOrdre, activePhotos);

            string? storageKey = null;
            try
            {
                storageKey = await _blobStore.UploadAsync(
                    CongoTravelPhotoStorageKeys.EntitySitesTouristiques,
                    idSiteTouristique,
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
                    "Upload photo lieu touristique échoué — fallback BLOB. LieuId={LieuId}, Ordre={Ordre}",
                    idSiteTouristique,
                    ordre);
            }

            return new SiteTouristiqueLieuPhoto
            {
                IdSiteTouristique = idSiteTouristique,
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

        private static int ResolveOrdre(int? requestedOrdre, IReadOnlyList<SiteTouristiqueLieuPhoto> activePhotos)
        {
            if (requestedOrdre.HasValue)
            {
                if (requestedOrdre.Value < 1 || requestedOrdre.Value > MaxPhotosPerLieu)
                {
                    throw new ArgumentException(
                        $"L'ordre doit être compris entre 1 et {MaxPhotosPerLieu}.");
                }

                return requestedOrdre.Value;
            }

            var used = activePhotos.Select(p => p.Ordre).ToHashSet();
            for (var i = 1; i <= MaxPhotosPerLieu; i++)
            {
                if (!used.Contains(i))
                    return i;
            }

            throw new InvalidOperationException(
                $"Aucune position libre (maximum {MaxPhotosPerLieu} photos).");
        }
    }
}
