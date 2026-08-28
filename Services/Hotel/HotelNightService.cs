using CongoTravel.Data;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelNightService : IHotelNightService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<HotelNightService> _logger;

        public HotelNightService(CongoTravelDbContext context, ILogger<HotelNightService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HotelNightResponseDto> CreateDraftAsync(
            HotelCreateNightRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            ValidateCreateFields(request.CapaciteTotale, request.PrixNuit, request.CodeDevise);
            var night = request.NightDate.Date;
            await EnsureHotelExistsAsync(request.IdHotel, idSociete, cancellationToken);

            if (await ExistsAsync(request.IdHotel, night, cancellationToken))
            {
                throw new HotelNightConflictException(
                    $"Une nuit GlobalQuota existe déjà pour l'hôtel {request.IdHotel}, nuit {night:yyyy-MM-dd}.");
            }

            var entity = BuildDraft(
                idSociete,
                request.IdHotel,
                night,
                request.CapaciteTotale,
                request.PrixNuit,
                request.CodeDevise,
                request.IdHotelPlanification);
            _context.HotelNights.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Nuit hôtel Draft créée — Id={Id}, Hotel={IdHotel}, Night={Night}, Planif={Planif}",
                entity.IdHotelNight, entity.IdHotel, night, request.IdHotelPlanification);
            return await LoadResponseAsync(entity.IdHotelNight, idSociete, cancellationToken)
                ?? HotelNightMapper.ToResponseDto(entity);
        }

        public async Task<HotelNightBatchResultDto> CreateDraftBatchAsync(
            HotelCreateNightBatchRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            ValidateCreateFields(request.CapaciteTotale, request.PrixNuit, request.CodeDevise);
            var from = request.From.Date;
            var to = request.To.Date;
            if (to <= from)
                throw new InvalidOperationException("To doit être strictement postérieur à From (intervalle [from, to)).");

            await EnsureHotelExistsAsync(request.IdHotel, idSociete, cancellationToken);

            var result = new HotelNightBatchResultDto();
            for (var d = from; d < to; d = d.AddDays(1))
            {
                if (await ExistsAsync(request.IdHotel, d, cancellationToken))
                {
                    if (request.SkipExisting)
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    throw new HotelNightConflictException(
                        $"Une nuit GlobalQuota existe déjà pour la nuit {d:yyyy-MM-dd}.");
                }

                var entity = BuildDraft(
                    idSociete,
                    request.IdHotel,
                    d,
                    request.CapaciteTotale,
                    request.PrixNuit,
                    request.CodeDevise);
                _context.HotelNights.Add(entity);
                result.CreatedCount++;
            }

            if (result.CreatedCount > 0)
                await _context.SaveChangesAsync(cancellationToken);

            var created = await QueryBase()
                .Where(n => n.IdSociete == idSociete
                    && n.IdHotel == request.IdHotel
                    && n.NightDate >= from
                    && n.NightDate < to
                    && n.Status == HotelStatus.Draft)
                .OrderBy(n => n.NightDate)
                .ToListAsync(cancellationToken);

            result.Created = created.Select(HotelNightMapper.ToResponseDto).ToList();
            _logger.LogInformation(
                "Batch nuits Draft — Hotel={IdHotel}, Created={Created}, Skipped={Skipped}",
                request.IdHotel, result.CreatedCount, result.SkippedCount);
            return result;
        }

        public async Task<HotelNightResponseDto?> GetByIdAsync(
            int idHotelNight,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            await LoadResponseAsync(idHotelNight, idSociete, cancellationToken);

        public async Task<HotelNightResponseDto?> GetPublishedByIdAsync(
            int idHotelNight,
            CancellationToken cancellationToken = default)
        {
            var entity = await QueryBase()
                .FirstOrDefaultAsync(
                    n => n.IdHotelNight == idHotelNight
                        && n.Status == HotelStatus.Published
                        && n.Hotel != null && n.Hotel.Status == HotelStatus.Published
                        && n.Societe != null && n.Societe.Statut == true,
                    cancellationToken);
            return entity == null ? null : HotelNightMapper.ToResponseDto(entity);
        }

        public async Task<IReadOnlyList<HotelNightResponseDto>> ListAsync(
            int idSociete,
            HotelNightListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var query = QueryBase().Where(n => n.IdSociete == idSociete);
            query = ApplyFilter(query, filter);
            var rows = await query.OrderBy(n => n.NightDate).ToListAsync(cancellationToken);
            return rows.Select(HotelNightMapper.ToResponseDto).ToList();
        }

        public async Task<IReadOnlyList<HotelNightResponseDto>> ListPublishedGlobalAsync(
            HotelNightListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var query = QueryBase().Where(n =>
                n.Status == HotelStatus.Published
                && n.Hotel != null && n.Hotel.Status == HotelStatus.Published
                && n.Societe != null && n.Societe.Statut == true);
            query = ApplyFilter(query, filter);
            var rows = await query.OrderBy(n => n.NightDate).ToListAsync(cancellationToken);
            return rows.Select(HotelNightMapper.ToResponseDto).ToList();
        }

        public async Task<HotelNightResponseDto?> UpdateAsync(
            int idHotelNight,
            HotelUpdateNightRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            ValidateCreateFields(request.CapaciteTotale, request.PrixNuit, request.CodeDevise);
            var entity = await _context.HotelNights
                .FirstOrDefaultAsync(
                    n => n.IdHotelNight == idHotelNight && n.IdSociete == idSociete,
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
            return await LoadResponseAsync(entity.IdHotelNight, idSociete, cancellationToken);
        }

        public async Task<HotelNightResponseDto> PublishAsync(
            int idHotelNight,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.HotelNights
                .Include(n => n.Hotel)
                .FirstOrDefaultAsync(
                    n => n.IdHotelNight == idHotelNight && n.IdSociete == idSociete,
                    cancellationToken)
                ?? throw new KeyNotFoundException($"Nuit {idHotelNight} introuvable.");

            if (entity.Status != HotelStatus.Draft)
                throw new InvalidOperationException($"Seule une nuit Draft peut être publiée (statut actuel : {entity.Status}).");
            if (entity.Hotel?.Status != HotelStatus.Published)
                throw new InvalidOperationException("Publication impossible : l'hôtel parent doit être Published.");
            if (entity.CapaciteTotale <= 0)
                throw new InvalidOperationException("Publication impossible : CapaciteTotale doit être > 0.");

            entity.Status = HotelStatus.Published;
            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return await LoadResponseAsync(entity.IdHotelNight, idSociete, cancellationToken)
                ?? HotelNightMapper.ToResponseDto(entity);
        }

        private IQueryable<HotelNight> QueryBase() =>
            _context.HotelNights.AsNoTracking()
                .Include(n => n.Hotel)
                .Include(n => n.Societe);

        private static IQueryable<HotelNight> ApplyFilter(
            IQueryable<HotelNight> query,
            HotelNightListFilter? filter)
        {
            if (filter == null)
                return query;
            if (filter.IdSociete is > 0)
                query = query.Where(n => n.IdSociete == filter.IdSociete);
            if (filter.IdHotel is > 0)
                query = query.Where(n => n.IdHotel == filter.IdHotel);
            if (filter.From.HasValue)
                query = query.Where(n => n.NightDate >= filter.From.Value.Date);
            if (filter.To.HasValue)
                query = query.Where(n => n.NightDate < filter.To.Value.Date);
            if (filter.Status.HasValue)
                query = query.Where(n => n.Status == filter.Status.Value);
            return query;
        }

        private async Task<HotelNightResponseDto?> LoadResponseAsync(
            int id,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var entity = await QueryBase()
                .FirstOrDefaultAsync(n => n.IdHotelNight == id && n.IdSociete == idSociete, cancellationToken);
            return entity == null ? null : HotelNightMapper.ToResponseDto(entity);
        }

        private async Task EnsureHotelExistsAsync(
            int idHotel,
            int idSociete,
            CancellationToken cancellationToken)
        {
            if (!await _context.Hotels.AsNoTracking()
                    .AnyAsync(h => h.IdHotel == idHotel && h.IdSociete == idSociete, cancellationToken))
                throw new KeyNotFoundException($"Hôtel {idHotel} introuvable.");
        }

        private Task<bool> ExistsAsync(int idHotel, DateTime night, CancellationToken cancellationToken) =>
            _context.HotelNights.AsNoTracking()
                .AnyAsync(n => n.IdHotel == idHotel && n.NightDate == night, cancellationToken);

        private static HotelNight BuildDraft(
            int idSociete,
            int idHotel,
            DateTime night,
            int capacite,
            decimal prix,
            string? devise,
            int? idHotelPlanification = null) =>
            new()
            {
                IdSociete = idSociete,
                IdHotel = idHotel,
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
