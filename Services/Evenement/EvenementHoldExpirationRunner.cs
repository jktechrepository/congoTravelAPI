using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement
{
    public class EvenementHoldExpirationRunner : IEvenementHoldExpirationRunner
    {
        private const string ExpireProcedureSql = "CALL `sp_ExpireEvenementHolds`()";

        private readonly ILogger<EvenementHoldExpirationRunner> _logger;

        public EvenementHoldExpirationRunner(ILogger<EvenementHoldExpirationRunner> logger)
        {
            _logger = logger;
        }

        public async Task ExpireHoldsAsync(CongoTravelDbContext context, CancellationToken cancellationToken = default)
        {
            if (!context.Database.IsRelational())
            {
                _logger.LogDebug("Expiration holds événement ignorée (base non relationnelle).");
                return;
            }

            if (!string.Equals(context.Database.ProviderName, "Pomelo.EntityFrameworkCore.MySql", StringComparison.Ordinal))
            {
                _logger.LogDebug(
                    "Expiration holds événement ignorée (provider {Provider}).",
                    context.Database.ProviderName);
                return;
            }

            var pendingCount = await context.EvenementReservations
                .AsNoTracking()
                .CountAsync(
                    r => r.Status == EvenementReservationStatus.HOLD
                         && r.ExpiresAtUtc != null
                         && r.ExpiresAtUtc < DateTime.UtcNow,
                    cancellationToken);

            if (pendingCount == 0)
            {
                _logger.LogDebug("Expiration holds événement : aucun HOLD expiré en attente.");
                return;
            }

            try
            {
                await context.Database.ExecuteSqlRawAsync(ExpireProcedureSql, cancellationToken);
                _logger.LogInformation(
                    "Expiration holds événement : {PendingCount} réservation(s) HOLD expirée(s) traitée(s) via sp_ExpireEvenementHolds.",
                    pendingCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Échec sp_ExpireEvenementHolds ({PendingCount} HOLD en attente). Vérifier Scripts/production_evenement_hold_expiration_job.sql.",
                    pendingCount);
            }
        }
    }
}
