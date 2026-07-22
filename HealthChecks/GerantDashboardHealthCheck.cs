using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using CongoTravel.Services;
using System.Threading;
using System.Threading.Tasks;

namespace CongoTravel.HealthChecks
{
    /// <summary>
    /// Health check pour valider que le GerantDashboardService est correctement injecté et fonctionnel
    /// </summary>
    public class GerantDashboardHealthCheck : IHealthCheck
    {
        private readonly GerantDashboardService _gerantDashboardService;
        private readonly ILogger<GerantDashboardHealthCheck> _logger;

        public GerantDashboardHealthCheck(
            GerantDashboardService gerantDashboardService,
            ILogger<GerantDashboardHealthCheck> logger)
        {
            _gerantDashboardService = gerantDashboardService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Vérification du health check du GerantDashboardService");

                // Vérifier que le service est injecté
                if (_gerantDashboardService == null)
                {
                    return HealthCheckResult.Unhealthy(
                        "Le GerantDashboardService n'est pas correctement injecté");
                }

                // Test simple : vérifier que le service peut être appelé
                // Nous utilisons une société fictive (ID 1) pour le test
                var testResult = await _gerantDashboardService.GetSocieteStatistiquesAsync(1, cancellationToken);

                if (testResult == null)
                {
                    return HealthCheckResult.Degraded(
                        "Le GerantDashboardService répond mais retourne des données null");
                }

                _logger.LogDebug("GerantDashboardService health check réussi");

                return HealthCheckResult.Healthy(
                    "Le GerantDashboardService fonctionne correctement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du health check du GerantDashboardService");
                
                return HealthCheckResult.Unhealthy(
                    "Erreur lors de la vérification du GerantDashboardService",
                    ex);
            }
        }
    }
}
