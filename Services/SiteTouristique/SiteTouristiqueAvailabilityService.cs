using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueAvailabilityService : ISiteTouristiqueAvailabilityService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<SiteTouristiqueAvailabilityService> _logger;

        public SiteTouristiqueAvailabilityService(
            CongoTravelDbContext context,
            ILogger<SiteTouristiqueAvailabilityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SiteTouristiqueAvailabilityResponseDto?> GetJourneeAvailabilityAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var journee = await _context.SiteTouristiqueJournees
                .AsNoTracking()
                .Include(j => j.Societe)
                .Include(j => j.GlobalQuota)
                .Include(j => j.ClassQuotas)
                    .ThenInclude(q => q.Classe)
                .FirstOrDefaultAsync(
                    j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && j.IdSociete == idSociete,
                    cancellationToken);

            if (journee == null)
                return null;

            var response = new SiteTouristiqueAvailabilityResponseDto
            {
                IdSiteTouristiqueJournee = journee.IdSiteTouristiqueJournee,
                IdSociete = journee.IdSociete,
                NomSociete = journee.Societe?.Nom,
                InventoryMode = journee.InventoryMode.ToString(),
                Status = journee.Status.ToString()
            };

            switch (journee.InventoryMode)
            {
                case SiteTouristiqueInventoryMode.GlobalQuota:
                    if (journee.GlobalQuota == null)
                        throw new InvalidOperationException("Inventaire global manquant pour cette journée.");

                    response.GlobalQuota = SiteTouristiqueJourneeMapper.ToGlobalQuotaAvailability(
                        journee.GlobalQuota, journee.CodeDevise);
                    break;

                case SiteTouristiqueInventoryMode.ClassQuota:
                    if (journee.ClassQuotas.Count == 0)
                        throw new InvalidOperationException("Inventaire par classe manquant pour cette journée.");

                    response.ClassQuotas = journee.ClassQuotas
                        .OrderBy(q => q.IdSiteTouristiqueClassQuota)
                        .Select(q => SiteTouristiqueJourneeMapper.ToClassQuotaAvailability(q, journee.CodeDevise))
                        .ToList();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(journee.InventoryMode),
                        journee.InventoryMode,
                        "Mode d'inventaire inconnu.");
            }

            _logger.LogDebug(
                "Availability journée site touristique — Id={Id}, Mode={Mode}",
                journee.IdSiteTouristiqueJournee,
                journee.InventoryMode);

            return response;
        }
    }
}
