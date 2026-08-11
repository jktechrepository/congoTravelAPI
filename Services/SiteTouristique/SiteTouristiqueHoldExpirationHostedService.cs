using CongoTravel.Data;

namespace CongoTravel.Services.SiteTouristique
{
    /// <summary>
    /// Job applicatif d'expiration des holds site touristiques (alternative / complément au MySQL EVENT scheduler).
    /// </summary>
    public class SiteTouristiqueHoldExpirationHostedService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SiteTouristiqueHoldExpirationHostedService> _logger;

        public SiteTouristiqueHoldExpirationHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<SiteTouristiqueHoldExpirationHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "SiteTouristiqueHoldExpirationHostedService démarré (intervalle {IntervalMinutes} min).",
                Interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur inattendue lors du cycle d'expiration holds site touristique.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("SiteTouristiqueHoldExpirationHostedService arrêté.");
        }

        private async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CongoTravelDbContext>();
            var runner = scope.ServiceProvider.GetRequiredService<ISiteTouristiqueHoldExpirationRunner>();
            await runner.ExpireHoldsAsync(context, cancellationToken);
        }
    }
}
