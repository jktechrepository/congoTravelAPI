using CongoTravel.Data;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelRoomService : IHotelRoomService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<HotelRoomService> _logger;

        public HotelRoomService(CongoTravelDbContext context, ILogger<HotelRoomService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HotelRoomResponseDto> CreateAsync(
            HotelCreateRoomRequestDto request, int idSociete, CancellationToken cancellationToken = default)
        {
            var numero = NormalizeNumero(request.Numero);
            await EnsureHotelAndRoomTypeAsync(request.IdHotel, request.IdHotelRoomType, idSociete, cancellationToken);
            await EnsureNumeroUniqueAsync(request.IdHotel, numero, excludeId: null, cancellationToken);

            var entity = new HotelRoom
            {
                IdSociete = idSociete,
                IdHotel = request.IdHotel,
                IdHotelRoomType = request.IdHotelRoomType,
                Numero = numero,
                Etage = Clean(request.Etage),
                Libelle = Clean(request.Libelle),
                IsActif = request.IsActif,
                DateCreation = DateTime.UtcNow
            };
            _context.HotelRooms.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Chambre créée — Id={Id}, Hotel={IdHotel}, Numero={Numero}",
                entity.IdHotelRoom, entity.IdHotel, entity.Numero);
            return HotelRoomMapper.ToResponseDto(entity);
        }

        public async Task<HotelRoomResponseDto?> GetByIdAsync(
            int idHotelRoom, int idSociete, CancellationToken cancellationToken = default)
        {
            var entity = await _context.HotelRooms.AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdHotelRoom == idHotelRoom && r.IdSociete == idSociete, cancellationToken);
            return entity == null ? null : HotelRoomMapper.ToResponseDto(entity);
        }

        public async Task<IReadOnlyList<HotelRoomResponseDto>> ListAsync(
            int idSociete, HotelRoomListFilter? filter = null, CancellationToken cancellationToken = default)
        {
            var query = _context.HotelRooms.AsNoTracking().Where(r => r.IdSociete == idSociete);
            if (filter?.IdHotel is > 0) query = query.Where(r => r.IdHotel == filter.IdHotel);
            if (filter?.IdHotelRoomType is > 0) query = query.Where(r => r.IdHotelRoomType == filter.IdHotelRoomType);
            if (filter?.IsActif != null) query = query.Where(r => r.IsActif == filter.IsActif);
            var rows = await query
                .OrderBy(r => r.IdHotel)
                .ThenBy(r => r.Numero)
                .ToListAsync(cancellationToken);
            return rows.Select(HotelRoomMapper.ToResponseDto).ToList();
        }

        public async Task<HotelRoomResponseDto?> UpdateAsync(
            int idHotelRoom, HotelUpdateRoomRequestDto request, int idSociete,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.HotelRooms.FirstOrDefaultAsync(
                r => r.IdHotelRoom == idHotelRoom && r.IdSociete == idSociete, cancellationToken);
            if (entity == null) return null;

            var numero = NormalizeNumero(request.Numero);
            await EnsureHotelAndRoomTypeAsync(entity.IdHotel, request.IdHotelRoomType, idSociete, cancellationToken);
            await EnsureNumeroUniqueAsync(entity.IdHotel, numero, excludeId: entity.IdHotelRoom, cancellationToken);

            entity.IdHotelRoomType = request.IdHotelRoomType;
            entity.Numero = numero;
            entity.Etage = Clean(request.Etage);
            entity.Libelle = Clean(request.Libelle);
            entity.IsActif = request.IsActif;
            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return HotelRoomMapper.ToResponseDto(entity);
        }

        public async Task DeleteAsync(int idHotelRoom, int idSociete, CancellationToken cancellationToken = default)
        {
            var entity = await _context.HotelRooms
                .Include(r => r.Assignments)
                .FirstOrDefaultAsync(r => r.IdHotelRoom == idHotelRoom && r.IdSociete == idSociete, cancellationToken)
                ?? throw new KeyNotFoundException($"Chambre {idHotelRoom} introuvable.");

            if (entity.Assignments.Count > 0)
            {
                throw new InvalidOperationException(
                    "Suppression impossible : la chambre a des attributions. Désactivez-la (IsActif=false) à la place.");
            }

            _context.HotelRooms.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Chambre supprimée — Id={Id}", idHotelRoom);
        }

        private async Task EnsureHotelAndRoomTypeAsync(
            int idHotel, int idHotelRoomType, int idSociete, CancellationToken cancellationToken)
        {
            if (!await _context.Hotels.AsNoTracking()
                    .AnyAsync(h => h.IdHotel == idHotel && h.IdSociete == idSociete, cancellationToken))
                throw new KeyNotFoundException($"Hôtel {idHotel} introuvable.");

            if (!await _context.HotelRoomTypes.AsNoTracking().AnyAsync(
                    t => t.IdHotelRoomType == idHotelRoomType
                         && t.IdHotel == idHotel
                         && t.IdSociete == idSociete,
                    cancellationToken))
            {
                throw new KeyNotFoundException(
                    $"Type de chambre {idHotelRoomType} introuvable pour l'hôtel {idHotel}.");
            }
        }

        private async Task EnsureNumeroUniqueAsync(
            int idHotel, string numero, int? excludeId, CancellationToken cancellationToken)
        {
            var exists = await _context.HotelRooms.AsNoTracking().AnyAsync(
                r => r.IdHotel == idHotel
                     && r.Numero == numero
                     && (excludeId == null || r.IdHotelRoom != excludeId),
                cancellationToken);
            if (exists)
                throw new HotelRoomConflictException(
                    $"Une chambre avec le numéro '{numero}' existe déjà pour cet hôtel.");
        }

        private static string NormalizeNumero(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new InvalidOperationException("Numero est obligatoire.");
            return numero.Trim();
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
