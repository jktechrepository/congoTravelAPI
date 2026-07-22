using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using CongoTravel.Configuration;

namespace CongoTravel.HealthChecks
{
    public class FlexPayConfigHealthCheck : IHealthCheck
    {
        private readonly FlexPayOptions _options;

        public FlexPayConfigHealthCheck(IOptions<FlexPayOptions> options) =>
            _options = options.Value;

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "FlexPay désactivé (paiement électronique indisponible)."));
            }

            if (string.IsNullOrWhiteSpace(_options.MobileMoneyUrl) || string.IsNullOrWhiteSpace(_options.ApiToken))
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Configuration FlexPay incomplète (MobileMoneyUrl ou ApiToken manquant)."));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Configuration FlexPay présente."));
        }
    }
}
