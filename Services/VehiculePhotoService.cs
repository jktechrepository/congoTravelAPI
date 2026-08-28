using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.PhotoStorage;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class VehiculePhotoService : IVehiculePhotoService
    {
        public const int MaxPhotosPerVehicule = 3;

        private readonly CongoTravelDbContext _context;
        private readonly ICongoTravelPhotoBlobStore _blobStore;
        private readonly IPhotoBinaryHydrator _hydrator;
        private readonly ILogger<VehiculePhotoService> _logger;

        public VehiculePhotoService(
            CongoTravelDbContext context,
            ICongoTravelPhotoBlobStore blobStore,
            IPhotoBinaryHydrator hydrator,
            ILogger<VehiculePhotoService> logger)
        {
            _context = context;
            _blobStore = blobStore;
            _hydrator = hydrator;
            _logger = logger;
        }

        public async Task<IReadOnlyList<PhotoVehicule>> GetByVehiculeIdAsync(
            int idVehicule,
            bool includePhotoBase64 = false)
        {
            var photos = await _context.PhotoVehicules
                .Where(p => p.IdVehicule == idVehicule && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync();

            if (includePhotoBase64)
                await _hydrator.HydratePhotoVehiculesAsync(photos);

            return photos;
        }

        public async Task<PhotoContentPayload?> GetContentAsync(
            int idVehicule,
            int idPhotoVehicule,
            CancellationToken cancellationToken = default)
        {
            var photo = await _context.PhotoVehicules
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.IdPhotoVehicule == idPhotoVehicule
                         && p.IdVehicule == idVehicule
                         && p.Statut,
                    cancellationToken);

            if (photo == null)
                return null;

            return await ResolveContentAsync(photo.PhotoData, photo.StorageKey, photo.TypeMIME, photo.OriginalFileName, cancellationToken);
        }

        public async Task AddPhotosOnCreateAsync(int idVehicule, IReadOnlyList<AddPhotoVehiculeDto>? photos)
        {
            if (photos == null || photos.Count == 0)
                return;

            await EnsureVehiculeExistsAsync(idVehicule);
            ValidatePhotoBatch(photos);

            var uploadedKeys = new List<string>();
            try
            {
                var active = new List<PhotoVehicule>();
                var entities = new List<PhotoVehicule>();
                foreach (var dto in photos)
                {
                    var entity = await BuildPhotoEntityAsync(idVehicule, dto, active);
                    if (!string.IsNullOrWhiteSpace(entity.StorageKey))
                        uploadedKeys.Add(entity.StorageKey);
                    entities.Add(entity);
                    active.Add(entity);
                }

                _context.PhotoVehicules.AddRange(entities);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Photos véhicule créées en lot - VehiculeId: {VehiculeId}, Nombre: {Count}",
                    idVehicule, entities.Count);
            }
            catch
            {
                await CompensateUploadedKeysAsync(uploadedKeys);
                throw;
            }
        }

        public async Task ReplaceAllPhotosOnUpdateAsync(int idVehicule, IReadOnlyList<AddPhotoVehiculeDto>? photos)
        {
            if (photos == null)
                return;

            await EnsureVehiculeExistsAsync(idVehicule);

            var existing = await _context.PhotoVehicules
                .Where(p => p.IdVehicule == idVehicule)
                .ToListAsync();
            var keysToDeleteAfterCommit = existing
                .Where(p => !string.IsNullOrWhiteSpace(p.StorageKey))
                .Select(p => p.StorageKey!)
                .ToList();

            var uploadedKeys = new List<string>();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (existing.Count > 0)
                {
                    _context.PhotoVehicules.RemoveRange(existing);
                    await _context.SaveChangesAsync();
                }

                if (photos.Count > 0)
                {
                    ValidatePhotoBatch(photos);
                    var active = new List<PhotoVehicule>();
                    var entities = new List<PhotoVehicule>();
                    foreach (var dto in photos)
                    {
                        var entity = await BuildPhotoEntityAsync(idVehicule, dto, active);
                        if (!string.IsNullOrWhiteSpace(entity.StorageKey))
                            uploadedKeys.Add(entity.StorageKey);
                        entities.Add(entity);
                        active.Add(entity);
                    }

                    _context.PhotoVehicules.AddRange(entities);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                foreach (var key in keysToDeleteAfterCommit)
                    await _blobStore.TryDeleteAsync(key);

                _logger.LogInformation(
                    "Photos véhicule remplacées - VehiculeId: {VehiculeId}, NouveauNombre: {Count}",
                    idVehicule, photos.Count);
            }
            catch
            {
                await transaction.RollbackAsync();
                await CompensateUploadedKeysAsync(uploadedKeys);
                throw;
            }
        }

        public async Task<IReadOnlyList<PhotoVehicule>> ReplaceAllFromFilesAsync(
            int idVehicule,
            IReadOnlyList<IFormFile> files,
            IReadOnlyList<int>? ordres = null,
            CancellationToken cancellationToken = default)
        {
            files ??= Array.Empty<IFormFile>();
            ValidateFileBatch(files, ordres);

            await EnsureVehiculeExistsAsync(idVehicule);

            var existing = await _context.PhotoVehicules
                .Where(p => p.IdVehicule == idVehicule)
                .ToListAsync(cancellationToken);
            var keysToDeleteAfterCommit = existing
                .Where(p => !string.IsNullOrWhiteSpace(p.StorageKey))
                .Select(p => p.StorageKey!)
                .ToList();

            var uploadedKeys = new List<string>();
            var entities = new List<PhotoVehicule>();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (existing.Count > 0)
                {
                    _context.PhotoVehicules.RemoveRange(existing);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                if (files.Count > 0)
                {
                    var active = new List<PhotoVehicule>();
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
                            idVehicule,
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

                    _context.PhotoVehicules.AddRange(entities);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                foreach (var key in keysToDeleteAfterCommit)
                    await _blobStore.TryDeleteAsync(key, cancellationToken);

                _logger.LogInformation(
                    "Photos véhicule remplacées (multipart) - VehiculeId: {VehiculeId}, NouveauNombre: {Count}",
                    idVehicule, entities.Count);

                return entities;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                await CompensateUploadedKeysAsync(uploadedKeys);
                throw;
            }
        }

        public async Task<PhotoVehicule> AddPhotoAsync(int idVehicule, AddPhotoVehiculeDto dto)
        {
            await EnsureVehiculeExistsAsync(idVehicule);

            var activePhotos = await _context.PhotoVehicules
                .Where(p => p.IdVehicule == idVehicule && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync();

            if (activePhotos.Count >= MaxPhotosPerVehicule)
                throw new InvalidOperationException($"Un véhicule ne peut pas avoir plus de {MaxPhotosPerVehicule} photos.");

            var photo = await BuildPhotoEntityAsync(idVehicule, dto, activePhotos);

            if (activePhotos.Any(p => p.Ordre == photo.Ordre))
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey);
                throw new InvalidOperationException($"La position {photo.Ordre} est déjà occupée pour ce véhicule.");
            }

            try
            {
                _context.PhotoVehicules.Add(photo);
                await _context.SaveChangesAsync();
            }
            catch
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey);
                throw;
            }

            _logger.LogInformation(
                "Photo véhicule ajoutée - VehiculeId: {VehiculeId}, PhotoId: {PhotoId}, Ordre: {Ordre}, StorageKey: {StorageKey}, Taille: {FileSize} o",
                idVehicule, photo.IdPhotoVehicule, photo.Ordre, photo.StorageKey, photo.FileSize);

            return photo;
        }

        public async Task<PhotoVehicule> AddPhotoFromFileAsync(
            int idVehicule,
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

            return await AddPhotoFromValidatedBytesAsync(
                idVehicule,
                bytes,
                contentType,
                resolvedFileName,
                ordre,
                cancellationToken);
        }

        private async Task<PhotoVehicule> AddPhotoFromValidatedBytesAsync(
            int idVehicule,
            byte[] bytes,
            string contentType,
            string? fileName,
            int? ordre,
            CancellationToken cancellationToken)
        {
            await EnsureVehiculeExistsAsync(idVehicule);

            var activePhotos = await _context.PhotoVehicules
                .Where(p => p.IdVehicule == idVehicule && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);

            if (activePhotos.Count >= MaxPhotosPerVehicule)
                throw new InvalidOperationException($"Un véhicule ne peut pas avoir plus de {MaxPhotosPerVehicule} photos.");

            var photo = await BuildPhotoEntityFromBytesAsync(
                idVehicule,
                bytes,
                contentType,
                fileName,
                ordre,
                activePhotos,
                cancellationToken);

            if (activePhotos.Any(p => p.Ordre == photo.Ordre))
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw new InvalidOperationException($"La position {photo.Ordre} est déjà occupée pour ce véhicule.");
            }

            try
            {
                _context.PhotoVehicules.Add(photo);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
                throw;
            }

            _logger.LogInformation(
                "Photo véhicule ajoutée (multipart) - VehiculeId: {VehiculeId}, PhotoId: {PhotoId}, Ordre: {Ordre}, StorageKey: {StorageKey}, Taille: {FileSize} o",
                idVehicule, photo.IdPhotoVehicule, photo.Ordre, photo.StorageKey, photo.FileSize);

            return photo;
        }

        public async Task<PhotoVehicule?> UpdateOrdreAsync(int idVehicule, int idPhotoVehicule, int ordre)
        {
            if (ordre < 1 || ordre > MaxPhotosPerVehicule)
                throw new ArgumentException($"L'ordre doit être compris entre 1 et {MaxPhotosPerVehicule}.");

            var photo = await _context.PhotoVehicules
                .FirstOrDefaultAsync(p => p.IdPhotoVehicule == idPhotoVehicule && p.IdVehicule == idVehicule && p.Statut);

            if (photo == null)
                return null;

            var conflict = await _context.PhotoVehicules
                .AnyAsync(p => p.IdVehicule == idVehicule && p.Ordre == ordre && p.IdPhotoVehicule != idPhotoVehicule && p.Statut);

            if (conflict)
                throw new InvalidOperationException($"La position {ordre} est déjà occupée pour ce véhicule.");

            photo.Ordre = ordre;
            photo.DateModification = DateTime.Now;
            await _context.SaveChangesAsync();

            return photo;
        }

        public async Task<bool> DeletePhotoAsync(int idVehicule, int idPhotoVehicule)
        {
            var photo = await _context.PhotoVehicules
                .FirstOrDefaultAsync(p => p.IdPhotoVehicule == idPhotoVehicule && p.IdVehicule == idVehicule);

            if (photo == null)
                return false;

            var storageKey = photo.StorageKey;
            _context.PhotoVehicules.Remove(photo);
            await _context.SaveChangesAsync();
            await _blobStore.TryDeleteAsync(storageKey);

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

        private async Task EnsureVehiculeExistsAsync(int idVehicule)
        {
            var exists = await _context.Vehicules.AnyAsync(v => v.IdVehicule == idVehicule);
            if (!exists)
                throw new ArgumentException($"Le véhicule avec l'ID {idVehicule} n'existe pas.");
        }

        private static void ValidatePhotoBatch(IReadOnlyList<AddPhotoVehiculeDto> photos)
        {
            if (photos.Count > MaxPhotosPerVehicule)
                throw new InvalidOperationException($"Un véhicule ne peut pas avoir plus de {MaxPhotosPerVehicule} photos.");

            var specifiedOrdres = photos
                .Where(p => p.Ordre.HasValue)
                .Select(p => p.Ordre!.Value)
                .ToList();

            if (specifiedOrdres.Any(o => o < 1 || o > MaxPhotosPerVehicule))
                throw new ArgumentException($"L'ordre doit être compris entre 1 et {MaxPhotosPerVehicule}.");

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
            if (files.Count > MaxPhotosPerVehicule)
                throw new InvalidOperationException($"Un véhicule ne peut pas avoir plus de {MaxPhotosPerVehicule} photos.");

            if (ordres != null && ordres.Count > 0 && ordres.Count != files.Count)
                throw new ArgumentException("Le nombre d'ordres doit correspondre au nombre de fichiers.");

            if (ordres != null && ordres.Count > 0)
            {
                if (ordres.Any(o => o < 1 || o > MaxPhotosPerVehicule))
                    throw new ArgumentException($"L'ordre doit être compris entre 1 et {MaxPhotosPerVehicule}.");
                if (ordres.Count != ordres.Distinct().Count())
                    throw new ArgumentException("Chaque photo doit avoir un ordre unique (1, 2 ou 3).");
            }

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("Chaque fichier photo doit être non vide.");
            }
        }

        private async Task<PhotoVehicule> BuildPhotoEntityAsync(
            int idVehicule,
            AddPhotoVehiculeDto dto,
            IReadOnlyList<PhotoVehicule> activePhotos)
        {
            var (bytes, _, contentType) = VehiculePhotoBase64Helper.ParseAndValidate(
                dto.PhotoBase64,
                dto.FileName);

            return await BuildPhotoEntityFromBytesAsync(
                idVehicule,
                bytes,
                contentType,
                dto.FileName,
                dto.Ordre,
                activePhotos);
        }

        private async Task<PhotoVehicule> BuildPhotoEntityFromBytesAsync(
            int idVehicule,
            byte[] bytes,
            string contentType,
            string? fileName,
            int? requestedOrdre,
            IReadOnlyList<PhotoVehicule> activePhotos,
            CancellationToken cancellationToken = default)
        {
            var ordre = ResolveOrdre(requestedOrdre, activePhotos);

            string? storageKey = null;
            try
            {
                storageKey = await _blobStore.UploadAsync(
                    CongoTravelPhotoStorageKeys.EntityVehicules,
                    idVehicule,
                    ordre,
                    bytes,
                    contentType,
                    fileName,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Dual-write : en cas d'échec stockage objet, on conserve le BLOB (pas de breaking).
                _logger.LogWarning(
                    ex,
                    "Upload photo véhicule échoué — fallback BLOB only. VehiculeId={VehiculeId}, Ordre={Ordre}",
                    idVehicule,
                    ordre);
            }

            return new PhotoVehicule
            {
                IdVehicule = idVehicule,
                PhotoData = bytes,
                StorageKey = storageKey,
                Ordre = ordre,
                OriginalFileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim(),
                TypeMIME = contentType,
                FileSize = bytes.Length,
                Statut = true,
                DateCreation = DateTime.Now
            };
        }

        private async Task CompensateUploadedKeysAsync(IEnumerable<string> keys)
        {
            foreach (var key in keys)
                await _blobStore.TryDeleteAsync(key);
        }

        private static int ResolveOrdre(int? requestedOrdre, IReadOnlyList<PhotoVehicule> activePhotos)
        {
            if (requestedOrdre.HasValue)
            {
                if (requestedOrdre.Value < 1 || requestedOrdre.Value > MaxPhotosPerVehicule)
                    throw new ArgumentException($"L'ordre doit être compris entre 1 et {MaxPhotosPerVehicule}.");
                return requestedOrdre.Value;
            }

            var used = activePhotos.Select(p => p.Ordre).ToHashSet();
            for (var i = 1; i <= MaxPhotosPerVehicule; i++)
            {
                if (!used.Contains(i))
                    return i;
            }

            throw new InvalidOperationException($"Aucune position libre (maximum {MaxPhotosPerVehicule} photos).");
        }
    }
}
