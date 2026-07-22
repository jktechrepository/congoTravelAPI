using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using CongoTravel.Data;

namespace CongoTravel.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly CongoTravelDbContext _context;

        public DatabaseHealthCheck(CongoTravelDbContext context) => _context = context;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                return canConnect
                    ? HealthCheckResult.Healthy("Connexion MariaDB OK.")
                    : HealthCheckResult.Unhealthy("Impossible de se connecter à la base de données.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Erreur connexion base de données.", ex);
            }
        }
    }
}
