using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant.Strategies
{
    /// <summary>Mode GlobalQuota : transfert QuantiteHold → QuantiteVendue à la confirmation.</summary>
    public class RestaurantGlobalQuotaConfirmStrategy : IRestaurantInventoryConfirmStrategy
    {
        private const string ConfirmHoldSql = @"
UPDATE `RestaurantCreneauGlobalQuotas`
SET `QuantiteHold` = `QuantiteHold` - {0},
    `QuantiteVendue` = `QuantiteVendue` + {0}
WHERE `IdRestaurantCreneau` = {1}
  AND `QuantiteHold` >= {0}";

        private readonly CongoTravelDbContext _context;

        public RestaurantGlobalQuotaConfirmStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public RestaurantInventoryMode SupportedMode => RestaurantInventoryMode.GlobalQuota;

        public async Task ConfirmHoldAsync(
            RestaurantInventoryConfirmRequest request,
            CancellationToken cancellationToken = default)
        {
            var creneau = request.Creneau;
            if (creneau.InventoryMode != RestaurantInventoryMode.GlobalQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie GlobalQuota ne s'applique pas au mode {creneau.InventoryMode}.");
            }

            var totalQuantity = SumGlobalQuotaQuantity(request.Reservation.Lines);
            if (totalQuantity <= 0)
            {
                throw new InvalidOperationException(
                    "Aucune ligne GlobalQuota valide pour confirmer cette réservation.");
            }

            var transferred = await TryTransferHoldToSoldAsync(
                creneau.IdRestaurantCreneau,
                totalQuantity,
                cancellationToken);

            if (!transferred)
            {
                throw new RestaurantHoldConflictException(
                    $"Impossible de confirmer : stock hold insuffisant ({totalQuantity} couvert(s)) sur le créneau {creneau.IdRestaurantCreneau}.");
            }
        }

        public static int SumGlobalQuotaQuantity(IEnumerable<RestaurantReservationLine> lines)
        {
            var total = 0;
            foreach (var line in lines)
            {
                if (line.LineType != RestaurantReservationLineType.GlobalQuota)
                {
                    throw new InvalidOperationException(
                        "Mode GlobalQuota : toutes les lignes doivent être de type GlobalQuota.");
                }

                if (line.Quantite <= 0)
                    throw new InvalidOperationException("Quantité de ligne invalide.");

                total += line.Quantite;
            }

            return total;
        }

        private async Task<bool> TryTransferHoldToSoldAsync(
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
                    ConfirmHoldSql,
                    new object[] { quantity, idRestaurantCreneau },
                    cancellationToken);
                return rows > 0;
            }

            return await TryTransferHoldToSoldViaEfAsync(idRestaurantCreneau, quantity, cancellationToken);
        }

        private async Task<bool> TryTransferHoldToSoldViaEfAsync(
            int idRestaurantCreneau,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.RestaurantCreneauGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdRestaurantCreneau == idRestaurantCreneau, cancellationToken);

            if (quota == null || quota.QuantiteHold < quantity)
                return false;

            quota.QuantiteHold -= quantity;
            quota.QuantiteVendue += quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
