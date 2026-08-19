using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueLieuService : ISiteTouristiqueLieuService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ISiteTouristiqueLieuPhotoService _photoService;
        private readonly ILogger<SiteTouristiqueLieuService> _logger;

        public SiteTouristiqueLieuService(
            CongoTravelDbContext context,
            ISiteTouristiqueLieuPhotoService photoService,
            ILogger<SiteTouristiqueLieuService> logger)
        {
            _context = context;
            _photoService = photoService;
            _logger = logger;
        }

        public async Task<SiteTouristiqueLieuResponseDto> CreateDraftAsync(
            SiteTouristiqueCreateLieuRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.CodeLieu))
                throw new InvalidOperationException("CodeLieu est obligatoire.");
            if (string.IsNullOrWhiteSpace(request.Nom))
                throw new InvalidOperationException("Nom est obligatoire.");
            if (request.IdSite <= 0)
                throw new InvalidOperationException("IdSite est obligatoire pour créer un lieu touristique.");

            EnsureHorairesValides(request.HeureOuverture, request.HeureFermeture);

            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                _context, request.IdSite, idSociete, cancellationToken);

            var codeLieu = request.CodeLieu.Trim();
            var exists = await _context.SiteTouristiques
                .AsNoTracking()
                .AnyAsync(l => l.IdSociete == idSociete && l.CodeLieu == codeLieu, cancellationToken);

            if (exists)
            {
                throw new SiteTouristiqueLieuConflictException(
                    $"Un lieu avec le code '{codeLieu}' existe déjà pour cette société.");
            }

            var lieu = new SiteTouristiqueLieu
            {
                IdSociete = idSociete,
                IdSite = request.IdSite,
                CodeLieu = codeLieu,
                Nom = request.Nom.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Province = string.IsNullOrWhiteSpace(request.Province) ? null : request.Province.Trim(),
                Ville = string.IsNullOrWhiteSpace(request.Ville) ? null : request.Ville.Trim(),
                Adresse = string.IsNullOrWhiteSpace(request.Adresse) ? null : request.Adresse.Trim(),
                Telephone = string.IsNullOrWhiteSpace(request.Telephone) ? null : request.Telephone.Trim(),
                HeureOuverture = request.HeureOuverture,
                HeureFermeture = request.HeureFermeture,
                JourOuverture = string.IsNullOrWhiteSpace(request.JourOuverture) ? null : request.JourOuverture.Trim(),
                Status = SiteTouristiqueStatus.Draft,
                DateCreation = DateTime.UtcNow
            };

            _context.SiteTouristiques.Add(lieu);
            await _context.SaveChangesAsync(cancellationToken);

            await _photoService.AddPhotosOnCreateAsync(
                lieu.IdSiteTouristique,
                idSociete,
                request.Photos,
                cancellationToken);

            _logger.LogInformation(
                "Lieu site touristique Draft créé — Id={Id}, Societe={IdSociete}, Code={Code}",
                lieu.IdSiteTouristique, idSociete, codeLieu);

            return (await GetByIdAsync(lieu.IdSiteTouristique, idSociete, cancellationToken))!;
        }

        public async Task<SiteTouristiqueLieuResponseDto?> GetByIdAsync(
            int idSiteTouristique,
            int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            var query = LieuDetailQuery().Where(l => l.IdSiteTouristique == idSiteTouristique);
            if (idSociete.HasValue && idSociete.Value > 0)
                query = query.Where(l => l.IdSociete == idSociete.Value);

            var lieu = await query.FirstOrDefaultAsync(cancellationToken);
            return lieu == null ? null : SiteTouristiqueLieuMapper.ToResponseDto(lieu);
        }

        public async Task<SiteTouristiqueLieuResponseDto?> GetPublishedByIdAsync(
            int idSiteTouristique,
            CancellationToken cancellationToken = default)
        {
            var lieu = await LieuDetailQuery()
                .FirstOrDefaultAsync(
                    l => l.IdSiteTouristique == idSiteTouristique
                         && l.Status == SiteTouristiqueStatus.Published
                         && l.Societe != null
                         && l.Societe.Statut == true,
                    cancellationToken);
            return lieu == null ? null : SiteTouristiqueLieuMapper.ToResponseDto(lieu);
        }

        public async Task<SiteTouristiqueLieuResponseDto?> GetByCodeAsync(
            string codeLieu,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(codeLieu))
                return null;

            var normalized = codeLieu.Trim();
            var lieu = await LieuDetailQuery()
                .FirstOrDefaultAsync(l => l.IdSociete == idSociete && l.CodeLieu == normalized, cancellationToken);
            return lieu == null ? null : SiteTouristiqueLieuMapper.ToResponseDto(lieu);
        }

        public async Task<SiteTouristiqueLieuResponseDto?> GetPublishedByCodeAsync(
            string codeLieu,
            int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(codeLieu))
                return null;

            var normalized = codeLieu.Trim();
            var query = LieuDetailQuery()
                .Where(l =>
                    l.CodeLieu == normalized
                    && l.Status == SiteTouristiqueStatus.Published
                    && l.Societe != null
                    && l.Societe.Statut == true);

            if (idSociete.HasValue && idSociete.Value > 0)
            {
                var lieu = await query.FirstOrDefaultAsync(l => l.IdSociete == idSociete.Value, cancellationToken);
                return lieu == null ? null : SiteTouristiqueLieuMapper.ToResponseDto(lieu);
            }

            var matches = await query.Take(2).ToListAsync(cancellationToken);
            if (matches.Count == 0)
                return null;
            if (matches.Count > 1)
            {
                throw new ArgumentException(
                    $"Plusieurs lieux Published portent le code '{normalized}'. Précisez ?idSociete=.");
            }

            return SiteTouristiqueLieuMapper.ToResponseDto(matches[0]);
        }

        public async Task<IReadOnlyList<SiteTouristiqueLieuListItemDto>> ListAsync(
            int idSociete,
            SiteTouristiqueLieuListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var query = LieuListQuery().Where(l => l.IdSociete == idSociete);
            if (filter?.Status.HasValue == true)
                query = query.Where(l => l.Status == filter.Status.Value);

            var lieux = await query.OrderBy(l => l.Nom).ToListAsync(cancellationToken);
            return lieux.Select(SiteTouristiqueLieuMapper.ToListItemDto).ToList();
        }

        public async Task<IReadOnlyList<SiteTouristiqueLieuListItemDto>> ListPublishedGlobalAsync(
            SiteTouristiqueLieuListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var query = LieuListQuery().Where(l =>
                l.Status == SiteTouristiqueStatus.Published
                && l.Societe != null
                && l.Societe.Statut == true);
            if (filter?.IdSociete.HasValue == true && filter.IdSociete.Value > 0)
                query = query.Where(l => l.IdSociete == filter.IdSociete.Value);

            var lieux = await query.OrderBy(l => l.Nom).ToListAsync(cancellationToken);
            return lieux.Select(SiteTouristiqueLieuMapper.ToListItemDto).ToList();
        }

        public Task<IReadOnlyList<SiteTouristiqueLieuListItemDto>> ListByStatusAsync(
            SiteTouristiqueStatus status,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(idSociete, new SiteTouristiqueLieuListFilter { Status = status }, cancellationToken);

        public async Task<SiteTouristiqueLieuResponseDto?> UpdateAsync(
            int idSiteTouristique,
            SiteTouristiqueUpdateLieuRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Nom))
                throw new InvalidOperationException("Nom est obligatoire.");

            EnsureHorairesValides(request.HeureOuverture, request.HeureFermeture);

            var lieu = await _context.SiteTouristiques
                .FirstOrDefaultAsync(
                    l => l.IdSiteTouristique == idSiteTouristique && l.IdSociete == idSociete,
                    cancellationToken);
            if (lieu == null)
                return null;

            if (request.IdSite.HasValue && request.IdSite.Value > 0)
            {
                await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                    _context, request.IdSite.Value, idSociete, cancellationToken);
                lieu.IdSite = request.IdSite.Value;
            }

            lieu.Nom = request.Nom.Trim();
            lieu.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            lieu.Province = string.IsNullOrWhiteSpace(request.Province) ? null : request.Province.Trim();
            lieu.Ville = string.IsNullOrWhiteSpace(request.Ville) ? null : request.Ville.Trim();
            lieu.Adresse = string.IsNullOrWhiteSpace(request.Adresse) ? null : request.Adresse.Trim();
            lieu.Telephone = string.IsNullOrWhiteSpace(request.Telephone) ? null : request.Telephone.Trim();
            lieu.HeureOuverture = request.HeureOuverture;
            lieu.HeureFermeture = request.HeureFermeture;
            lieu.JourOuverture = string.IsNullOrWhiteSpace(request.JourOuverture) ? null : request.JourOuverture.Trim();
            lieu.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(idSiteTouristique, idSociete, cancellationToken);
        }

        private static void EnsureHorairesValides(TimeOnly? heureOuverture, TimeOnly? heureFermeture)
        {
            if (heureOuverture.HasValue
                && heureFermeture.HasValue
                && heureFermeture.Value <= heureOuverture.Value)
            {
                throw new InvalidOperationException(
                    "HeureFermeture doit être strictement postérieure à HeureOuverture.");
            }
        }

        public async Task<SiteTouristiqueLieuResponseDto> PublishAsync(
            int idSiteTouristique,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var lieu = await _context.SiteTouristiques
                .FirstOrDefaultAsync(
                    l => l.IdSiteTouristique == idSiteTouristique && l.IdSociete == idSociete,
                    cancellationToken);

            if (lieu == null)
                throw new KeyNotFoundException($"Lieu site touristique {idSiteTouristique} introuvable.");

            if (lieu.Status != SiteTouristiqueStatus.Draft)
            {
                throw new InvalidOperationException(
                    $"Seul un lieu Draft peut être publié (statut actuel : {lieu.Status}).");
            }

            if (!lieu.IdSite.HasValue || lieu.IdSite.Value <= 0)
                throw new InvalidOperationException("Publication impossible : IdSite requis.");

            lieu.Status = SiteTouristiqueStatus.Published;
            lieu.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Lieu site touristique publié — Id={Id}, Societe={IdSociete}",
                idSiteTouristique, idSociete);

            return (await GetByIdAsync(idSiteTouristique, idSociete, cancellationToken))!;
        }

        private IQueryable<SiteTouristiqueLieu> LieuListQuery() =>
            _context.SiteTouristiques
                .AsNoTracking()
                .Include(l => l.Societe)
                .Include(l => l.Site)
                .Include(l => l.Photos);

        private IQueryable<SiteTouristiqueLieu> LieuDetailQuery() =>
            _context.SiteTouristiques
                .AsNoTracking()
                .Include(l => l.Societe)
                .Include(l => l.Site)
                .Include(l => l.Journees)
                .Include(l => l.Photos);
    }
}
