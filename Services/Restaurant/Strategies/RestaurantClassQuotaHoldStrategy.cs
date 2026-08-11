using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant.Strategies
{
    /// <summary>Mode ClassQuota : incrément atomique de QuantiteHold par zone.</summary>
    public class RestaurantClassQuotaHoldStrategy : IRestaurantInventoryHoldStrategy
    {
        private const string ReserveHoldSql = @"
UPDATE `RestaurantCreneauZoneQuotas`
SET `QuantiteHold` = `QuantiteHold` + {0}
WHERE `IdRestaurantCreneauZoneQuota` = {1}
  AND (`QuantiteHold` + `QuantiteVendue` + {0}) <= `CapaciteTotale`";

        private readonly CongoTravelDbContext _context;

        public RestaurantClassQuotaHoldStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public RestaurantInventoryMode SupportedMode => RestaurantInventoryMode.ClassQuota;

        public async Task<RestaurantHoldStrategyResult> ReserveHoldAsync(
            RestaurantInventoryHoldRequest request,
            CancellationToken cancellationToken = default)
        {
            var creneau = request.Creneau;
            if (creneau.InventoryMode != RestaurantInventoryMode.ClassQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie ClassQuota ne s'applique pas au mode {creneau.InventoryMode}.");
            }

            if (creneau.Status != RestaurantStatus.Published)
            {
                throw new InvalidOperationException(
                    "Le créneau doit être publié pour créer un hold.");
            }

            var aggregated = ValidateAndAggregateItems(request.Items);
            var quotas = await LoadZoneQuotasAsync(creneau, cancellationToken);
            var acomptePourcent = creneau.Restaurant?.AcomptePourcentDefaut ?? 0m;
            var codeDevise = string.IsNullOrWhiteSpace(request.CodeDevise)
                ? creneau.CodeDevise
                : request.CodeDevise.Trim().ToUpperInvariant();

            var lines = new List<RestaurantHoldLineResult>();
            decimal montantSousTotal = 0;
            var nombreCouverts = 0;

            foreach (var (zoneId, quantity) in aggregated)
            {
                var quota = quotas.FirstOrDefault(q => q.IdRestaurantZone == zoneId);
                if (quota == null)
                {
                    throw new InvalidOperationException(
                        $"Zone {zoneId} non configurée pour le créneau {creneau.IdRestaurantCreneau}.");
                }

                var reserved = await TryIncrementHoldAsync(
                    quota.IdRestaurantCreneauZoneQuota,
                    quantity,
                    cancellationToken);

                if (!reserved)
                {
                    throw new RestaurantHoldConflictException(
                        $"Capacité insuffisante pour {quantity} couvert(s) sur la zone {zoneId} (créneau {creneau.IdRestaurantCreneau}).");
                }

                var acompteUnitaire = RestaurantAcompteHelper.ComputeAcompteUnitaire(
                    creneau.MontantAcompte,
                    quota.PrixUnitaire,
                    acomptePourcent);
                var montantLigne = RestaurantAcompteHelper.ComputeAcompteTotal(acompteUnitaire, quantity);

                lines.Add(new RestaurantHoldLineResult
                {
                    LineType = RestaurantReservationLineType.ClassQuota,
                    Quantite = quantity,
                    PrixUnitaire = acompteUnitaire,
                    MontantLigne = montantLigne,
                    CodeDevise = codeDevise,
                    IdRestaurantCreneauZoneQuota = quota.IdRestaurantCreneauZoneQuota
                });

                montantSousTotal += montantLigne;
                nombreCouverts += quantity;
            }

            return new RestaurantHoldStrategyResult
            {
                Lines = lines,
                MontantSousTotal = montantSousTotal,
                NombreCouverts = nombreCouverts
            };
        }

        public static Dictionary<int, int> ValidateAndAggregateItems(IReadOnlyList<RestaurantHoldItemRequestDto> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new InvalidOperationException(
                    "Au moins un item est requis pour un hold ClassQuota.");
            }

            var aggregated = new Dictionary<int, int>();

            foreach (var item in items)
            {
                if (!item.ZoneId.HasValue || item.ZoneId.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "Mode ClassQuota : zoneId est obligatoire sur chaque item.");
                }

                if (item.Quantity <= 0)
                    throw new InvalidOperationException("La quantité doit être strictement positive.");

                aggregated[item.ZoneId.Value] = aggregated.GetValueOrDefault(item.ZoneId.Value) + item.Quantity;
            }

            return aggregated;
        }

        private async Task<List<RestaurantCreneauZoneQuota>> LoadZoneQuotasAsync(
            RestaurantCreneau creneau,
            CancellationToken cancellationToken)
        {
            if (creneau.ZoneQuotas.Count > 0)
                return creneau.ZoneQuotas.ToList();

            return await _context.RestaurantCreneauZoneQuotas
                .AsNoTracking()
                .Where(q => q.IdRestaurantCreneau == creneau.IdRestaurantCreneau)
                .ToListAsync(cancellationToken);
        }

        private async Task<bool> TryIncrementHoldAsync(
            int idRestaurantCreneauZoneQuota,
            int quantity,
            CancellationToken cancellationToken)
        {
            if (_context.Database.IsRelational()
                && string.Equals(
                    _context.Database.ProviderName,
                    "Pomelo.EntityFrameworkCore.MySql",
                    StringComparison.Ordinal))
            {
                var rows = await _context.Database.ExecuteSqlRawAsync(
                    ReserveHoldSql,
                    new object[] { quantity, idRestaurantCreneauZoneQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryIncrementHoldViaEfAsync(idRestaurantCreneauZoneQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryIncrementHoldViaEfAsync(
            int idRestaurantCreneauZoneQuota,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.RestaurantCreneauZoneQuotas
                .FirstOrDefaultAsync(
                    q => q.IdRestaurantCreneauZoneQuota == idRestaurantCreneauZoneQuota,
                    cancellationToken);

            if (quota == null)
                return false;

            if (quota.QuantiteHold + quota.QuantiteVendue + quantity > quota.CapaciteTotale)
                return false;

            quota.QuantiteHold += quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
