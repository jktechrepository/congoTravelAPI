using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;

namespace CongoTravel.Services.Evenement
{
    public class EvenementClasseService : IEvenementClasseService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<EvenementClasseService> _logger;

        public EvenementClasseService(CongoTravelDbContext context, ILogger<EvenementClasseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EvenementClasseResponseDto> CreateAsync(
            EvenementCreateClasseRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.CodeClasse))
                throw new InvalidOperationException("CodeClasse est obligatoire.");

            if (string.IsNullOrWhiteSpace(request.Libelle))
                throw new InvalidOperationException("Libelle est obligatoire.");

            var codeClasse = request.CodeClasse.Trim();
            var exists = await _context.EvenementClasses
                .AsNoTracking()
                .AnyAsync(
                    c => c.IdSociete == idSociete && c.CodeClasse == codeClasse,
                    cancellationToken);

            if (exists)
            {
                throw new EvenementClasseConflictException(
                    $"Une classe avec le code '{codeClasse}' existe déjà pour cette société.");
            }

            var classe = new EvenementClasse
            {
                IdSociete = idSociete,
                CodeClasse = codeClasse,
                Libelle = request.Libelle.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),
                Statut = true,
                DateCreation = DateTime.UtcNow
            };

            _context.EvenementClasses.Add(classe);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Classe événement créée — Id={Id}, Societe={IdSociete}, Code={Code}",
                classe.IdEvenementClasse,
                idSociete,
                codeClasse);

            return EvenementClasseMapper.ToResponseDto(classe);
        }

        public async Task<EvenementClasseResponseDto?> GetByIdAsync(
            int idEvenementClasse,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var classe = await _context.EvenementClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.IdEvenementClasse == idEvenementClasse && c.IdSociete == idSociete,
                    cancellationToken);

            return classe == null ? null : EvenementClasseMapper.ToResponseDto(classe);
        }

        public async Task<IReadOnlyList<EvenementClasseResponseDto>> ListAsync(
            int idSociete,
            bool actifsSeulement = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.EvenementClasses
                .AsNoTracking()
                .Where(c => c.IdSociete == idSociete);

            if (actifsSeulement)
                query = query.Where(c => c.Statut);

            var classes = await query
                .OrderBy(c => c.CodeClasse)
                .ToListAsync(cancellationToken);

            return classes.Select(EvenementClasseMapper.ToResponseDto).ToList();
        }

        public async Task<EvenementClasseResponseDto?> UpdateAsync(
            int idEvenementClasse,
            EvenementUpdateClasseRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Libelle))
                throw new InvalidOperationException("Libelle est obligatoire.");

            var classe = await _context.EvenementClasses
                .FirstOrDefaultAsync(
                    c => c.IdEvenementClasse == idEvenementClasse && c.IdSociete == idSociete,
                    cancellationToken);

            if (classe == null)
                return null;

            classe.Libelle = request.Libelle.Trim();
            classe.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
            classe.Statut = request.Statut;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Classe événement mise à jour — Id={Id}, Societe={IdSociete}",
                idEvenementClasse,
                idSociete);

            return EvenementClasseMapper.ToResponseDto(classe);
        }

        public async Task<EvenementClasseResponseDto?> ToggleStatutAsync(
            int idEvenementClasse,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var classe = await _context.EvenementClasses
                .FirstOrDefaultAsync(
                    c => c.IdEvenementClasse == idEvenementClasse && c.IdSociete == idSociete,
                    cancellationToken);

            if (classe == null)
                return null;

            classe.Statut = !classe.Statut;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Classe événement statut basculé — Id={Id}, Societe={IdSociete}, Statut={Statut}",
                idEvenementClasse,
                idSociete,
                classe.Statut);

            return EvenementClasseMapper.ToResponseDto(classe);
        }

        public async Task<EvenementClasseResponseDto?> GetByLibelleAsync(
            string libelle,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(libelle))
                throw new InvalidOperationException("Libelle est obligatoire.");

            var normalized = libelle.Trim();
            var classe = await _context.EvenementClasses
                .AsNoTracking()
                .Where(c => c.IdSociete == idSociete)
                .Where(c => c.Libelle.ToLower() == normalized.ToLower())
                .OrderBy(c => c.CodeClasse)
                .FirstOrDefaultAsync(cancellationToken);

            return classe == null ? null : EvenementClasseMapper.ToResponseDto(classe);
        }
    }
}
