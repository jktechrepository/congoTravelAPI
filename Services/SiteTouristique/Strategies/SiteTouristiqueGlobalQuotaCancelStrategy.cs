using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    /// <summary>Mode GlobalQuota : restitution stock hold ou vendu à l'annulation.</summary>
    public class SiteTouristiqueGlobalQuotaCancelStrategy : ISiteTouristiqueInventoryCancelStrategy
    {
        private const string ReleaseHoldSql = @"
UPDATE `SiteTouristiqueGlobalQuotas`
SET `QuantiteHold` = GREATEST(0, `QuantiteHold` - {0})
WHERE `IdSiteTouristiqueJournee` = {1}
  AND `QuantiteHold` >= {0}";

        private const string ReleaseSoldSql = @"
UPDATE `SiteTouristiqueGlobalQuotas`
SET `QuantiteVendue` = GREATEST(0, `QuantiteVendue` - {0})
WHERE `IdSiteTouristiqueJournee` = {1}
  AND `QuantiteVendue` >= {0}";

        private readonly CongoTravelDbContext _context;

        public SiteTouristiqueGlobalQuotaCancelStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public SiteTouristiqueInventoryMode SupportedMode => SiteTouristiqueInventoryMode.GlobalQuota;

        public async Task ReleaseReservationAsync(
            SiteTouristiqueInventoryCancelRequest request,
            CancellationToken cancellationToken = default)
        {
            var journee = request.Journee;
            if (journee.InventoryMode != SiteTouristiqueInventoryMode.GlobalQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie GlobalQuota ne s'applique pas au mode {journee.InventoryMode}.");
            }

            var totalQuantity = SiteTouristiqueGlobalQuotaConfirmStrategy.SumGlobalQuotaQuantity(request.Reservation.Lines);
            var released = request.FromConfirmedSale
                ? await TryReleaseSoldAsync(journee.IdSiteTouristiqueJournee, totalQuantity, cancellationToken)
                : await TryReleaseHoldAsync(journee.IdSiteTouristiqueJournee, totalQuantity, cancellationToken);

            if (!released)
            {
                var stockType = request.FromConfirmedSale ? "vendue" : "hold";
                throw new SiteTouristiqueHoldConflictException(
                    $"Impossible d'annuler : stock {stockType} insuffisant ({totalQuantity} place(s)) sur la journée {journee.IdSiteTouristiqueJournee}.");
            }
        }

        private async Task<bool> TryReleaseHoldAsync(
            int idSiteTouristiqueJournee,
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
                    new object[] { quantity, idSiteTouristiqueJournee },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseHoldViaEfAsync(idSiteTouristiqueJournee, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseSoldAsync(
            int idSiteTouristiqueJournee,
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
                    new object[] { quantity, idSiteTouristiqueJournee },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseSoldViaEfAsync(idSiteTouristiqueJournee, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseHoldViaEfAsync(
            int idSiteTouristiqueJournee,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.SiteTouristiqueGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdSiteTouristiqueJournee == idSiteTouristiqueJournee, cancellationToken);

            if (quota == null || quota.QuantiteHold < quantity)
                return false;

            quota.QuantiteHold -= quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<bool> TryReleaseSoldViaEfAsync(
            int idSiteTouristiqueJournee,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.SiteTouristiqueGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdSiteTouristiqueJournee == idSiteTouristiqueJournee, cancellationToken);

            if (quota == null || quota.QuantiteVendue < quantity)
                return false;

            quota.QuantiteVendue -= quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
