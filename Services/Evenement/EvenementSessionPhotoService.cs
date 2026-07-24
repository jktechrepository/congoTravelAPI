using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;

namespace CongoTravel.Services.Evenement
{
    public class EvenementSessionPhotoService : IEvenementSessionPhotoService
    {
        public const int MaxPhotosPerSession = 3;

        private readonly CongoTravelDbContext _context;
        private readonly ILogger<EvenementSessionPhotoService> _logger;

        public EvenementSessionPhotoService(
            CongoTravelDbContext context,
            ILogger<EvenementSessionPhotoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<EvenementSessionPhoto>> GetBySessionIdAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            await EnsureSessionExistsAsync(idEvenementSession, idSociete, cancellationToken);

            return await _context.EvenementSessionPhotos
                .AsNoTracking()
                .Where(p => p.IdEvenementSession == idEvenementSession && p.Statut)
                .OrderBy(p => p.Ordre)
                .ToListAsync(cancellationToken);
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

            var active = new List<EvenementSessionPhoto>();
            var entities = new List<EvenementSessionPhoto>();
            foreach (var dto in photos)
            {
                var entity = BuildPhotoEntity(idEvenementSession, dto, active);
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

            var photo = BuildPhotoEntity(idEvenementSession, dto, activePhotos);

            if (activePhotos.Any(p => p.Ordre == photo.Ordre))
            {
                throw new InvalidOperationException(
                    $"La position {photo.Ordre} est déjà occupée pour cette session.");
            }

            _context.EvenementSessionPhotos.Add(photo);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Photo session événement ajoutée — SessionId={SessionId}, PhotoId={PhotoId}, Ordre={Ordre}, Taille={FileSize}",
                idEvenementSession,
                photo.IdEvenementSessionPhoto,
                photo.Ordre,
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

            _context.EvenementSessionPhotos.Remove(photo);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
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

        private static EvenementSessionPhoto BuildPhotoEntity(
            int idEvenementSession,
            AddEvenementSessionPhotoDto dto,
            IReadOnlyList<EvenementSessionPhoto> activePhotos)
        {
            var ordre = ResolveOrdre(dto.Ordre, activePhotos);

            var (bytes, _, contentType) = VehiculePhotoBase64Helper.ParseAndValidate(
                dto.PhotoBase64,
                dto.FileName);

            return new EvenementSessionPhoto
            {
                IdEvenementSession = idEvenementSession,
                PhotoData = bytes,
                Ordre = ordre,
                OriginalFileName = string.IsNullOrWhiteSpace(dto.FileName) ? null : dto.FileName.Trim(),
                TypeMIME = contentType,
                FileSize = bytes.Length,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
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
