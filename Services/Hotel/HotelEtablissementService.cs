using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.PhotoStorage;
using Microsoft.EntityFrameworkCore;
using HotelEntity = CongoTravel.Models.Hotel.Hotel;

namespace CongoTravel.Services.Hotel
{
    public class HotelEtablissementService : IHotelEtablissementService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IHotelPhotoService _photoService;
        private readonly IPhotoBinaryHydrator _photoHydrator;
        private readonly ILogger<HotelEtablissementService> _logger;

        public HotelEtablissementService(CongoTravelDbContext context, IHotelPhotoService photoService,
            IPhotoBinaryHydrator photoHydrator, ILogger<HotelEtablissementService> logger)
        {
            _context = context;
            _photoService = photoService;
            _photoHydrator = photoHydrator;
            _logger = logger;
        }

        public async Task<HotelEtablissementResponseDto> CreateDraftAsync(
            HotelCreateEtablissementRequestDto request, int idSociete, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.CodeHotel)) throw new InvalidOperationException("CodeHotel est obligatoire.");
            if (string.IsNullOrWhiteSpace(request.Nom)) throw new InvalidOperationException("Nom est obligatoire.");
            if (request.IdSite <= 0) throw new InvalidOperationException("IdSite est obligatoire pour créer un hôtel.");
            if (request.AcomptePourcentDefaut is < 0 or > 100) throw new InvalidOperationException("AcomptePourcentDefaut doit être entre 0 et 100.");
            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(_context, request.IdSite, idSociete, cancellationToken);
            var code = request.CodeHotel.Trim();
            if (await _context.Hotels.AsNoTracking().AnyAsync(h => h.IdSociete == idSociete && h.CodeHotel == code, cancellationToken))
                throw new Models.Hotel.HotelConflictException($"Un hôtel avec le code '{code}' existe déjà pour cette société.");

            var hotel = new HotelEntity
            {
                IdSociete = idSociete, IdSite = request.IdSite, CodeHotel = code, Nom = request.Nom.Trim(),
                Description = Clean(request.Description), Adresse = Clean(request.Adresse),
                AcomptePourcentDefaut = request.AcomptePourcentDefaut, Status = HotelStatus.Draft,
                DateCreation = DateTime.UtcNow
            };
            _context.Hotels.Add(hotel);
            await _context.SaveChangesAsync(cancellationToken);
            await _photoService.AddPhotosOnCreateAsync(hotel.IdHotel, idSociete, request.Photos, cancellationToken);
            _logger.LogInformation("Hôtel Draft créé — Id={Id}, Societe={IdSociete}", hotel.IdHotel, idSociete);
            return (await GetByIdAsync(hotel.IdHotel, idSociete, cancellationToken))!;
        }

        public async Task<HotelEtablissementResponseDto?> GetByIdAsync(int idHotel, int? idSociete = null,
            CancellationToken cancellationToken = default, bool includePhotoBase64 = false)
        {
            var query = DetailQuery().Where(h => h.IdHotel == idHotel);
            if (idSociete is > 0) query = query.Where(h => h.IdSociete == idSociete);
            var hotel = await query.FirstOrDefaultAsync(cancellationToken);
            if (hotel == null) return null;
            if (includePhotoBase64) await _photoHydrator.HydrateHotelPhotosAsync(hotel.Photos, cancellationToken);
            return HotelEtablissementMapper.ToResponseDto(hotel, includePhotoBase64);
        }

        public async Task<HotelEtablissementResponseDto?> GetPublishedByIdAsync(int idHotel,
            CancellationToken cancellationToken = default, bool includePhotoBase64 = false)
        {
            var hotel = await DetailQuery().FirstOrDefaultAsync(h => h.IdHotel == idHotel && h.Status == HotelStatus.Published, cancellationToken);
            if (hotel == null) return null;
            if (includePhotoBase64) await _photoHydrator.HydrateHotelPhotosAsync(hotel.Photos, cancellationToken);
            return HotelEtablissementMapper.ToResponseDto(hotel, includePhotoBase64);
        }

        public async Task<IReadOnlyList<HotelEtablissementListItemDto>> ListAsync(int idSociete,
            HotelEtablissementListFilter? filter = null, CancellationToken cancellationToken = default, bool includePhotoBase64 = false)
        {
            var query = ListQuery().Where(h => h.IdSociete == idSociete);
            if (filter?.Status != null) query = query.Where(h => h.Status == filter.Status);
            var hotels = await query.OrderBy(h => h.Nom).ToListAsync(cancellationToken);
            if (includePhotoBase64) await _photoHydrator.HydrateHotelsAsync(hotels, cancellationToken);
            return hotels.Select(h => HotelEtablissementMapper.ToListItemDto(h, includePhotoBase64)).ToList();
        }

        public async Task<IReadOnlyList<HotelEtablissementListItemDto>> ListPublishedGlobalAsync(
            HotelEtablissementListFilter? filter = null, CancellationToken cancellationToken = default, bool includePhotoBase64 = false)
        {
            var query = ListQuery().Where(h => h.Status == HotelStatus.Published);
            if (filter?.IdSociete is > 0) query = query.Where(h => h.IdSociete == filter.IdSociete);
            var hotels = await query.OrderBy(h => h.Nom).ToListAsync(cancellationToken);
            if (includePhotoBase64) await _photoHydrator.HydrateHotelsAsync(hotels, cancellationToken);
            return hotels.Select(h => HotelEtablissementMapper.ToListItemDto(h, includePhotoBase64)).ToList();
        }

        public async Task<HotelEtablissementResponseDto?> UpdateAsync(int idHotel, HotelUpdateEtablissementRequestDto request,
            int idSociete, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Nom)) throw new InvalidOperationException("Nom est obligatoire.");
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.IdHotel == idHotel && h.IdSociete == idSociete, cancellationToken);
            if (hotel == null) return null;
            if (request.IdSite is > 0)
            {
                await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(_context, request.IdSite.Value, idSociete, cancellationToken);
                hotel.IdSite = request.IdSite;
            }
            if (request.AcomptePourcentDefaut.HasValue)
            {
                if (request.AcomptePourcentDefaut is < 0 or > 100) throw new InvalidOperationException("AcomptePourcentDefaut doit être entre 0 et 100.");
                hotel.AcomptePourcentDefaut = request.AcomptePourcentDefaut.Value;
            }
            hotel.Nom = request.Nom.Trim(); hotel.Description = Clean(request.Description); hotel.Adresse = Clean(request.Adresse);
            hotel.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return await GetByIdAsync(idHotel, idSociete, cancellationToken);
        }

        public async Task<HotelEtablissementResponseDto> PublishAsync(int idHotel, int idSociete, CancellationToken cancellationToken = default)
        {
            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.IdHotel == idHotel && h.IdSociete == idSociete, cancellationToken)
                ?? throw new KeyNotFoundException($"Hôtel {idHotel} introuvable.");
            if (hotel.Status != HotelStatus.Draft) throw new InvalidOperationException($"Seul un hôtel Draft peut être publié (statut actuel : {hotel.Status}).");
            if (hotel.IdSite is not > 0) throw new InvalidOperationException("Publication impossible : IdSite requis.");
            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(_context, hotel.IdSite.Value, idSociete, cancellationToken);
            hotel.Status = HotelStatus.Published; hotel.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(idHotel, idSociete, cancellationToken))!;
        }

        private IQueryable<HotelEntity> ListQuery() => _context.Hotels.AsNoTracking().Include(h => h.Societe).Include(h => h.Site).Include(h => h.Photos);
        private IQueryable<HotelEntity> DetailQuery() => ListQuery().Include(h => h.RoomTypes);
        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
