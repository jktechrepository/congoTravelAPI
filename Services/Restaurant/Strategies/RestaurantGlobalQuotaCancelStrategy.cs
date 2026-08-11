using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant.Strategies
{
    /// <summary>Mode GlobalQuota : restitution stock hold ou vendu à l'annulation.</summary>
    public class RestaurantGlobalQuotaCancelStrategy : IRestaurantInventoryCancelStrategy
    {
        private const string ReleaseHoldSql = @"
UPDATE `RestaurantCreneauGlobalQuotas`
SET `QuantiteHold` = GREATEST(0, `QuantiteHold` - {0})
WHERE `IdRestaurantCreneau` = {1}
  AND `QuantiteHold` >= {0}";

        private const string ReleaseSoldSql = @"
UPDATE `RestaurantCreneauGlobalQuotas`
SET `QuantiteVendue` = GREATEST(0, `QuantiteVendue` - {0})
WHERE `IdRestaurantCreneau` = {1}
  AND `QuantiteVendue` >= {0}";

        private readonly CongoTravelDbContext _context;

        public RestaurantGlobalQuotaCancelStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public RestaurantInventoryMode SupportedMode => RestaurantInventoryMode.GlobalQuota;

        public async Task ReleaseReservationAsync(
            RestaurantInventoryCancelRequest request,
            CancellationToken cancellationToken = default)
        {
            var creneau = request.Creneau;
            if (creneau.InventoryMode != RestaurantInventoryMode.GlobalQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie GlobalQuota ne s'applique pas au mode {creneau.InventoryMode}.");
            }

            var totalQuantity = RestaurantGlobalQuotaConfirmStrategy.SumGlobalQuotaQuantity(request.Reservation.Lines);
            var released = request.FromConfirmedSale
                ? await TryReleaseSoldAsync(creneau.IdRestaurantCreneau, totalQuantity, cancellationToken)
                : await TryReleaseHoldAsync(creneau.IdRestaurantCreneau, totalQuantity, cancellationToken);

            if (!released)
            {
                var stockType = request.FromConfirmedSale ? "vendue" : "hold";
                throw new RestaurantHoldConflictException(
                    $"Impossible d'annuler : stock {stockType} insuffisant ({totalQuantity} couvert(s)) sur le créneau {creneau.IdRestaurantCreneau}.");
            }
        }

        private async Task<bool> TryReleaseHoldAsync(
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
                    ReleaseHoldSql,
                    new object[] { quantity, idRestaurantCreneau },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseHoldViaEfAsync(idRestaurantCreneau, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseSoldAsync(
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
                    ReleaseSoldSql,
                    new object[] { quantity, idRestaurantCreneau },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseSoldViaEfAsync(idRestaurantCreneau, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseHoldViaEfAsync(
            int idRestaurantCreneau,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.RestaurantCreneauGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdRestaurantCreneau == idRestaurantCreneau, cancellationToken);

            if (quota == null || quota.QuantiteHold < quantity)
                return false;

            quota.QuantiteHold -= quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<bool> TryReleaseSoldViaEfAsync(
            int idRestaurantCreneau,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.RestaurantCreneauGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdRestaurantCreneau == idRestaurantCreneau, cancellationToken);

            if (quota == null || quota.QuantiteVendue < quantity)
                return false;

            quota.QuantiteVendue -= quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
