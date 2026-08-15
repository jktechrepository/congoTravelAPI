using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantPhotoService : IRestaurantPhotoService
    {
        public const int MaxPhotosPerRestaurant = 3;

        private readonly CongoTravelDbContext _context;
        private readonly ILogger<RestaurantPhotoService> _logger;

        public RestaurantPhotoService(
            CongoTravelDbContext context,
            ILogger<RestaurantPhotoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<RestaurantPhoto>> GetByRestaurantIdAsync(
            int idRestaurant,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            await EnsureRestaurantExistsAsync(idRestaurant, idSociete, cancellationToken);

            return await _context.RestaurantPhotos
                .AsNoTracking()
                .Where(p => p.IdRestaurant == idRestaurant && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);
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

            var active = new List<RestaurantPhoto>();
            var entities = new List<RestaurantPhoto>();
            foreach (var dto in photos)
            {
                var entity = BuildPhotoEntity(idRestaurant, dto, active);
                entities.Add(entity);
                active.Add(entity);
            }

            _context.RestaurantPhotos.AddRange(entities);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Photos établissement restaurant créées en lot — RestaurantId={RestaurantId}, Nombre={Count}",
                idRestaurant,
                entities.Count);
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

            var photo = BuildPhotoEntity(idRestaurant, dto, activePhotos);

            if (activePhotos.Any(p => p.Ordre == photo.Ordre))
            {
                throw new InvalidOperationException(
                    $"La position {photo.Ordre} est déjà occupée pour cet établissement.");
            }

            _context.RestaurantPhotos.Add(photo);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Photo établissement restaurant ajoutée — RestaurantId={RestaurantId}, PhotoId={PhotoId}, Ordre={Ordre}, Taille={FileSize}",
                idRestaurant,
                photo.IdRestaurantPhoto,
                photo.Ordre,
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

            _context.RestaurantPhotos.Remove(photo);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
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

        private static RestaurantPhoto BuildPhotoEntity(
            int idRestaurant,
            AddRestaurantPhotoDto dto,
            IReadOnlyList<RestaurantPhoto> activePhotos)
        {
            var ordre = ResolveOrdre(dto.Ordre, activePhotos);

            var (bytes, _, contentType) = VehiculePhotoBase64Helper.ParseAndValidate(
                dto.PhotoBase64,
                dto.FileName);

            return new RestaurantPhoto
            {
                IdRestaurant = idRestaurant,
                PhotoData = bytes,
                Ordre = ordre,
                OriginalFileName = string.IsNullOrWhiteSpace(dto.FileName) ? null : dto.FileName.Trim(),
                TypeMIME = contentType,
                FileSize = bytes.Length,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
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
