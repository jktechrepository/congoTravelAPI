using CongoTravel.Data;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelExtraService : IHotelExtraService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<HotelExtraService> _logger;

        public HotelExtraService(CongoTravelDbContext context, ILogger<HotelExtraService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HotelExtraResponseDto> CreateAsync(
            HotelCreateExtraRequestDto request, int idSociete, CancellationToken cancellationToken = default)
        {
            ValidateFields(request.PrixUnitaire, request.CodeDevise);
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new InvalidOperationException("Code est obligatoire.");
            if (string.IsNullOrWhiteSpace(request.Libelle))
                throw new InvalidOperationException("Libelle est obligatoire.");

            if (!await _context.Hotels.AsNoTracking()
                    .AnyAsync(h => h.IdHotel == request.IdHotel && h.IdSociete == idSociete, cancellationToken))
                throw new KeyNotFoundException($"Hôtel {request.IdHotel} introuvable.");

            var code = request.Code.Trim();
            if (await _context.HotelExtras.AsNoTracking().AnyAsync(
                    e => e.IdHotel == request.IdHotel && e.Code == code, cancellationToken))
            {
                throw new HotelExtraConflictException(
                    $"Un extra avec le code '{code}' existe déjà pour cet hôtel.");
            }

            var entity = new HotelExtra
            {
                IdSociete = idSociete,
                IdHotel = request.IdHotel,
                Code = code,
                Libelle = request.Libelle.Trim(),
                PrixUnitaire = request.PrixUnitaire,
                CodeDevise = NormalizeCurrency(request.CodeDevise) ?? "CDF",
                PricingUnit = request.PricingUnit,
                IsActif = request.IsActif,
                DateCreation = DateTime.UtcNow
            };
            _context.HotelExtras.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Extra hôtel créé — Id={Id}, Hotel={IdHotel}, Code={Code}",
                entity.IdHotelExtra, entity.IdHotel, entity.Code);
            return HotelExtraMapper.ToResponseDto(entity);
        }

        public async Task<HotelExtraResponseDto?> GetByIdAsync(
            int idHotelExtra, int idSociete, CancellationToken cancellationToken = default)
        {
            var entity = await _context.HotelExtras.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdHotelExtra == idHotelExtra && e.IdSociete == idSociete, cancellationToken);
            return entity == null ? null : HotelExtraMapper.ToResponseDto(entity);
        }

        public async Task<IReadOnlyList<HotelExtraResponseDto>> ListAsync(
            int idSociete, HotelExtraListFilter? filter = null, CancellationToken cancellationToken = default)
        {
            var query = _context.HotelExtras.AsNoTracking().Where(e => e.IdSociete == idSociete);
            if (filter?.IdHotel is > 0) query = query.Where(e => e.IdHotel == filter.IdHotel);
            if (filter?.IsActif != null) query = query.Where(e => e.IsActif == filter.IsActif);
            var rows = await query.OrderBy(e => e.Libelle).ToListAsync(cancellationToken);
            return rows.Select(HotelExtraMapper.ToResponseDto).ToList();
        }

        public async Task<HotelExtraResponseDto?> UpdateAsync(
            int idHotelExtra, HotelUpdateExtraRequestDto request, int idSociete,
            CancellationToken cancellationToken = default)
        {
            ValidateFields(request.PrixUnitaire, request.CodeDevise);
            if (string.IsNullOrWhiteSpace(request.Libelle))
                throw new InvalidOperationException("Libelle est obligatoire.");

            var entity = await _context.HotelExtras.FirstOrDefaultAsync(
                e => e.IdHotelExtra == idHotelExtra && e.IdSociete == idSociete, cancellationToken);
            if (entity == null) return null;

            entity.Libelle = request.Libelle.Trim();
            entity.PrixUnitaire = request.PrixUnitaire;
            entity.CodeDevise = NormalizeCurrency(request.CodeDevise) ?? entity.CodeDevise;
            entity.PricingUnit = request.PricingUnit;
            entity.IsActif = request.IsActif;
            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return HotelExtraMapper.ToResponseDto(entity);
        }

        public async Task DeleteAsync(int idHotelExtra, int idSociete, CancellationToken cancellationToken = default)
        {
            var entity = await _context.HotelExtras
                .Include(e => e.ReservationExtras)
                .FirstOrDefaultAsync(e => e.IdHotelExtra == idHotelExtra && e.IdSociete == idSociete, cancellationToken)
                ?? throw new KeyNotFoundException($"Extra {idHotelExtra} introuvable.");

            if (entity.ReservationExtras.Count > 0)
            {
                throw new InvalidOperationException(
                    "Suppression impossible : l'extra est utilisé sur des réservations. Désactivez-le (IsActif=false).");
            }

            _context.HotelExtras.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Extra hôtel supprimé — Id={Id}", idHotelExtra);
        }

        private static void ValidateFields(decimal prix, string? codeDevise)
        {
            if (prix < 0) throw new InvalidOperationException("PrixUnitaire ne peut pas être négatif.");
            if (!string.IsNullOrWhiteSpace(codeDevise) && codeDevise.Trim().Length != 3)
                throw new InvalidOperationException("CodeDevise doit contenir 3 caractères.");
        }

        private static string? NormalizeCurrency(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }
}
