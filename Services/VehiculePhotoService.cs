using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class VehiculePhotoService : IVehiculePhotoService
    {
        public const int MaxPhotosPerVehicule = 3;

        private readonly CongoTravelDbContext _context;
        private readonly ILogger<VehiculePhotoService> _logger;

        public VehiculePhotoService(
            CongoTravelDbContext context,
            ILogger<VehiculePhotoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<PhotoVehicule>> GetByVehiculeIdAsync(int idVehicule)
        {
            return await _context.PhotoVehicules
                .Where(p => p.IdVehicule == idVehicule && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync();
        }

        public async Task AddPhotosOnCreateAsync(int idVehicule, IReadOnlyList<AddPhotoVehiculeDto>? photos)
        {
            if (photos == null || photos.Count == 0)
                return;

            await EnsureVehiculeExistsAsync(idVehicule);
            ValidatePhotoBatch(photos);

            var active = new List<PhotoVehicule>();
            var entities = new List<PhotoVehicule>();
            foreach (var dto in photos)
            {
                var entity = BuildPhotoEntity(idVehicule, dto, active);
                entities.Add(entity);
                active.Add(entity);
            }

            _context.PhotoVehicules.AddRange(entities);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Photos véhicule créées en lot - VehiculeId: {VehiculeId}, Nombre: {Count}",
                idVehicule, entities.Count);
        }

        public async Task ReplaceAllPhotosOnUpdateAsync(int idVehicule, IReadOnlyList<AddPhotoVehiculeDto>? photos)
        {
            if (photos == null)
                return;

            await EnsureVehiculeExistsAsync(idVehicule);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existing = await _context.PhotoVehicules
                    .Where(p => p.IdVehicule == idVehicule)
                    .ToListAsync();

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
                        var entity = BuildPhotoEntity(idVehicule, dto, active);
                        entities.Add(entity);
                        active.Add(entity);
                    }

                    _context.PhotoVehicules.AddRange(entities);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Photos véhicule remplacées - VehiculeId: {VehiculeId}, NouveauNombre: {Count}",
                    idVehicule, photos.Count);
            }
            catch
            {
                await transaction.RollbackAsync();
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

            var photo = BuildPhotoEntity(idVehicule, dto, activePhotos);

            if (activePhotos.Any(p => p.Ordre == photo.Ordre))
                throw new InvalidOperationException($"La position {photo.Ordre} est déjà occupée pour ce véhicule.");

            _context.PhotoVehicules.Add(photo);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Photo véhicule ajoutée (MEDIUMBLOB) - VehiculeId: {VehiculeId}, PhotoId: {PhotoId}, Ordre: {Ordre}, Taille: {FileSize} o",
                idVehicule, photo.IdPhotoVehicule, photo.Ordre, photo.FileSize);

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

            _context.PhotoVehicules.Remove(photo);
            await _context.SaveChangesAsync();

            return true;
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

        private static PhotoVehicule BuildPhotoEntity(
            int idVehicule,
            AddPhotoVehiculeDto dto,
            IReadOnlyList<PhotoVehicule> activePhotos)
        {
            var ordre = ResolveOrdre(dto.Ordre, activePhotos);

            var (bytes, _, contentType) = VehiculePhotoBase64Helper.ParseAndValidate(
                dto.PhotoBase64,
                dto.FileName);

            return new PhotoVehicule
            {
                IdVehicule = idVehicule,
                PhotoData = bytes,
                Ordre = ordre,
                OriginalFileName = string.IsNullOrWhiteSpace(dto.FileName) ? null : dto.FileName.Trim(),
                TypeMIME = contentType,
                FileSize = bytes.Length,
                Statut = true,
                DateCreation = DateTime.Now
            };
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
