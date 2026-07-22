using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    /// <summary>Mode B : transfert <c>QuantiteHold</c> → <c>QuantiteVendue</c> par quota classe.</summary>
    public class EvenementClassQuotaConfirmStrategy : IEvenementInventoryConfirmStrategy
    {
        private const string ConfirmHoldSql = @"
UPDATE `EvenementSessionClassQuotas`
SET `QuantiteHold` = `QuantiteHold` - {0},
    `QuantiteVendue` = `QuantiteVendue` + {0}
WHERE `IdEvenementSessionClassQuota` = {1}
  AND `QuantiteHold` >= {0}";

        private readonly CongoTravelDbContext _context;

        public EvenementClassQuotaConfirmStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public EvenementInventoryMode SupportedMode => EvenementInventoryMode.ClassQuota;

        public async Task ConfirmHoldAsync(
            EvenementInventoryConfirmRequest request,
            CancellationToken cancellationToken = default)
        {
            var session = request.Session;
            if (session.InventoryMode != EvenementInventoryMode.ClassQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie ClassQuota ne s'applique pas au mode {session.InventoryMode}.");
            }

            var transfers = GetClassQuotaLineTransfers(request.Reservation.Lines);

            foreach (var (idEvenementSessionClassQuota, quantity) in transfers)
            {
                var transferred = await TryTransferHoldToSoldAsync(
                    idEvenementSessionClassQuota,
                    quantity,
                    cancellationToken);

                if (!transferred)
                {
                    throw new EvenementHoldConflictException(
                        $"Impossible de confirmer : stock hold insuffisant ({quantity} place(s)) sur le quota classe {idEvenementSessionClassQuota}.");
                }
            }
        }

        public static IReadOnlyList<(int IdEvenementSessionClassQuota, int Quantite)> GetClassQuotaLineTransfers(
            IEnumerable<EvenementReservationLine> lines)
        {
            var transfers = new List<(int IdEvenementSessionClassQuota, int Quantite)>();

            foreach (var line in lines)
            {
                if (line.LineType != EvenementReservationLineType.ClassQuota)
                {
                    throw new InvalidOperationException(
                        "Mode ClassQuota : toutes les lignes doivent être de type ClassQuota.");
                }

                if (!line.IdEvenementSessionClassQuota.HasValue || line.IdEvenementSessionClassQuota.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "Ligne ClassQuota sans IdEvenementSessionClassQuota.");
                }

                if (line.Quantite <= 0)
                    throw new InvalidOperationException("Quantité de ligne invalide.");

                transfers.Add((line.IdEvenementSessionClassQuota.Value, line.Quantite));
            }

            if (transfers.Count == 0)
            {
                throw new InvalidOperationException(
                    "Aucune ligne ClassQuota valide pour confirmer cette réservation.");
            }

            return transfers;
        }

        private async Task<bool> TryTransferHoldToSoldAsync(
            int idEvenementSessionClassQuota,
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
                    new object[] { quantity, idEvenementSessionClassQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryTransferHoldToSoldViaEfAsync(idEvenementSessionClassQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryTransferHoldToSoldViaEfAsync(
            int idEvenementSessionClassQuota,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.EvenementSessionClassQuotas
                .FirstOrDefaultAsync(
                    q => q.IdEvenementSessionClassQuota == idEvenementSessionClassQuota,
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
