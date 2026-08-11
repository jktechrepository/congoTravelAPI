using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueClasseService : ISiteTouristiqueClasseService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<SiteTouristiqueClasseService> _logger;

        public SiteTouristiqueClasseService(CongoTravelDbContext context, ILogger<SiteTouristiqueClasseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SiteTouristiqueClasseResponseDto> CreateAsync(
            SiteTouristiqueCreateClasseRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Libelle))
                throw new InvalidOperationException("Libelle est obligatoire.");

            string? code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
            if (code != null)
            {
                var exists = await _context.SiteTouristiqueClasses
                    .AsNoTracking()
                    .AnyAsync(c => c.IdSociete == idSociete && c.Code == code, cancellationToken);

                if (exists)
                {
                    throw new SiteTouristiqueClasseConflictException(
                        $"Une classe avec le code '{code}' existe déjà pour cette société.");
                }
            }

            var classe = new SiteTouristiqueClasse
            {
                IdSociete = idSociete,
                Code = code,
                Libelle = request.Libelle.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Actif = true,
                DateCreation = DateTime.UtcNow
            };

            _context.SiteTouristiqueClasses.Add(classe);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Classe site touristique créée — Id={Id}, Societe={IdSociete}, Code={Code}",
                classe.IdSiteTouristiqueClasse, idSociete, code);

            return SiteTouristiqueClasseMapper.ToResponseDto(classe);
        }

        public async Task<SiteTouristiqueClasseResponseDto?> GetByIdAsync(
            int idSiteTouristiqueClasse,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var classe = await _context.SiteTouristiqueClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.IdSiteTouristiqueClasse == idSiteTouristiqueClasse && c.IdSociete == idSociete,
                    cancellationToken);

            return classe == null ? null : SiteTouristiqueClasseMapper.ToResponseDto(classe);
        }

        public async Task<IReadOnlyList<SiteTouristiqueClasseResponseDto>> ListAsync(
            int idSociete,
            bool actifsSeulement = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.SiteTouristiqueClasses
                .AsNoTracking()
                .Where(c => c.IdSociete == idSociete);

            if (actifsSeulement)
                query = query.Where(c => c.Actif);

            var classes = await query
                .OrderBy(c => c.Libelle)
                .ToListAsync(cancellationToken);

            return classes.Select(SiteTouristiqueClasseMapper.ToResponseDto).ToList();
        }

        public async Task<SiteTouristiqueClasseResponseDto?> UpdateAsync(
            int idSiteTouristiqueClasse,
            SiteTouristiqueUpdateClasseRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Libelle))
                throw new InvalidOperationException("Libelle est obligatoire.");

            var classe = await _context.SiteTouristiqueClasses
                .FirstOrDefaultAsync(
                    c => c.IdSiteTouristiqueClasse == idSiteTouristiqueClasse && c.IdSociete == idSociete,
                    cancellationToken);

            if (classe == null)
                return null;

            classe.Libelle = request.Libelle.Trim();
            classe.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            classe.Actif = request.Actif;

            await _context.SaveChangesAsync(cancellationToken);
            return SiteTouristiqueClasseMapper.ToResponseDto(classe);
        }

        public async Task<SiteTouristiqueClasseResponseDto?> ToggleStatutAsync(
            int idSiteTouristiqueClasse,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var classe = await _context.SiteTouristiqueClasses
                .FirstOrDefaultAsync(
                    c => c.IdSiteTouristiqueClasse == idSiteTouristiqueClasse && c.IdSociete == idSociete,
                    cancellationToken);

            if (classe == null)
                return null;

            classe.Actif = !classe.Actif;
            await _context.SaveChangesAsync(cancellationToken);
            return SiteTouristiqueClasseMapper.ToResponseDto(classe);
        }

        public async Task<SiteTouristiqueClasseResponseDto?> GetByLibelleAsync(
            string libelle,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(libelle))
                throw new InvalidOperationException("Libelle est obligatoire.");

            var normalized = libelle.Trim();
            var classe = await _context.SiteTouristiqueClasses
                .AsNoTracking()
                .Where(c => c.IdSociete == idSociete)
                .Where(c => c.Libelle.ToLower() == normalized.ToLower())
                .OrderBy(c => c.Libelle)
                .FirstOrDefaultAsync(cancellationToken);

            return classe == null ? null : SiteTouristiqueClasseMapper.ToResponseDto(classe);
        }
    }
}
