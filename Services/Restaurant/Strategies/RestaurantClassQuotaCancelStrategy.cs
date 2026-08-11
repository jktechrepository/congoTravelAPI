using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant.Strategies
{
    /// <summary>Mode ClassQuota : restitution stock hold ou vendu par zone à l'annulation.</summary>
    public class RestaurantClassQuotaCancelStrategy : IRestaurantInventoryCancelStrategy
    {
        private const string ReleaseHoldSql = @"
UPDATE `RestaurantCreneauZoneQuotas`
SET `QuantiteHold` = GREATEST(0, `QuantiteHold` - {0})
WHERE `IdRestaurantCreneauZoneQuota` = {1}
  AND `QuantiteHold` >= {0}";

        private const string ReleaseSoldSql = @"
UPDATE `RestaurantCreneauZoneQuotas`
SET `QuantiteVendue` = GREATEST(0, `QuantiteVendue` - {0})
WHERE `IdRestaurantCreneauZoneQuota` = {1}
  AND `QuantiteVendue` >= {0}";

        private readonly CongoTravelDbContext _context;

        public RestaurantClassQuotaCancelStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public RestaurantInventoryMode SupportedMode => RestaurantInventoryMode.ClassQuota;

        public async Task ReleaseReservationAsync(
            RestaurantInventoryCancelRequest request,
            CancellationToken cancellationToken = default)
        {
            var creneau = request.Creneau;
            if (creneau.InventoryMode != RestaurantInventoryMode.ClassQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie ClassQuota ne s'applique pas au mode {creneau.InventoryMode}.");
            }

            var transfers = RestaurantClassQuotaConfirmStrategy.GetClassQuotaLineTransfers(request.Reservation.Lines);

            foreach (var (idZoneQuota, quantity) in transfers)
            {
                var released = request.FromConfirmedSale
                    ? await TryReleaseSoldAsync(idZoneQuota, quantity, cancellationToken)
                    : await TryReleaseHoldAsync(idZoneQuota, quantity, cancellationToken);

                if (!released)
                {
                    var stockType = request.FromConfirmedSale ? "vendue" : "hold";
                    throw new RestaurantHoldConflictException(
                        $"Impossible d'annuler : stock {stockType} insuffisant ({quantity} couvert(s)) sur le zoneQuota {idZoneQuota}.");
                }
            }
        }

        private async Task<bool> TryReleaseHoldAsync(
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
                    ReleaseHoldSql,
                    new object[] { quantity, idRestaurantCreneauZoneQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseHoldViaEfAsync(idRestaurantCreneauZoneQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseSoldAsync(
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
                    ReleaseSoldSql,
                    new object[] { quantity, idRestaurantCreneauZoneQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseSoldViaEfAsync(idRestaurantCreneauZoneQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseHoldViaEfAsync(
            int idRestaurantCreneauZoneQuota,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.RestaurantCreneauZoneQuotas
                .FirstOrDefaultAsync(
                    q => q.IdRestaurantCreneauZoneQuota == idRestaurantCreneauZoneQuota,
                    cancellationToken);

            if (quota == null || quota.QuantiteHold < quantity)
                return false;

            quota.QuantiteHold -= quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<bool> TryReleaseSoldViaEfAsync(
            int idRestaurantCreneauZoneQuota,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.RestaurantCreneauZoneQuotas
                .FirstOrDefaultAsync(
                    q => q.IdRestaurantCreneauZoneQuota == idRestaurantCreneauZoneQuota,
                    cancellationToken);

            if (quota == null || quota.QuantiteVendue < quantity)
                return false;

            quota.QuantiteVendue -= quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
