using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    /// <summary>Mode GlobalQuota : transfert <c>QuantiteHold</c> → <c>QuantiteVendue</c> à la confirmation.</summary>
    public class SiteTouristiqueGlobalQuotaConfirmStrategy : ISiteTouristiqueInventoryConfirmStrategy
    {
        private const string ConfirmHoldSql = @"
UPDATE `SiteTouristiqueGlobalQuotas`
SET `QuantiteHold` = `QuantiteHold` - {0},
    `QuantiteVendue` = `QuantiteVendue` + {0}
WHERE `IdSiteTouristiqueJournee` = {1}
  AND `QuantiteHold` >= {0}";

        private readonly CongoTravelDbContext _context;

        public SiteTouristiqueGlobalQuotaConfirmStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public SiteTouristiqueInventoryMode SupportedMode => SiteTouristiqueInventoryMode.GlobalQuota;

        public async Task ConfirmHoldAsync(
            SiteTouristiqueInventoryConfirmRequest request,
            CancellationToken cancellationToken = default)
        {
            var journee = request.Journee;
            if (journee.InventoryMode != SiteTouristiqueInventoryMode.GlobalQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie GlobalQuota ne s'applique pas au mode {journee.InventoryMode}.");
            }

            var totalQuantity = SumGlobalQuotaQuantity(request.Reservation.Lines);
            if (totalQuantity <= 0)
            {
                throw new InvalidOperationException(
                    "Aucune ligne GlobalQuota valide pour confirmer cette réservation.");
            }

            var transferred = await TryTransferHoldToSoldAsync(
                journee.IdSiteTouristiqueJournee,
                totalQuantity,
                cancellationToken);

            if (!transferred)
            {
                throw new SiteTouristiqueHoldConflictException(
                    $"Impossible de confirmer : stock hold insuffisant ({totalQuantity} place(s)) sur la journée {journee.IdSiteTouristiqueJournee}.");
            }
        }

        public static int SumGlobalQuotaQuantity(IEnumerable<SiteTouristiqueReservationLine> lines)
        {
            var total = 0;
            foreach (var line in lines)
            {
                if (line.LineType != SiteTouristiqueReservationLineType.GlobalQuota)
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
                    ConfirmHoldSql,
                    new object[] { quantity, idSiteTouristiqueJournee },
                    cancellationToken);
                return rows > 0;
            }

            return await TryTransferHoldToSoldViaEfAsync(idSiteTouristiqueJournee, quantity, cancellationToken);
        }

        private async Task<bool> TryTransferHoldToSoldViaEfAsync(
            int idSiteTouristiqueJournee,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.SiteTouristiqueGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdSiteTouristiqueJournee == idSiteTouristiqueJournee, cancellationToken);

            if (quota == null || quota.QuantiteHold < quantity)
                return false;

            quota.QuantiteHold -= quantity;
            quota.QuantiteVendue += quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
