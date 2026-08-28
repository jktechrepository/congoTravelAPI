using CongoTravel.Data;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelRoomTypeService : IHotelRoomTypeService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<HotelRoomTypeService> _logger;

        public HotelRoomTypeService(CongoTravelDbContext context, ILogger<HotelRoomTypeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HotelRoomTypeResponseDto> CreateDraftAsync(HotelCreateRoomTypeRequestDto request, int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Code)) throw new InvalidOperationException("Code est obligatoire.");
            if (string.IsNullOrWhiteSpace(request.Libelle)) throw new InvalidOperationException("Libelle est obligatoire.");
            ValidateOptionalFields(request.CapacitePersonnesMax, request.PrixNuitReference, request.CodeDevise);
            if (!await _context.Hotels.AsNoTracking().AnyAsync(h => h.IdHotel == request.IdHotel && h.IdSociete == idSociete, cancellationToken))
                throw new KeyNotFoundException($"Hôtel {request.IdHotel} introuvable.");
            var code = request.Code.Trim();
            if (await _context.HotelRoomTypes.AsNoTracking().AnyAsync(r => r.IdHotel == request.IdHotel && r.Code == code, cancellationToken))
                throw new HotelRoomTypeConflictException($"Un type de chambre avec le code '{code}' existe déjà pour cet hôtel.");
            var roomType = new HotelRoomType
            {
                IdSociete = idSociete, IdHotel = request.IdHotel, Code = code, Libelle = request.Libelle.Trim(),
                Description = Clean(request.Description), CapacitePersonnesMax = request.CapacitePersonnesMax,
                PrixNuitReference = request.PrixNuitReference, CodeDevise = NormalizeCurrency(request.CodeDevise),
                Status = HotelStatus.Draft, DateCreation = DateTime.UtcNow
            };
            _context.HotelRoomTypes.Add(roomType);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Type de chambre Draft créé — Id={Id}, Hotel={IdHotel}", roomType.IdHotelRoomType, request.IdHotel);
            return HotelRoomTypeMapper.ToResponseDto(roomType);
        }

        public async Task<HotelRoomTypeResponseDto?> GetByIdAsync(int idHotelRoomType, int idSociete, CancellationToken cancellationToken = default)
        {
            var value = await _context.HotelRoomTypes.AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdHotelRoomType == idHotelRoomType && r.IdSociete == idSociete, cancellationToken);
            return value == null ? null : HotelRoomTypeMapper.ToResponseDto(value);
        }

        public async Task<HotelRoomTypeResponseDto?> GetPublishedByIdAsync(int idHotelRoomType, CancellationToken cancellationToken = default)
        {
            var value = await _context.HotelRoomTypes.AsNoTracking().FirstOrDefaultAsync(r =>
                r.IdHotelRoomType == idHotelRoomType && r.Status == HotelStatus.Published &&
                r.Hotel != null && r.Hotel.Status == HotelStatus.Published &&
                r.Societe != null && r.Societe.Statut == true, cancellationToken);
            return value == null ? null : HotelRoomTypeMapper.ToResponseDto(value);
        }

        public async Task<IReadOnlyList<HotelRoomTypeResponseDto>> ListAsync(int idSociete,
            HotelRoomTypeListFilter? filter = null, CancellationToken cancellationToken = default)
        {
            var query = _context.HotelRoomTypes.AsNoTracking().Where(r => r.IdSociete == idSociete);
            if (filter?.IdHotel is > 0) query = query.Where(r => r.IdHotel == filter.IdHotel);
            if (filter?.Status != null) query = query.Where(r => r.Status == filter.Status);
            return (await query.OrderBy(r => r.Libelle).ToListAsync(cancellationToken)).Select(HotelRoomTypeMapper.ToResponseDto).ToList();
        }

        public async Task<IReadOnlyList<HotelRoomTypeResponseDto>> ListPublishedGlobalAsync(
            HotelRoomTypeListFilter? filter = null, CancellationToken cancellationToken = default)
        {
            var query = _context.HotelRoomTypes.AsNoTracking().Where(r =>
                r.Status == HotelStatus.Published && r.Hotel != null && r.Hotel.Status == HotelStatus.Published &&
                r.Societe != null && r.Societe.Statut == true);
            if (filter?.IdSociete is > 0) query = query.Where(r => r.IdSociete == filter.IdSociete);
            if (filter?.IdHotel is > 0) query = query.Where(r => r.IdHotel == filter.IdHotel);
            return (await query.OrderBy(r => r.Libelle).ToListAsync(cancellationToken)).Select(HotelRoomTypeMapper.ToResponseDto).ToList();
        }

        public async Task<HotelRoomTypeResponseDto?> UpdateAsync(int idHotelRoomType, HotelUpdateRoomTypeRequestDto request,
            int idSociete, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Libelle)) throw new InvalidOperationException("Libelle est obligatoire.");
            ValidateOptionalFields(request.CapacitePersonnesMax, request.PrixNuitReference, request.CodeDevise);
            var roomType = await _context.HotelRoomTypes.FirstOrDefaultAsync(r =>
                r.IdHotelRoomType == idHotelRoomType && r.IdSociete == idSociete, cancellationToken);
            if (roomType == null) return null;
            roomType.Libelle = request.Libelle.Trim(); roomType.Description = Clean(request.Description);
            roomType.CapacitePersonnesMax = request.CapacitePersonnesMax; roomType.PrixNuitReference = request.PrixNuitReference;
            roomType.CodeDevise = NormalizeCurrency(request.CodeDevise); roomType.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return HotelRoomTypeMapper.ToResponseDto(roomType);
        }

        public async Task<HotelRoomTypeResponseDto> PublishAsync(int idHotelRoomType, int idSociete, CancellationToken cancellationToken = default)
        {
            var roomType = await _context.HotelRoomTypes.Include(r => r.Hotel).FirstOrDefaultAsync(r =>
                r.IdHotelRoomType == idHotelRoomType && r.IdSociete == idSociete, cancellationToken)
                ?? throw new KeyNotFoundException($"Type de chambre {idHotelRoomType} introuvable.");
            if (roomType.Status != HotelStatus.Draft) throw new InvalidOperationException($"Seul un type de chambre Draft peut être publié (statut actuel : {roomType.Status}).");
            if (roomType.Hotel?.Status != HotelStatus.Published)
                throw new InvalidOperationException("Publication impossible : l'hôtel parent doit être Published.");
            roomType.Status = HotelStatus.Published; roomType.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return HotelRoomTypeMapper.ToResponseDto(roomType);
        }

        private static void ValidateOptionalFields(int? capacity, decimal? price, string? currency)
        {
            if (capacity is <= 0) throw new InvalidOperationException("CapacitePersonnesMax doit être positive.");
            if (price is < 0) throw new InvalidOperationException("PrixNuitReference ne peut pas être négatif.");
            if (!string.IsNullOrWhiteSpace(currency) && currency.Trim().Length != 3)
                throw new InvalidOperationException("CodeDevise doit contenir 3 caractères.");
        }
        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static string? NormalizeCurrency(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }
}
