using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    /// <summary>Mode C : transfert <c>QuantiteHold</c> → <c>QuantiteVendue</c> à la confirmation.</summary>
    public class EvenementGlobalQuotaConfirmStrategy : IEvenementInventoryConfirmStrategy
    {
        private const string ConfirmHoldSql = @"
UPDATE `EvenementSessionGlobalQuotas`
SET `QuantiteHold` = `QuantiteHold` - {0},
    `QuantiteVendue` = `QuantiteVendue` + {0}
WHERE `IdEvenementSession` = {1}
  AND `QuantiteHold` >= {0}";

        private readonly CongoTravelDbContext _context;

        public EvenementGlobalQuotaConfirmStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public EvenementInventoryMode SupportedMode => EvenementInventoryMode.GlobalQuota;

        public async Task ConfirmHoldAsync(
            EvenementInventoryConfirmRequest request,
            CancellationToken cancellationToken = default)
        {
            var session = request.Session;
            if (session.InventoryMode != EvenementInventoryMode.GlobalQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie GlobalQuota ne s'applique pas au mode {session.InventoryMode}.");
            }

            var totalQuantity = SumGlobalQuotaQuantity(request.Reservation.Lines);
            if (totalQuantity <= 0)
            {
                throw new InvalidOperationException(
                    "Aucune ligne GlobalQuota valide pour confirmer cette réservation.");
            }

            var transferred = await TryTransferHoldToSoldAsync(
                session.IdEvenementSession,
                totalQuantity,
                cancellationToken);

            if (!transferred)
            {
                throw new EvenementHoldConflictException(
                    $"Impossible de confirmer : stock hold insuffisant ({totalQuantity} place(s)) sur la session {session.IdEvenementSession}.");
            }
        }

        public static int SumGlobalQuotaQuantity(IEnumerable<EvenementReservationLine> lines)
        {
            var total = 0;
            foreach (var line in lines)
            {
                if (line.LineType != EvenementReservationLineType.GlobalQuota)
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
            int idEvenementSession,
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
                    new object[] { quantity, idEvenementSession },
                    cancellationToken);
                return rows > 0;
            }

            return await TryTransferHoldToSoldViaEfAsync(idEvenementSession, quantity, cancellationToken);
        }

        private async Task<bool> TryTransferHoldToSoldViaEfAsync(
            int idEvenementSession,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.EvenementSessionGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdEvenementSession == idEvenementSession, cancellationToken);

            if (quota == null || quota.QuantiteHold < quantity)
                return false;

            quota.QuantiteHold -= quantity;
            quota.QuantiteVendue += quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
