using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantAvailabilityService : IRestaurantAvailabilityService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<RestaurantAvailabilityService> _logger;

        public RestaurantAvailabilityService(
            CongoTravelDbContext context,
            ILogger<RestaurantAvailabilityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RestaurantAvailabilityResponseDto?> GetAvailabilityAsync(
            int idRestaurantCreneau,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var creneau = await _context.RestaurantCreneaux
                .AsNoTracking()
                .Include(c => c.Societe)
                .Include(c => c.GlobalQuota)
                .Include(c => c.ZoneQuotas)
                    .ThenInclude(q => q.Zone)
                .Include(c => c.Restaurant)
                .FirstOrDefaultAsync(
                    c => c.IdRestaurantCreneau == idRestaurantCreneau && c.IdSociete == idSociete,
                    cancellationToken);

            if (creneau == null)
                return null;

            var response = new RestaurantAvailabilityResponseDto
            {
                IdRestaurantCreneau = creneau.IdRestaurantCreneau,
                IdSociete = creneau.IdSociete,
                NomSociete = creneau.Societe?.Nom,
                InventoryMode = creneau.InventoryMode.ToString(),
                Status = creneau.Status.ToString()
            };

            var acomptePourcent = creneau.Restaurant?.AcomptePourcentDefaut ?? 0m;

            switch (creneau.InventoryMode)
            {
                case RestaurantInventoryMode.GlobalQuota:
                    if (creneau.GlobalQuota == null)
                        throw new InvalidOperationException("Inventaire global manquant pour ce créneau.");

                    var disponible = Math.Max(
                        0,
                        creneau.GlobalQuota.CapaciteTotale
                        - creneau.GlobalQuota.QuantiteHold
                        - creneau.GlobalQuota.QuantiteVendue);

                    var acompteUnitaire = RestaurantAcompteHelper.ComputeAcompteUnitaire(
                        creneau.MontantAcompte,
                        creneau.GlobalQuota.PrixUnitaire,
                        acomptePourcent);

                    response.GlobalQuota = new RestaurantGlobalQuotaAvailabilityDto
                    {
                        CapaciteTotale = creneau.GlobalQuota.CapaciteTotale,
                        QuantiteHold = creneau.GlobalQuota.QuantiteHold,
                        QuantiteVendue = creneau.GlobalQuota.QuantiteVendue,
                        QuantiteDisponible = disponible,
                        PrixUnitaire = creneau.GlobalQuota.PrixUnitaire,
                        MontantAcompteUnitaire = acompteUnitaire,
                        CodeDevise = creneau.CodeDevise
                    };
                    break;

                case RestaurantInventoryMode.ClassQuota:
                    if (creneau.ZoneQuotas.Count == 0)
                        throw new InvalidOperationException("Inventaire zones manquant pour ce créneau.");

                    response.ZoneQuotas = creneau.ZoneQuotas
                        .OrderBy(q => q.IdRestaurantCreneauZoneQuota)
                        .Select(q =>
                        {
                            var restants = Math.Max(
                                0,
                                q.CapaciteTotale - q.QuantiteHold - q.QuantiteVendue);
                            return new RestaurantZoneQuotaAvailabilityDto
                            {
                                IdRestaurantCreneauZoneQuota = q.IdRestaurantCreneauZoneQuota,
                                IdRestaurantZone = q.IdRestaurantZone,
                                CodeZone = q.Zone?.Code,
                                LibelleZone = q.Zone?.Libelle ?? string.Empty,
                                CapaciteTotale = q.CapaciteTotale,
                                QuantiteHold = q.QuantiteHold,
                                QuantiteVendue = q.QuantiteVendue,
                                QuantiteDisponible = restants,
                                PrixUnitaire = q.PrixUnitaire,
                                MontantAcompteUnitaire = RestaurantAcompteHelper.ComputeAcompteUnitaire(
                                    creneau.MontantAcompte,
                                    q.PrixUnitaire,
                                    acomptePourcent),
                                CodeDevise = creneau.CodeDevise
                            };
                        })
                        .ToList();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(creneau.InventoryMode),
                        creneau.InventoryMode,
                        "Mode d'inventaire inconnu.");
            }

            _logger.LogDebug(
                "Availability créneau restaurant — Id={Id}, Mode={Mode}",
                creneau.IdRestaurantCreneau,
                creneau.InventoryMode);

            return response;
        }
    }
}
