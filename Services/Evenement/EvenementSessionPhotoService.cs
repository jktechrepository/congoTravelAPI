using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Services.PhotoStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Evenement
{
    public class EvenementSessionPhotoService : IEvenementSessionPhotoService
    {
        public const int MaxPhotosPerSession = 3;

        private readonly CongoTravelDbContext _context;
        private readonly ICongoTravelPhotoBlobStore _blobStore;
        private readonly IPhotoBinaryHydrator _hydrator;
        private readonly ILogger<EvenementSessionPhotoService> _logger;

        public EvenementSessionPhotoService(
            CongoTravelDbContext context,
            ICongoTravelPhotoBlobStore blobStore,
            IPhotoBinaryHydrator hydrator,
            ILogger<EvenementSessionPhotoService> logger)
        {
            _context = context;
            _blobStore = blobStore;
            _hydrator = hydrator;
            _logger = logger;
        }

        public async Task<IReadOnlyList<EvenementSessionPhoto>> GetBySessionIdAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false)
        {
            await EnsureSessionExistsAsync(idEvenementSession, idSociete, cancellationToken);

            var photos = await _context.EvenementSessionPhotos
                .AsNoTracking()
                .Where(p => p.IdEvenementSession == idEvenementSession && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);

            if (includePhotoBase64)
                await _hydrator.HydrateEvenementSessionPhotosAsync(photos, cancellationToken);

            return photos;
        }

        public async Task<PhotoContentPayload?> GetContentAsync(
            int idEvenementSession,
            int idSociete,
            int idEvenementSessionPhoto,
            CancellationToken cancellationToken = default)
        {
            await EnsureSessionExistsAsync(idEvenementSession, idSociete, cancellationToken);

            var photo = await _context.EvenementSessionPhotos
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.IdEvenementSessionPhoto == idEvenementSessionPhoto
                         && p.IdEvenementSession == idEvenementSession
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
            int idEvenementSession,
            int idSociete,
            IReadOnlyList<AddEvenementSessionPhotoDto>? photos,
            CancellationToken cancellationToken = default)
        {
            if (photos == null || photos.Count == 0)
                return;

            await EnsureSessionExistsAsync(idEvenementSession, idSociete, cancellationToken);
            ValidatePhotoBatch(photos);

            var uploadedKeys = new List<string>();
            try
            {
                var active = new List<EvenementSessionPhoto>();
                var entities = new List<EvenementSessionPhoto>();
                foreach (var dto in photos)
                {
                    var entity = await BuildPhotoEntityAsync(idEvenementSession, dto, active, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(entity.StorageKey))
                        uploadedKeys.Add(entity.StorageKey);
                    entities.Add(entity);
                    active.Add(entity);
                }

                _context.EvenementSessionPhotos.AddRange(entities);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Photos session événement créées en lot — SessionId={SessionId}, Nombre={Count}",
                    idEvenementSession,
                    entities.Count);
            }
            catch
            {
                await CompensateUploadedKeysAsync(uploadedKeys, cancellationToken);
                throw;
            }
        }

        public async Task<IReadOnlyList<EvenementSessionPhoto>> ReplaceAllFromFilesAsync(
            int idEvenementSession,
            int idSociete,
            IReadOnlyList<IFormFile> files,
            IReadOnlyList<int>? ordres = null,
            CancellationToken cancellationToken = default)
        {
            files ??= Array.Empty<IFormFile>();
            ValidateFileBatch(files, ordres);
            await EnsureSessionExistsAsync(idEvenementSession, idSociete, cancellationToken);

            var existing = await _context.EvenementSessionPhotos
                .Where(p => p.IdEvenementSession == idEvenementSession)
                .ToListAsync(cancellationToken);
            var keysToDeleteAfterCommit = existing
                .Where(p => !string.IsNullOrWhiteSpace(p.StorageKey))
                .Select(p => p.StorageKey!)
                .ToList();

            var uploadedKeys = new List<string>();
            var entities = new List<EvenementSessionPhoto>();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (existing.Count > 0)
                {
                    _context.EvenementSessionPhotos.RemoveRange(existing);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                if (files.Count > 0)
                {
                    var active = new List<EvenementSessionPhoto>();
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
                            idEvenementSession,
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

                    _context.EvenementSessionPhotos.AddRange(entities);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                foreach (var key in keysToDeleteAfterCommit)
                    await _blobStore.TryDeleteAsync(key, cancellationToken);

                _logger.LogInformation(
                    "Photos session événement remplacées (multipart) — SessionId={SessionId}, Nombre={Count}",
                    idEvenementSession,
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

        public async Task<EvenementSessionPhoto> AddPhotoAsync(
            int idEvenementSession,
            int idSociete,
            AddEvenementSessionPhotoDto dto,
            CancellationToken cancellationToken = default)
        {
            await EnsureSessionExistsAsync(idEvenementSession, idSociete, cancellationToken);

            var activePhotos = await _context.EvenementSessionPhotos
                .Where(p => p.IdEvenementSession == idEvenementSession && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);

            if (activePhotos.Count >= MaxPhotosPerSession)
            {
                throw new InvalidOperationException(
                    $"Une session événement ne peut pas avoir plus de {MaxPhotosPerSession} photos.");
            }

            var photo = await BuildPhotoEntityAsync(idEvenementSession, dto, activePhotos, cancellationToken);

            if (activePhotos.Any(p => p.Ordre == photo.Ordre))
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw new InvalidOperationException(
                    $"La position {photo.Ordre} est déjà occupée pour cette session.");
            }

            try
            {
                _context.EvenementSessionPhotos.Add(photo);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw;
            }

            _logger.LogInformation(
                "Photo session événement ajoutée — SessionId={SessionId}, PhotoId={PhotoId}, Ordre={Ordre}, StorageKey={StorageKey}, Taille={FileSize}",
                idEvenementSession,
                photo.IdEvenementSessionPhoto,
                photo.Ordre,
                photo.StorageKey,
                photo.FileSize);

            return photo;
        }

        public async Task<EvenementSessionPhoto> AddPhotoFromFileAsync(
            int idEvenementSession,
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

            await EnsureSessionExistsAsync(idEvenementSession, idSociete, cancellationToken);

            var activePhotos = await _context.EvenementSessionPhotos
                .Where(p => p.IdEvenementSession == idEvenementSession && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);

            if (activePhotos.Count >= MaxPhotosPerSession)
            {
                throw new InvalidOperationException(
                    $"Une session événement ne peut pas avoir plus de {MaxPhotosPerSession} photos.");
            }

            var photo = await BuildPhotoEntityFromBytesAsync(
                idEvenementSession,
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
                    $"La position {photo.Ordre} est déjà occupée pour cette session.");
            }

            try
            {
                _context.EvenementSessionPhotos.Add(photo);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw;
            }

            _logger.LogInformation(
                "Photo session événement ajoutée (multipart) — SessionId={SessionId}, PhotoId={PhotoId}, Ordre={Ordre}, StorageKey={StorageKey}, Taille={FileSize}",
                idEvenementSession,
                photo.IdEvenementSessionPhoto,
                photo.Ordre,
                photo.StorageKey,
                photo.FileSize);

            return photo;
        }

        public async Task<EvenementSessionPhoto?> UpdateOrdreAsync(
            int idEvenementSession,
            int idSociete,
            int idEvenementSessionPhoto,
            int ordre,
            CancellationToken cancellationToken = default)
        {
            if (ordre < 1 || ordre > MaxPhotosPerSession)
            {
                throw new ArgumentException(
                    $"L'ordre doit être compris entre 1 et {MaxPhotosPerSession}.");
            }

            await EnsureSessionExistsAsync(idEvenementSession, idSociete, cancellationToken);

            var photo = await _context.EvenementSessionPhotos
                .FirstOrDefaultAsync(
                    p => p.IdEvenementSessionPhoto == idEvenementSessionPhoto
                         && p.IdEvenementSession == idEvenementSession
                         && p.Statut,
                    cancellationToken);

            if (photo == null)
                return null;

            var conflict = await _context.EvenementSessionPhotos
                .AnyAsync(
                    p => p.IdEvenementSession == idEvenementSession
                         && p.Ordre == ordre
                         && p.IdEvenementSessionPhoto != idEvenementSessionPhoto
                         && p.Statut,
                    cancellationToken);

            if (conflict)
            {
                throw new InvalidOperationException(
                    $"La position {ordre} est déjà occupée pour cette session.");
            }

            photo.Ordre = ordre;
            photo.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return photo;
        }

        public async Task<bool> DeletePhotoAsync(
            int idEvenementSession,
            int idSociete,
            int idEvenementSessionPhoto,
            CancellationToken cancellationToken = default)
        {
            await EnsureSessionExistsAsync(idEvenementSession, idSociete, cancellationToken);

            var photo = await _context.EvenementSessionPhotos
                .FirstOrDefaultAsync(
                    p => p.IdEvenementSessionPhoto == idEvenementSessionPhoto
                         && p.IdEvenementSession == idEvenementSession,
                    cancellationToken);

            if (photo == null)
                return false;

            var storageKey = photo.StorageKey;
            _context.EvenementSessionPhotos.Remove(photo);
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

        private async Task EnsureSessionExistsAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var exists = await _context.EvenementSessions
                .AsNoTracking()
                .AnyAsync(
                    s => s.IdEvenementSession == idEvenementSession && s.IdSociete == idSociete,
                    cancellationToken);

            if (!exists)
            {
                throw new KeyNotFoundException(
                    $"Session événement {idEvenementSession} introuvable pour la société {idSociete}.");
            }
        }

        private static void ValidatePhotoBatch(IReadOnlyList<AddEvenementSessionPhotoDto> photos)
        {
            if (photos.Count > MaxPhotosPerSession)
            {
                throw new InvalidOperationException(
                    $"Une session événement ne peut pas avoir plus de {MaxPhotosPerSession} photos.");
            }

            var specifiedOrdres = photos
                .Where(p => p.Ordre.HasValue)
                .Select(p => p.Ordre!.Value)
                .ToList();

            if (specifiedOrdres.Any(o => o < 1 || o > MaxPhotosPerSession))
            {
                throw new ArgumentException(
                    $"L'ordre doit être compris entre 1 et {MaxPhotosPerSession}.");
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
            if (files.Count > MaxPhotosPerSession)
            {
                throw new InvalidOperationException(
                    $"Une session événement ne peut pas avoir plus de {MaxPhotosPerSession} photos.");
            }

            if (ordres != null && ordres.Count > 0 && ordres.Count != files.Count)
                throw new ArgumentException("Le nombre d'ordres doit correspondre au nombre de fichiers.");

            if (ordres != null && ordres.Count > 0)
            {
                if (ordres.Any(o => o < 1 || o > MaxPhotosPerSession))
                {
                    throw new ArgumentException(
                        $"L'ordre doit être compris entre 1 et {MaxPhotosPerSession}.");
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

        private async Task<EvenementSessionPhoto> BuildPhotoEntityAsync(
            int idEvenementSession,
            AddEvenementSessionPhotoDto dto,
            IReadOnlyList<EvenementSessionPhoto> activePhotos,
            CancellationToken cancellationToken)
        {
            var (bytes, _, contentType) = VehiculePhotoBase64Helper.ParseAndValidate(
                dto.PhotoBase64,
                dto.FileName);

            return await BuildPhotoEntityFromBytesAsync(
                idEvenementSession,
                bytes,
                contentType,
                dto.FileName,
                dto.Ordre,
                activePhotos,
                cancellationToken);
        }

        private async Task<EvenementSessionPhoto> BuildPhotoEntityFromBytesAsync(
            int idEvenementSession,
            byte[] bytes,
            string contentType,
            string? fileName,
            int? requestedOrdre,
            IReadOnlyList<EvenementSessionPhoto> activePhotos,
            CancellationToken cancellationToken)
        {
            var ordre = ResolveOrdre(requestedOrdre, activePhotos);

            string? storageKey = null;
            try
            {
                storageKey = await _blobStore.UploadAsync(
                    CongoTravelPhotoStorageKeys.EntityEvenementSessions,
                    idEvenementSession,
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
                    "Upload photo session événement échoué — fallback BLOB. SessionId={SessionId}, Ordre={Ordre}",
                    idEvenementSession,
                    ordre);
            }

            return new EvenementSessionPhoto
            {
                IdEvenementSession = idEvenementSession,
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

        private static int ResolveOrdre(int? requestedOrdre, IReadOnlyList<EvenementSessionPhoto> activePhotos)
        {
            if (requestedOrdre.HasValue)
            {
                if (requestedOrdre.Value < 1 || requestedOrdre.Value > MaxPhotosPerSession)
                {
                    throw new ArgumentException(
                        $"L'ordre doit être compris entre 1 et {MaxPhotosPerSession}.");
                }

                return requestedOrdre.Value;
            }

            var used = activePhotos.Select(p => p.Ordre).ToHashSet();
            for (var i = 1; i <= MaxPhotosPerSession; i++)
            {
                if (!used.Contains(i))
                    return i;
            }

            throw new InvalidOperationException(
                $"Aucune position libre (maximum {MaxPhotosPerSession} photos).");
        }
    }
}
