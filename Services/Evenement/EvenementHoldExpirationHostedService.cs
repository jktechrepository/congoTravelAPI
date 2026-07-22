using CongoTravel.Data;

namespace CongoTravel.Services.Evenement
{
    /// <summary>
    /// Job applicatif d'expiration des holds événementiels (alternative / complément au MySQL EVENT scheduler).
    /// </summary>
    public class EvenementHoldExpirationHostedService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EvenementHoldExpirationHostedService> _logger;

        public EvenementHoldExpirationHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<EvenementHoldExpirationHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "EvenementHoldExpirationHostedService démarré (intervalle {IntervalMinutes} min).",
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
                    _logger.LogError(ex, "Erreur inattendue lors du cycle d'expiration holds événement.");
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

            _logger.LogInformation("EvenementHoldExpirationHostedService arrêté.");
        }

        private async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CongoTravelDbContext>();
            var runner = scope.ServiceProvider.GetRequiredService<IEvenementHoldExpirationRunner>();
            await runner.ExpireHoldsAsync(context, cancellationToken);
        }
    }
}
