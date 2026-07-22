using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    /// <summary>Mode B : restitution stock hold ou vendu par quota classe à l'annulation.</summary>
    public class EvenementClassQuotaCancelStrategy : IEvenementInventoryCancelStrategy
    {
        private const string ReleaseHoldSql = @"
UPDATE `EvenementSessionClassQuotas`
SET `QuantiteHold` = GREATEST(0, `QuantiteHold` - {0})
WHERE `IdEvenementSessionClassQuota` = {1}
  AND `QuantiteHold` >= {0}";

        private const string ReleaseSoldSql = @"
UPDATE `EvenementSessionClassQuotas`
SET `QuantiteVendue` = GREATEST(0, `QuantiteVendue` - {0})
WHERE `IdEvenementSessionClassQuota` = {1}
  AND `QuantiteVendue` >= {0}";

        private readonly CongoTravelDbContext _context;

        public EvenementClassQuotaCancelStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public EvenementInventoryMode SupportedMode => EvenementInventoryMode.ClassQuota;

        public async Task ReleaseReservationAsync(
            EvenementInventoryCancelRequest request,
            CancellationToken cancellationToken = default)
        {
            var session = request.Session;
            if (session.InventoryMode != EvenementInventoryMode.ClassQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie ClassQuota ne s'applique pas au mode {session.InventoryMode}.");
            }

            var transfers = EvenementClassQuotaConfirmStrategy.GetClassQuotaLineTransfers(request.Reservation.Lines);

            foreach (var (idEvenementSessionClassQuota, quantity) in transfers)
            {
                var released = request.FromConfirmedSale
                    ? await TryReleaseSoldAsync(idEvenementSessionClassQuota, quantity, cancellationToken)
                    : await TryReleaseHoldAsync(idEvenementSessionClassQuota, quantity, cancellationToken);

                if (!released)
                {
                    var stockType = request.FromConfirmedSale ? "vendue" : "hold";
                    throw new EvenementHoldConflictException(
                        $"Impossible d'annuler : stock {stockType} insuffisant ({quantity} place(s)) sur le quota classe {idEvenementSessionClassQuota}.");
                }
            }
        }

        private async Task<bool> TryReleaseHoldAsync(
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
                    ReleaseHoldSql,
                    new object[] { quantity, idEvenementSessionClassQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseHoldViaEfAsync(idEvenementSessionClassQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseSoldAsync(
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
                    ReleaseSoldSql,
                    new object[] { quantity, idEvenementSessionClassQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseSoldViaEfAsync(idEvenementSessionClassQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseHoldViaEfAsync(
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
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<bool> TryReleaseSoldViaEfAsync(
            int idEvenementSessionClassQuota,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.EvenementSessionClassQuotas
                .FirstOrDefaultAsync(
                    q => q.IdEvenementSessionClassQuota == idEvenementSessionClassQuota,
                    cancellationToken);

            if (quota == null || quota.QuantiteVendue < quantity)
                return false;

            quota.QuantiteVendue -= quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
