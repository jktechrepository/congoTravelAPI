using CongoTravel.Data;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant.Strategies
{
    /// <summary>Mode GlobalQuota : incrément atomique de QuantiteHold.</summary>
    public class RestaurantGlobalQuotaHoldStrategy : IRestaurantInventoryHoldStrategy
    {
        private const string ReserveHoldSql = @"
UPDATE `RestaurantCreneauGlobalQuotas`
SET `QuantiteHold` = `QuantiteHold` + {0}
WHERE `IdRestaurantCreneau` = {1}
  AND (`QuantiteHold` + `QuantiteVendue` + {0}) <= `CapaciteTotale`";

        private readonly CongoTravelDbContext _context;

        public RestaurantGlobalQuotaHoldStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public RestaurantInventoryMode SupportedMode => RestaurantInventoryMode.GlobalQuota;

        public async Task<RestaurantHoldStrategyResult> ReserveHoldAsync(
            RestaurantInventoryHoldRequest request,
            CancellationToken cancellationToken = default)
        {
            var creneau = request.Creneau;
            if (creneau.InventoryMode != RestaurantInventoryMode.GlobalQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie GlobalQuota ne s'applique pas au mode {creneau.InventoryMode}.");
            }

            if (creneau.Status != RestaurantStatus.Published)
            {
                throw new InvalidOperationException(
                    "Le créneau doit être publié pour créer un hold.");
            }

            var totalQuantity = ValidateAndSumItems(request.Items);
            if (request.PrixUnitaire < 0)
                throw new InvalidOperationException("L'acompte unitaire ne peut pas être négatif.");

            var codeDevise = string.IsNullOrWhiteSpace(request.CodeDevise)
                ? creneau.CodeDevise
                : request.CodeDevise.Trim().ToUpperInvariant();

            var reserved = await TryIncrementHoldAsync(creneau.IdRestaurantCreneau, totalQuantity, cancellationToken);
            if (!reserved)
            {
                throw new RestaurantHoldConflictException(
                    $"Capacité insuffisante pour {totalQuantity} couvert(s) sur le créneau {creneau.IdRestaurantCreneau}.");
            }

            var montantLigne = RestaurantAcompteHelper.ComputeAcompteTotal(request.PrixUnitaire, totalQuantity);
            var line = new RestaurantHoldLineResult
            {
                LineType = RestaurantReservationLineType.GlobalQuota,
                Quantite = totalQuantity,
                PrixUnitaire = request.PrixUnitaire,
                MontantLigne = montantLigne,
                CodeDevise = codeDevise,
                IdRestaurantCreneauGlobalQuota = creneau.IdRestaurantCreneau
            };

            return new RestaurantHoldStrategyResult
            {
                Lines = new[] { line },
                MontantSousTotal = montantLigne,
                NombreCouverts = totalQuantity
            };
        }

        public static int ValidateAndSumItems(IReadOnlyList<RestaurantHoldItemRequestDto> items)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Au moins un item est requis pour un hold GlobalQuota.");

            var total = 0;
            foreach (var item in items)
            {
                if (item.ZoneId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Mode GlobalQuota : les items ne doivent pas contenir zoneId.");
                }

                if (item.Quantity <= 0)
                    throw new InvalidOperationException("La quantité doit être strictement positive.");

                total += item.Quantity;
            }

            return total;
        }

        private async Task<bool> TryIncrementHoldAsync(
            int idRestaurantCreneau,
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
                    new object[] { quantity, idRestaurantCreneau },
                    cancellationToken);
                return rows > 0;
            }

            return await TryIncrementHoldViaEfAsync(idRestaurantCreneau, quantity, cancellationToken);
        }

        private async Task<bool> TryIncrementHoldViaEfAsync(
            int idRestaurantCreneau,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.RestaurantCreneauGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdRestaurantCreneau == idRestaurantCreneau, cancellationToken);

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
