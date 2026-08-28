using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.PhotoStorage;
using RestaurantEntity = CongoTravel.Models.Restaurant.Restaurant;
using RestaurantConflictException = CongoTravel.Models.Restaurant.RestaurantConflictException;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantEtablissementService : IRestaurantEtablissementService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IRestaurantPhotoService _photoService;
        private readonly IPhotoBinaryHydrator _photoHydrator;
        private readonly ILogger<RestaurantEtablissementService> _logger;

        public RestaurantEtablissementService(
            CongoTravelDbContext context,
            IRestaurantPhotoService photoService,
            IPhotoBinaryHydrator photoHydrator,
            ILogger<RestaurantEtablissementService> logger)
        {
            _context = context;
            _photoService = photoService;
            _photoHydrator = photoHydrator;
            _logger = logger;
        }

        public async Task<RestaurantEtablissementResponseDto> CreateDraftAsync(
            RestaurantCreateEtablissementRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.CodeRestaurant))
                throw new InvalidOperationException("CodeRestaurant est obligatoire.");
            if (string.IsNullOrWhiteSpace(request.Nom))
                throw new InvalidOperationException("Nom est obligatoire.");
            if (request.IdSite <= 0)
                throw new InvalidOperationException("IdSite est obligatoire pour créer un établissement restaurant.");
            if (request.AcomptePourcentDefaut < 0 || request.AcomptePourcentDefaut > 100)
                throw new InvalidOperationException("AcomptePourcentDefaut doit être entre 0 et 100.");

            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                _context, request.IdSite, idSociete, cancellationToken);

            var codeRestaurant = request.CodeRestaurant.Trim();
            var exists = await _context.Restaurants
                .AsNoTracking()
                .AnyAsync(r => r.IdSociete == idSociete && r.CodeRestaurant == codeRestaurant, cancellationToken);

            if (exists)
            {
                throw new RestaurantConflictException(
                    $"Un établissement avec le code '{codeRestaurant}' existe déjà pour cette société.");
            }

            var restaurant = new RestaurantEntity
            {
                IdSociete = idSociete,
                IdSite = request.IdSite,
                CodeRestaurant = codeRestaurant,
                Nom = request.Nom.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Adresse = string.IsNullOrWhiteSpace(request.Adresse) ? null : request.Adresse.Trim(),
                AcomptePourcentDefaut = request.AcomptePourcentDefaut,
                Status = RestaurantStatus.Draft,
                DateCreation = DateTime.UtcNow
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync(cancellationToken);

            await _photoService.AddPhotosOnCreateAsync(
                restaurant.IdRestaurant,
                idSociete,
                request.Photos,
                cancellationToken);

            _logger.LogInformation(
                "Établissement restaurant Draft créé — Id={Id}, Societe={IdSociete}, Code={Code}",
                restaurant.IdRestaurant, idSociete, codeRestaurant);

            return (await GetByIdAsync(restaurant.IdRestaurant, idSociete, cancellationToken))!;
        }

        public async Task<RestaurantEtablissementResponseDto?> GetByIdAsync(
            int idRestaurant,
            int? idSociete = null,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false)
        {
            var query = EtablissementDetailQuery().Where(r => r.IdRestaurant == idRestaurant);
            if (idSociete.HasValue && idSociete.Value > 0)
                query = query.Where(r => r.IdSociete == idSociete.Value);

            var restaurant = await query.FirstOrDefaultAsync(cancellationToken);
            if (restaurant == null)
                return null;
            if (includePhotoBase64)
                await _photoHydrator.HydrateRestaurantPhotosAsync(restaurant.Photos, cancellationToken);
            return RestaurantEtablissementMapper.ToResponseDto(restaurant, includePhotoBase64);
        }

        public async Task<RestaurantEtablissementResponseDto?> GetPublishedByIdAsync(
            int idRestaurant,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false)
        {
            var restaurant = await EtablissementDetailQuery()
                .FirstOrDefaultAsync(
                    r => r.IdRestaurant == idRestaurant && r.Status == RestaurantStatus.Published,
                    cancellationToken);
            if (restaurant == null)
                return null;
            if (includePhotoBase64)
                await _photoHydrator.HydrateRestaurantPhotosAsync(restaurant.Photos, cancellationToken);
            return RestaurantEtablissementMapper.ToResponseDto(restaurant, includePhotoBase64);
        }

        public async Task<IReadOnlyList<RestaurantEtablissementListItemDto>> ListAsync(
            int idSociete,
            RestaurantEtablissementListFilter? filter = null,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false)
        {
            var query = EtablissementListQuery().Where(r => r.IdSociete == idSociete);
            if (filter?.Status.HasValue == true)
                query = query.Where(r => r.Status == filter.Status.Value);

            var restaurants = await query.OrderBy(r => r.Nom).ToListAsync(cancellationToken);
            if (includePhotoBase64)
                await _photoHydrator.HydrateRestaurantsAsync(restaurants, cancellationToken);
            return restaurants
                .Select(r => RestaurantEtablissementMapper.ToListItemDto(r, includePhotoBase64))
                .ToList();
        }

        public async Task<IReadOnlyList<RestaurantEtablissementListItemDto>> ListPublishedGlobalAsync(
            RestaurantEtablissementListFilter? filter = null,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false)
        {
            var query = EtablissementListQuery().Where(r => r.Status == RestaurantStatus.Published);
            if (filter?.IdSociete.HasValue == true && filter.IdSociete.Value > 0)
                query = query.Where(r => r.IdSociete == filter.IdSociete.Value);

            var restaurants = await query.OrderBy(r => r.Nom).ToListAsync(cancellationToken);
            if (includePhotoBase64)
                await _photoHydrator.HydrateRestaurantsAsync(restaurants, cancellationToken);
            return restaurants
                .Select(r => RestaurantEtablissementMapper.ToListItemDto(r, includePhotoBase64))
                .ToList();
        }

        public async Task<RestaurantEtablissementResponseDto?> UpdateAsync(
            int idRestaurant,
            RestaurantUpdateEtablissementRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Nom))
                throw new InvalidOperationException("Nom est obligatoire.");

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(
                    r => r.IdRestaurant == idRestaurant && r.IdSociete == idSociete,
                    cancellationToken);
            if (restaurant == null)
                return null;

            if (request.IdSite.HasValue && request.IdSite.Value > 0)
            {
                await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                    _context, request.IdSite.Value, idSociete, cancellationToken);
                restaurant.IdSite = request.IdSite.Value;
            }

            if (request.AcomptePourcentDefaut.HasValue)
            {
                if (request.AcomptePourcentDefaut.Value < 0 || request.AcomptePourcentDefaut.Value > 100)
                    throw new InvalidOperationException("AcomptePourcentDefaut doit être entre 0 et 100.");
                restaurant.AcomptePourcentDefaut = request.AcomptePourcentDefaut.Value;
            }

            restaurant.Nom = request.Nom.Trim();
            restaurant.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            restaurant.Adresse = string.IsNullOrWhiteSpace(request.Adresse) ? null : request.Adresse.Trim();
            restaurant.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(idRestaurant, idSociete, cancellationToken);
        }

        public async Task<RestaurantEtablissementResponseDto> PublishAsync(
            int idRestaurant,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(
                    r => r.IdRestaurant == idRestaurant && r.IdSociete == idSociete,
                    cancellationToken);

            if (restaurant == null)
                throw new KeyNotFoundException($"Établissement restaurant {idRestaurant} introuvable.");

            if (restaurant.Status != RestaurantStatus.Draft)
            {
                throw new InvalidOperationException(
                    $"Seul un établissement Draft peut être publié (statut actuel : {restaurant.Status}).");
            }

            if (!restaurant.IdSite.HasValue || restaurant.IdSite.Value <= 0)
                throw new InvalidOperationException("Publication impossible : IdSite requis.");

            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                _context, restaurant.IdSite.Value, idSociete, cancellationToken);

            restaurant.Status = RestaurantStatus.Published;
            restaurant.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Établissement restaurant publié — Id={Id}, Societe={IdSociete}",
                idRestaurant, idSociete);

            return (await GetByIdAsync(idRestaurant, idSociete, cancellationToken))!;
        }

        private IQueryable<RestaurantEntity> EtablissementListQuery() =>
            _context.Restaurants
                .AsNoTracking()
                .Include(r => r.Societe)
                .Include(r => r.Site)
                .Include(r => r.Photos);

        private IQueryable<RestaurantEntity> EtablissementDetailQuery() =>
            _context.Restaurants
                .AsNoTracking()
                .Include(r => r.Societe)
                .Include(r => r.Site)
                .Include(r => r.Creneaux)
                .Include(r => r.Photos);
    }
}
