using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant.Strategies
{
    /// <summary>Mode ClassQuota : transfert QuantiteHold → QuantiteVendue par zone.</summary>
    public class RestaurantClassQuotaConfirmStrategy : IRestaurantInventoryConfirmStrategy
    {
        private const string ConfirmHoldSql = @"
UPDATE `RestaurantCreneauZoneQuotas`
SET `QuantiteHold` = `QuantiteHold` - {0},
    `QuantiteVendue` = `QuantiteVendue` + {0}
WHERE `IdRestaurantCreneauZoneQuota` = {1}
  AND `QuantiteHold` >= {0}";

        private readonly CongoTravelDbContext _context;

        public RestaurantClassQuotaConfirmStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public RestaurantInventoryMode SupportedMode => RestaurantInventoryMode.ClassQuota;

        public async Task ConfirmHoldAsync(
            RestaurantInventoryConfirmRequest request,
            CancellationToken cancellationToken = default)
        {
            var creneau = request.Creneau;
            if (creneau.InventoryMode != RestaurantInventoryMode.ClassQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie ClassQuota ne s'applique pas au mode {creneau.InventoryMode}.");
            }

            var transfers = GetClassQuotaLineTransfers(request.Reservation.Lines);

            foreach (var (idZoneQuota, quantity) in transfers)
            {
                var transferred = await TryTransferHoldToSoldAsync(idZoneQuota, quantity, cancellationToken);
                if (!transferred)
                {
                    throw new RestaurantHoldConflictException(
                        $"Impossible de confirmer : stock hold insuffisant ({quantity} couvert(s)) sur le zoneQuota {idZoneQuota}.");
                }
            }
        }

        public static IReadOnlyList<(int IdRestaurantCreneauZoneQuota, int Quantite)> GetClassQuotaLineTransfers(
            IEnumerable<RestaurantReservationLine> lines)
        {
            var transfers = new List<(int IdRestaurantCreneauZoneQuota, int Quantite)>();

            foreach (var line in lines)
            {
                if (line.LineType != RestaurantReservationLineType.ClassQuota)
                {
                    throw new InvalidOperationException(
                        "Mode ClassQuota : toutes les lignes doivent être de type ClassQuota.");
                }

                if (!line.IdRestaurantCreneauZoneQuota.HasValue || line.IdRestaurantCreneauZoneQuota.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "Ligne ClassQuota sans IdRestaurantCreneauZoneQuota.");
                }

                if (line.Quantite <= 0)
                    throw new InvalidOperationException("Quantité de ligne invalide.");

                transfers.Add((line.IdRestaurantCreneauZoneQuota.Value, line.Quantite));
            }

            if (transfers.Count == 0)
            {
                throw new InvalidOperationException(
                    "Aucune ligne ClassQuota valide pour confirmer cette réservation.");
            }

            return transfers;
        }

        private async Task<bool> TryTransferHoldToSoldAsync(
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
                    ConfirmHoldSql,
                    new object[] { quantity, idRestaurantCreneauZoneQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryTransferHoldToSoldViaEfAsync(idRestaurantCreneauZoneQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryTransferHoldToSoldViaEfAsync(
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
            quota.QuantiteVendue += quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
