using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    /// <summary>Mode ClassQuota : transfert <c>QuantiteHold</c> → <c>QuantiteVendue</c> par quota classe.</summary>
    public class SiteTouristiqueClassQuotaConfirmStrategy : ISiteTouristiqueInventoryConfirmStrategy
    {
        private const string ConfirmHoldSql = @"
UPDATE `SiteTouristiqueClassQuotas`
SET `QuantiteHold` = `QuantiteHold` - {0},
    `QuantiteVendue` = `QuantiteVendue` + {0}
WHERE `IdSiteTouristiqueClassQuota` = {1}
  AND `QuantiteHold` >= {0}";

        private readonly CongoTravelDbContext _context;

        public SiteTouristiqueClassQuotaConfirmStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public SiteTouristiqueInventoryMode SupportedMode => SiteTouristiqueInventoryMode.ClassQuota;

        public async Task ConfirmHoldAsync(
            SiteTouristiqueInventoryConfirmRequest request,
            CancellationToken cancellationToken = default)
        {
            var journee = request.Journee;
            if (journee.InventoryMode != SiteTouristiqueInventoryMode.ClassQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie ClassQuota ne s'applique pas au mode {journee.InventoryMode}.");
            }

            var transfers = GetClassQuotaLineTransfers(request.Reservation.Lines);

            foreach (var (idSiteTouristiqueClassQuota, quantity) in transfers)
            {
                var transferred = await TryTransferHoldToSoldAsync(
                    idSiteTouristiqueClassQuota,
                    quantity,
                    cancellationToken);

                if (!transferred)
                {
                    throw new SiteTouristiqueHoldConflictException(
                        $"Impossible de confirmer : stock hold insuffisant ({quantity} place(s)) sur le quota classe {idSiteTouristiqueClassQuota}.");
                }
            }
        }

        public static IReadOnlyList<(int IdSiteTouristiqueClassQuota, int Quantite)> GetClassQuotaLineTransfers(
            IEnumerable<SiteTouristiqueReservationLine> lines)
        {
            var transfers = new List<(int IdSiteTouristiqueClassQuota, int Quantite)>();

            foreach (var line in lines)
            {
                if (line.LineType != SiteTouristiqueReservationLineType.ClassQuota)
                {
                    throw new InvalidOperationException(
                        "Mode ClassQuota : toutes les lignes doivent être de type ClassQuota.");
                }

                if (!line.IdSiteTouristiqueClassQuota.HasValue || line.IdSiteTouristiqueClassQuota.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "Ligne ClassQuota sans IdSiteTouristiqueClassQuota.");
                }

                if (line.Quantite <= 0)
                    throw new InvalidOperationException("Quantité de ligne invalide.");

                transfers.Add((line.IdSiteTouristiqueClassQuota.Value, line.Quantite));
            }

            if (transfers.Count == 0)
            {
                throw new InvalidOperationException(
                    "Aucune ligne ClassQuota valide pour confirmer cette réservation.");
            }

            return transfers;
        }

        private async Task<bool> TryTransferHoldToSoldAsync(
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
                    ConfirmHoldSql,
                    new object[] { quantity, idSiteTouristiqueClassQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryTransferHoldToSoldViaEfAsync(idSiteTouristiqueClassQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryTransferHoldToSoldViaEfAsync(
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
            quota.QuantiteVendue += quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
