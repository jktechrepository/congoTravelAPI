using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    /// <summary>Mode C : restitution stock hold ou vendu à l'annulation.</summary>
    public class EvenementGlobalQuotaCancelStrategy : IEvenementInventoryCancelStrategy
    {
        private const string ReleaseHoldSql = @"
UPDATE `EvenementSessionGlobalQuotas`
SET `QuantiteHold` = GREATEST(0, `QuantiteHold` - {0})
WHERE `IdEvenementSession` = {1}
  AND `QuantiteHold` >= {0}";

        private const string ReleaseSoldSql = @"
UPDATE `EvenementSessionGlobalQuotas`
SET `QuantiteVendue` = GREATEST(0, `QuantiteVendue` - {0})
WHERE `IdEvenementSession` = {1}
  AND `QuantiteVendue` >= {0}";

        private readonly CongoTravelDbContext _context;

        public EvenementGlobalQuotaCancelStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public EvenementInventoryMode SupportedMode => EvenementInventoryMode.GlobalQuota;

        public async Task ReleaseReservationAsync(
            EvenementInventoryCancelRequest request,
            CancellationToken cancellationToken = default)
        {
            var session = request.Session;
            if (session.InventoryMode != EvenementInventoryMode.GlobalQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie GlobalQuota ne s'applique pas au mode {session.InventoryMode}.");
            }

            var totalQuantity = EvenementGlobalQuotaConfirmStrategy.SumGlobalQuotaQuantity(request.Reservation.Lines);
            var released = request.FromConfirmedSale
                ? await TryReleaseSoldAsync(session.IdEvenementSession, totalQuantity, cancellationToken)
                : await TryReleaseHoldAsync(session.IdEvenementSession, totalQuantity, cancellationToken);

            if (!released)
            {
                var stockType = request.FromConfirmedSale ? "vendue" : "hold";
                throw new EvenementHoldConflictException(
                    $"Impossible d'annuler : stock {stockType} insuffisant ({totalQuantity} place(s)) sur la session {session.IdEvenementSession}.");
            }
        }

        private async Task<bool> TryReleaseHoldAsync(
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
                    ReleaseHoldSql,
                    new object[] { quantity, idEvenementSession },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseHoldViaEfAsync(idEvenementSession, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseSoldAsync(
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
                    ReleaseSoldSql,
                    new object[] { quantity, idEvenementSession },
                    cancellationToken);
                return rows > 0;
            }

            return await TryReleaseSoldViaEfAsync(idEvenementSession, quantity, cancellationToken);
        }

        private async Task<bool> TryReleaseHoldViaEfAsync(
            int idEvenementSession,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.EvenementSessionGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdEvenementSession == idEvenementSession, cancellationToken);

            if (quota == null || quota.QuantiteHold < quantity)
                return false;

            quota.QuantiteHold -= quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<bool> TryReleaseSoldViaEfAsync(
            int idEvenementSession,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.EvenementSessionGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdEvenementSession == idEvenementSession, cancellationToken);

            if (quota == null || quota.QuantiteVendue < quantity)
                return false;

            quota.QuantiteVendue -= quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
