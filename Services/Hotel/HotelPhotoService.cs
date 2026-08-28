using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Services.PhotoStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelPhotoService : IHotelPhotoService
    {
        public const int MaxPhotosPerHotel = 3;
        private readonly CongoTravelDbContext _context;
        private readonly ICongoTravelPhotoBlobStore _blobStore;
        private readonly IPhotoBinaryHydrator _hydrator;
        private readonly ILogger<HotelPhotoService> _logger;

        public HotelPhotoService(CongoTravelDbContext context, ICongoTravelPhotoBlobStore blobStore,
            IPhotoBinaryHydrator hydrator, ILogger<HotelPhotoService> logger)
        {
            _context = context; _blobStore = blobStore; _hydrator = hydrator; _logger = logger;
        }

        public async Task<IReadOnlyList<HotelPhoto>> GetByHotelIdAsync(int idHotel, int idSociete,
            CancellationToken cancellationToken = default, bool includePhotoBase64 = false)
        {
            await EnsureHotelExistsAsync(idHotel, idSociete, cancellationToken);
            var photos = await _context.HotelPhotos.AsNoTracking().Where(p => p.IdHotel == idHotel && p.Statut)
                .OrderBy(p => p.Ordre).ToListAsync(cancellationToken);
            if (includePhotoBase64) await _hydrator.HydrateHotelPhotosAsync(photos, cancellationToken);
            return photos;
        }

        public async Task<PhotoContentPayload?> GetContentAsync(int idHotel, int idSociete, int idHotelPhoto,
            CancellationToken cancellationToken = default)
        {
            await EnsureHotelExistsAsync(idHotel, idSociete, cancellationToken);
            var photo = await _context.HotelPhotos.AsNoTracking().FirstOrDefaultAsync(p =>
                p.IdHotelPhoto == idHotelPhoto && p.IdHotel == idHotel && p.Statut, cancellationToken);
            if (photo == null) return null;
            var bytes = photo.PhotoData is { Length: > 0 } ? photo.PhotoData :
                string.IsNullOrWhiteSpace(photo.StorageKey) ? null : await _blobStore.GetBytesAsync(photo.StorageKey, cancellationToken);
            return bytes == null ? null : new PhotoContentPayload
            {
                Content = bytes, ContentType = PhotoContentHelper.ResolveContentType(photo.TypeMIME), FileName = photo.OriginalFileName
            };
        }

        public async Task AddPhotosOnCreateAsync(int idHotel, int idSociete, IReadOnlyList<AddHotelPhotoDto>? photos,
            CancellationToken cancellationToken = default)
        {
            if (photos == null || photos.Count == 0) return;
            if (photos.Count > MaxPhotosPerHotel) throw new InvalidOperationException("Un hôtel ne peut pas avoir plus de 3 photos.");
            if (photos.Where(p => p.Ordre.HasValue).Select(p => p.Ordre).Distinct().Count() != photos.Count(p => p.Ordre.HasValue))
                throw new ArgumentException("Chaque photo doit avoir un ordre unique.");
            foreach (var photo in photos) await AddPhotoAsync(idHotel, idSociete, photo, cancellationToken);
        }

        public async Task<HotelPhoto> AddPhotoAsync(int idHotel, int idSociete, AddHotelPhotoDto dto,
            CancellationToken cancellationToken = default)
        {
            var (bytes, _, contentType) = VehiculePhotoBase64Helper.ParseAndValidate(dto.PhotoBase64, dto.FileName);
            return await AddBytesAsync(idHotel, idSociete, bytes, contentType, dto.FileName, dto.Ordre, cancellationToken);
        }

        public async Task<HotelPhoto> AddPhotoFromFileAsync(int idHotel, int idSociete, IFormFile file, int? ordre = null,
            string? fileName = null, CancellationToken cancellationToken = default)
        {
            var name = string.IsNullOrWhiteSpace(fileName) ? file.FileName : fileName;
            var (bytes, _, contentType) = await VehiculePhotoBase64Helper.ParseAndValidateFileAsync(file, name, cancellationToken);
            return await AddBytesAsync(idHotel, idSociete, bytes, contentType, name, ordre, cancellationToken);
        }

        public async Task<IReadOnlyList<HotelPhoto>> ReplaceAllFromFilesAsync(int idHotel, int idSociete,
            IReadOnlyList<IFormFile> files, IReadOnlyList<int>? ordres = null, CancellationToken cancellationToken = default)
        {
            if (files.Count > MaxPhotosPerHotel) throw new InvalidOperationException("Un hôtel ne peut pas avoir plus de 3 photos.");
            if (ordres != null && ordres.Count != files.Count) throw new ArgumentException("Le nombre d'ordres doit correspondre au nombre de fichiers.");
            await EnsureHotelExistsAsync(idHotel, idSociete, cancellationToken);
            var existing = await _context.HotelPhotos.Where(p => p.IdHotel == idHotel).ToListAsync(cancellationToken);
            _context.HotelPhotos.RemoveRange(existing);
            await _context.SaveChangesAsync(cancellationToken);
            foreach (var photo in existing) await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
            var result = new List<HotelPhoto>();
            for (var i = 0; i < files.Count; i++)
                result.Add(await AddPhotoFromFileAsync(idHotel, idSociete, files[i], ordres?[i], cancellationToken: cancellationToken));
            return result;
        }

        public async Task<HotelPhoto?> UpdateOrdreAsync(int idHotel, int idSociete, int idHotelPhoto, int ordre,
            CancellationToken cancellationToken = default)
        {
            ValidateOrdre(ordre);
            await EnsureHotelExistsAsync(idHotel, idSociete, cancellationToken);
            var photo = await _context.HotelPhotos.FirstOrDefaultAsync(p => p.IdHotelPhoto == idHotelPhoto && p.IdHotel == idHotel, cancellationToken);
            if (photo == null) return null;
            if (await _context.HotelPhotos.AnyAsync(p => p.IdHotel == idHotel && p.Ordre == ordre && p.IdHotelPhoto != idHotelPhoto, cancellationToken))
                throw new InvalidOperationException($"La position {ordre} est déjà occupée pour cet hôtel.");
            photo.Ordre = ordre; photo.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return photo;
        }

        public async Task<bool> DeletePhotoAsync(int idHotel, int idSociete, int idHotelPhoto, CancellationToken cancellationToken = default)
        {
            await EnsureHotelExistsAsync(idHotel, idSociete, cancellationToken);
            var photo = await _context.HotelPhotos.FirstOrDefaultAsync(p => p.IdHotelPhoto == idHotelPhoto && p.IdHotel == idHotel, cancellationToken);
            if (photo == null) return false;
            _context.HotelPhotos.Remove(photo); await _context.SaveChangesAsync(cancellationToken);
            await _blobStore.TryDeleteAsync(photo.StorageKey, cancellationToken);
            return true;
        }

        private async Task<HotelPhoto> AddBytesAsync(int idHotel, int idSociete, byte[] bytes, string contentType,
            string? fileName, int? requestedOrdre, CancellationToken cancellationToken)
        {
            await EnsureHotelExistsAsync(idHotel, idSociete, cancellationToken);
            var active = await _context.HotelPhotos.Where(p => p.IdHotel == idHotel && p.Statut).ToListAsync(cancellationToken);
            if (active.Count >= MaxPhotosPerHotel) throw new InvalidOperationException("Un hôtel ne peut pas avoir plus de 3 photos.");
            var ordre = requestedOrdre ?? Enumerable.Range(1, 3).First(i => active.All(p => p.Ordre != i));
            ValidateOrdre(ordre);
            if (active.Any(p => p.Ordre == ordre)) throw new InvalidOperationException($"La position {ordre} est déjà occupée pour cet hôtel.");
            string? key = null;
            try { key = await _blobStore.UploadAsync(CongoTravelPhotoStorageKeys.EntityHotels, idHotel, ordre, bytes, contentType, fileName, cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Upload photo hôtel échoué — fallback BLOB."); }
            var photo = new HotelPhoto
            {
                IdHotel = idHotel, PhotoData = bytes, StorageKey = key, Ordre = ordre,
                OriginalFileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim(),
                TypeMIME = contentType, FileSize = bytes.Length, Statut = true, DateCreation = DateTime.UtcNow
            };
            _context.HotelPhotos.Add(photo); await _context.SaveChangesAsync(cancellationToken);
            return photo;
        }

        private async Task EnsureHotelExistsAsync(int idHotel, int idSociete, CancellationToken cancellationToken)
        {
            if (!await _context.Hotels.AsNoTracking().AnyAsync(h => h.IdHotel == idHotel && h.IdSociete == idSociete, cancellationToken))
                throw new KeyNotFoundException($"Hôtel {idHotel} introuvable pour la société {idSociete}.");
        }
        private static void ValidateOrdre(int ordre)
        {
            if (ordre is < 1 or > 3) throw new ArgumentException("L'ordre doit être compris entre 1 et 3.");
        }
    }
}
