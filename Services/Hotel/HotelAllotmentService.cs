using CongoTravel.Data;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelAllotmentService : IHotelAllotmentService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<HotelAllotmentService> _logger;

        public HotelAllotmentService(CongoTravelDbContext context, ILogger<HotelAllotmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HotelAllotmentResponseDto> CreateDraftAsync(
            HotelCreateAllotmentRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            ValidateCreateFields(request.CapaciteTotale, request.PrixNuit, request.CodeDevise);
            var night = request.NightDate.Date;
            await EnsureParentsExistAsync(request.IdHotel, request.IdHotelRoomType, idSociete, cancellationToken);

            if (await ExistsAsync(request.IdHotel, request.IdHotelRoomType, night, cancellationToken))
            {
                throw new HotelNightAllotmentConflictException(
                    $"Un allotment existe déjà pour l'hôtel {request.IdHotel}, type {request.IdHotelRoomType}, nuit {night:yyyy-MM-dd}.");
            }

            var entity = BuildDraft(
                idSociete,
                request.IdHotel,
                request.IdHotelRoomType,
                night,
                request.CapaciteTotale,
                request.PrixNuit,
                request.CodeDevise,
                request.IdHotelPlanification);
            _context.HotelNightAllotments.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Allotment hôtel Draft créé — Id={Id}, Hotel={IdHotel}, Night={Night}, Planif={Planif}",
                entity.IdHotelNightAllotment, entity.IdHotel, night, request.IdHotelPlanification);
            return await LoadResponseAsync(entity.IdHotelNightAllotment, idSociete, cancellationToken)
                ?? HotelAllotmentMapper.ToResponseDto(entity);
        }

        public async Task<HotelAllotmentBatchResultDto> CreateDraftBatchAsync(
            HotelCreateAllotmentBatchRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            ValidateCreateFields(request.CapaciteTotale, request.PrixNuit, request.CodeDevise);
            var from = request.From.Date;
            var to = request.To.Date;
            if (to <= from)
                throw new InvalidOperationException("To doit être strictement postérieur à From (intervalle [from, to)).");

            await EnsureParentsExistAsync(request.IdHotel, request.IdHotelRoomType, idSociete, cancellationToken);

            var result = new HotelAllotmentBatchResultDto();
            for (var d = from; d < to; d = d.AddDays(1))
            {
                if (await ExistsAsync(request.IdHotel, request.IdHotelRoomType, d, cancellationToken))
                {
                    if (request.SkipExisting)
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    throw new HotelNightAllotmentConflictException(
                        $"Un allotment existe déjà pour la nuit {d:yyyy-MM-dd}.");
                }

                var entity = BuildDraft(
                    idSociete,
                    request.IdHotel,
                    request.IdHotelRoomType,
                    d,
                    request.CapaciteTotale,
                    request.PrixNuit,
                    request.CodeDevise);
                _context.HotelNightAllotments.Add(entity);
                result.CreatedCount++;
            }

            if (result.CreatedCount > 0)
                await _context.SaveChangesAsync(cancellationToken);

            var created = await QueryBase()
                .Where(a => a.IdSociete == idSociete
                    && a.IdHotel == request.IdHotel
                    && a.IdHotelRoomType == request.IdHotelRoomType
                    && a.NightDate >= from
                    && a.NightDate < to
                    && a.Status == HotelStatus.Draft)
                .OrderBy(a => a.NightDate)
                .ToListAsync(cancellationToken);

            result.Created = created.Select(HotelAllotmentMapper.ToResponseDto).ToList();
            _logger.LogInformation(
                "Batch allotments Draft — Hotel={IdHotel}, Created={Created}, Skipped={Skipped}",
                request.IdHotel, result.CreatedCount, result.SkippedCount);
            return result;
        }

        public async Task<HotelAllotmentResponseDto?> GetByIdAsync(
            int idHotelNightAllotment,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            await LoadResponseAsync(idHotelNightAllotment, idSociete, cancellationToken);

        public async Task<HotelAllotmentResponseDto?> GetPublishedByIdAsync(
            int idHotelNightAllotment,
            CancellationToken cancellationToken = default)
        {
            var entity = await QueryBase()
                .FirstOrDefaultAsync(
                    a => a.IdHotelNightAllotment == idHotelNightAllotment
                        && a.Status == HotelStatus.Published
                        && a.Hotel != null && a.Hotel.Status == HotelStatus.Published
                        && a.RoomType != null && a.RoomType.Status == HotelStatus.Published
                        && a.Societe != null && a.Societe.Statut == true,
                    cancellationToken);
            return entity == null ? null : HotelAllotmentMapper.ToResponseDto(entity);
        }

        public async Task<IReadOnlyList<HotelAllotmentResponseDto>> ListAsync(
            int idSociete,
            HotelAllotmentListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var query = QueryBase().Where(a => a.IdSociete == idSociete);
            query = ApplyFilter(query, filter);
            var rows = await query.OrderBy(a => a.NightDate).ThenBy(a => a.IdHotelRoomType)
                .ToListAsync(cancellationToken);
            return rows.Select(HotelAllotmentMapper.ToResponseDto).ToList();
        }

        public async Task<IReadOnlyList<HotelAllotmentResponseDto>> ListPublishedGlobalAsync(
            HotelAllotmentListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var query = QueryBase().Where(a =>
                a.Status == HotelStatus.Published
                && a.Hotel != null && a.Hotel.Status == HotelStatus.Published
                && a.RoomType != null && a.RoomType.Status == HotelStatus.Published
                && a.Societe != null && a.Societe.Statut == true);
            query = ApplyFilter(query, filter);
            var rows = await query.OrderBy(a => a.NightDate).ThenBy(a => a.IdHotelRoomType)
                .ToListAsync(cancellationToken);
            return rows.Select(HotelAllotmentMapper.ToResponseDto).ToList();
        }

        public async Task<HotelAllotmentResponseDto?> UpdateAsync(
            int idHotelNightAllotment,
            HotelUpdateAllotmentRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            ValidateCreateFields(request.CapaciteTotale, request.PrixNuit, request.CodeDevise);
            var entity = await _context.HotelNightAllotments
                .FirstOrDefaultAsync(
                    a => a.IdHotelNightAllotment == idHotelNightAllotment && a.IdSociete == idSociete,
                    cancellationToken);
            if (entity == null)
                return null;

            if (request.CapaciteTotale < entity.QuantiteHold + entity.QuantiteVendue)
            {
                throw new InvalidOperationException(
                    $"CapaciteTotale ({request.CapaciteTotale}) ne peut pas être inférieure à Hold+Vendue ({entity.QuantiteHold + entity.QuantiteVendue}).");
            }

            entity.CapaciteTotale = request.CapaciteTotale;
            entity.PrixNuit = request.PrixNuit;
            entity.CodeDevise = NormalizeCurrency(request.CodeDevise);
            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return await LoadResponseAsync(entity.IdHotelNightAllotment, idSociete, cancellationToken);
        }

        public async Task<HotelAllotmentResponseDto> PublishAsync(
            int idHotelNightAllotment,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.HotelNightAllotments
                .Include(a => a.Hotel)
                .Include(a => a.RoomType)
                .FirstOrDefaultAsync(
                    a => a.IdHotelNightAllotment == idHotelNightAllotment && a.IdSociete == idSociete,
                    cancellationToken)
                ?? throw new KeyNotFoundException($"Allotment {idHotelNightAllotment} introuvable.");

            if (entity.Status != HotelStatus.Draft)
                throw new InvalidOperationException($"Seul un allotment Draft peut être publié (statut actuel : {entity.Status}).");
            if (entity.Hotel?.Status != HotelStatus.Published)
                throw new InvalidOperationException("Publication impossible : l'hôtel parent doit être Published.");
            if (entity.RoomType?.Status != HotelStatus.Published)
                throw new InvalidOperationException("Publication impossible : le type de chambre parent doit être Published.");
            if (entity.CapaciteTotale <= 0)
                throw new InvalidOperationException("Publication impossible : CapaciteTotale doit être > 0.");

            entity.Status = HotelStatus.Published;
            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return await LoadResponseAsync(entity.IdHotelNightAllotment, idSociete, cancellationToken)
                ?? HotelAllotmentMapper.ToResponseDto(entity);
        }

        private IQueryable<HotelNightAllotment> QueryBase() =>
            _context.HotelNightAllotments.AsNoTracking()
                .Include(a => a.RoomType)
                .Include(a => a.Hotel)
                .Include(a => a.Societe);

        private static IQueryable<HotelNightAllotment> ApplyFilter(
            IQueryable<HotelNightAllotment> query,
            HotelAllotmentListFilter? filter)
        {
            if (filter == null)
                return query;
            if (filter.IdSociete is > 0)
                query = query.Where(a => a.IdSociete == filter.IdSociete);
            if (filter.IdHotel is > 0)
                query = query.Where(a => a.IdHotel == filter.IdHotel);
            if (filter.IdHotelRoomType is > 0)
                query = query.Where(a => a.IdHotelRoomType == filter.IdHotelRoomType);
            if (filter.From.HasValue)
                query = query.Where(a => a.NightDate >= filter.From.Value.Date);
            if (filter.To.HasValue)
                query = query.Where(a => a.NightDate < filter.To.Value.Date);
            if (filter.Status.HasValue)
                query = query.Where(a => a.Status == filter.Status.Value);
            return query;
        }

        private async Task<HotelAllotmentResponseDto?> LoadResponseAsync(
            int id,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var entity = await QueryBase()
                .FirstOrDefaultAsync(a => a.IdHotelNightAllotment == id && a.IdSociete == idSociete, cancellationToken);
            return entity == null ? null : HotelAllotmentMapper.ToResponseDto(entity);
        }

        private async Task EnsureParentsExistAsync(
            int idHotel,
            int idRoomType,
            int idSociete,
            CancellationToken cancellationToken)
        {
            if (!await _context.Hotels.AsNoTracking()
                    .AnyAsync(h => h.IdHotel == idHotel && h.IdSociete == idSociete, cancellationToken))
                throw new KeyNotFoundException($"Hôtel {idHotel} introuvable.");

            if (!await _context.HotelRoomTypes.AsNoTracking()
                    .AnyAsync(
                        r => r.IdHotelRoomType == idRoomType && r.IdHotel == idHotel && r.IdSociete == idSociete,
                        cancellationToken))
                throw new KeyNotFoundException($"Type de chambre {idRoomType} introuvable pour cet hôtel.");
        }

        private Task<bool> ExistsAsync(int idHotel, int idRoomType, DateTime night, CancellationToken cancellationToken) =>
            _context.HotelNightAllotments.AsNoTracking()
                .AnyAsync(
                    a => a.IdHotel == idHotel && a.IdHotelRoomType == idRoomType && a.NightDate == night,
                    cancellationToken);

        private static HotelNightAllotment BuildDraft(
            int idSociete,
            int idHotel,
            int idRoomType,
            DateTime night,
            int capacite,
            decimal prix,
            string? devise,
            int? idHotelPlanification = null) =>
            new()
            {
                IdSociete = idSociete,
                IdHotel = idHotel,
                IdHotelRoomType = idRoomType,
                NightDate = night,
                CapaciteTotale = capacite,
                QuantiteHold = 0,
                QuantiteVendue = 0,
                PrixNuit = prix,
                CodeDevise = NormalizeCurrency(devise),
                Status = HotelStatus.Draft,
                IdHotelPlanification = idHotelPlanification,
                DateCreation = DateTime.UtcNow
            };

        private static void ValidateCreateFields(int capacite, decimal prix, string? devise)
        {
            if (capacite < 0)
                throw new InvalidOperationException("CapaciteTotale ne peut pas être négative.");
            if (prix < 0)
                throw new InvalidOperationException("PrixNuit ne peut pas être négatif.");
            if (!string.IsNullOrWhiteSpace(devise) && devise.Trim().Length != 3)
                throw new InvalidOperationException("CodeDevise doit contenir 3 caractères.");
        }

        private static string NormalizeCurrency(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "CDF" : value.Trim().ToUpperInvariant();
    }
}
