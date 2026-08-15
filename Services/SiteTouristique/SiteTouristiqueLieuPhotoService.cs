using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueLieuPhotoService : ISiteTouristiqueLieuPhotoService
    {
        public const int MaxPhotosPerLieu = 3;

        private readonly CongoTravelDbContext _context;
        private readonly ILogger<SiteTouristiqueLieuPhotoService> _logger;

        public SiteTouristiqueLieuPhotoService(
            CongoTravelDbContext context,
            ILogger<SiteTouristiqueLieuPhotoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<SiteTouristiqueLieuPhoto>> GetByLieuIdAsync(
            int idSiteTouristique,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            await EnsureLieuExistsAsync(idSiteTouristique, idSociete, cancellationToken);

            return await _context.SiteTouristiqueLieuPhotos
                .AsNoTracking()
                .Where(p => p.IdSiteTouristique == idSiteTouristique && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);
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

            var active = new List<SiteTouristiqueLieuPhoto>();
            var entities = new List<SiteTouristiqueLieuPhoto>();
            foreach (var dto in photos)
            {
                var entity = BuildPhotoEntity(idSiteTouristique, dto, active);
                entities.Add(entity);
                active.Add(entity);
            }

            _context.SiteTouristiqueLieuPhotos.AddRange(entities);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Photos lieu site touristique créées en lot — LieuId={LieuId}, Nombre={Count}",
                idSiteTouristique,
                entities.Count);
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

            var photo = BuildPhotoEntity(idSiteTouristique, dto, activePhotos);

            if (activePhotos.Any(p => p.Ordre == photo.Ordre))
            {
                throw new InvalidOperationException(
                    $"La position {photo.Ordre} est déjà occupée pour ce lieu.");
            }

            _context.SiteTouristiqueLieuPhotos.Add(photo);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Photo lieu site touristique ajoutée — LieuId={LieuId}, PhotoId={PhotoId}, Ordre={Ordre}, Taille={FileSize}",
                idSiteTouristique,
                photo.IdSiteTouristiqueLieuPhoto,
                photo.Ordre,
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

            _context.SiteTouristiqueLieuPhotos.Remove(photo);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
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

        private static SiteTouristiqueLieuPhoto BuildPhotoEntity(
            int idSiteTouristique,
            AddSiteTouristiqueLieuPhotoDto dto,
            IReadOnlyList<SiteTouristiqueLieuPhoto> activePhotos)
        {
            var ordre = ResolveOrdre(dto.Ordre, activePhotos);

            var (bytes, _, contentType) = VehiculePhotoBase64Helper.ParseAndValidate(
                dto.PhotoBase64,
                dto.FileName);

            return new SiteTouristiqueLieuPhoto
            {
                IdSiteTouristique = idSiteTouristique,
                PhotoData = bytes,
                Ordre = ordre,
                OriginalFileName = string.IsNullOrWhiteSpace(dto.FileName) ? null : dto.FileName.Trim(),
                TypeMIME = contentType,
                FileSize = bytes.Length,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
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
