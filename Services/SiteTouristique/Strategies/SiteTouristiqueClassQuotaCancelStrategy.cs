using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    /// <summary>Mode ClassQuota : restitution stock hold ou vendu par quota classe à l'annulation.</summary>
    public class SiteTouristiqueClassQuotaCancelStrategy : ISiteTouristiqueInventoryCancelStrategy
    {
        private const string ReleaseHoldSql = @"
UPDATE `SiteTouristiqueClassQuotas`
SET `QuantiteHold` = GREATEST(0, `QuantiteHold` - {0})
WHERE `IdSiteTouristiqueClassQuota` = {1}
  AND `QuantiteHold` >= {0}";

        private const string ReleaseSoldSql = @"
UPDATE `SiteTouristiqueClassQuotas`
SET `QuantiteVendue` = GREATEST(0, `QuantiteVendue` - {0})
WHERE `IdSiteTouristiqueClassQuota` = {1}
  AND `QuantiteVendue` >= {0}";

        private readonly CongoTravelDbContext _context;

        public SiteTouristiqueClassQuotaCancelStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public SiteTouristiqueInventoryMode SupportedMode => SiteTouristiqueInventoryMode.ClassQuota;

        public async Task ReleaseReservationAsync(
            SiteTouristiqueInventoryCancelRequest request,
            CancellationToken cancellationToken = default)
        {
            var journee = request.Journee;
            if (journee.InventoryMode != SiteTouristiqueInventoryMode.ClassQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie ClassQuota ne s'applique pas au mode {journee.InventoryMode}.");
            }

            var transfers = SiteTouristiqueClassQuotaConfirmStrategy.GetClassQuotaLineTransfers(request.Reservation.Lines);

            foreach (var (idSiteTouristiqueClassQuota, quantity) in transfers)
            {
                var released = request.FromConfirmedSale
                    ? await TryReleaseSoldAsync(idSiteTouristiqueClassQuota, quantity, cancellationToken)
                    : await TryReleaseHoldAsync(idSiteTouristiqueClassQuota, quantity, cancellationToken);

                if (!released)
                {
                    var stockType = request.FromConfirmedSale ? "vendue" : "hold";
                    throw new SiteTouristiqueHoldConflictException(
                        $"Impossible d'annuler : stock {stockType} insuffisant ({quantity} place(s)) sur le quota classe {idSiteTouristiqueClassQuota}.");
                }
            }
        }

        private async Task<bool> TryReleaseHoldAsync(
            int idSiteTouristiqueClassQuota,
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
                    new object[] { quantity, idSiteTouristiqueClassQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseHoldViaEfAsync(idSiteTouristiqueClassQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseSoldAsync(
            int idSiteTouristiqueClassQuota,
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
                    new object[] { quantity, idSiteTouristiqueClassQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseSoldViaEfAsync(idSiteTouristiqueClassQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseHoldViaEfAsync(
            int idSiteTouristiqueClassQuota,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.SiteTouristiqueClassQuotas
                .FirstOrDefaultAsync(
                    q => q.IdSiteTouristiqueClassQuota == idSiteTouristiqueClassQuota,
                    cancellationToken);

            if (quota == null || quota.QuantiteHold < quantity)
                return false;

            quota.QuantiteHold -= quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<bool> TryReleaseSoldViaEfAsync(
            int idSiteTouristiqueClassQuota,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.SiteTouristiqueClassQuotas
                .FirstOrDefaultAsync(
                    q => q.IdSiteTouristiqueClassQuota == idSiteTouristiqueClassQuota,
                    cancellationToken);

            if (quota == null || quota.QuantiteVendue < quantity)
                return false;

            quota.QuantiteVendue -= quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
