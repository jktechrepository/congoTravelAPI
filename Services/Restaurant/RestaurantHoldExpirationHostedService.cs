using CongoTravel.Data;

namespace CongoTravel.Services.Restaurant
{
    /// <summary>
    /// Job applicatif d'expiration des holds restaurant (alternative / complément au MySQL EVENT scheduler).
    /// </summary>
    public class RestaurantHoldExpirationHostedService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RestaurantHoldExpirationHostedService> _logger;

        public RestaurantHoldExpirationHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<RestaurantHoldExpirationHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "RestaurantHoldExpirationHostedService démarré (intervalle {IntervalMinutes} min).",
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
                    _logger.LogError(ex, "Erreur inattendue lors du cycle d'expiration holds restaurant.");
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

            _logger.LogInformation("RestaurantHoldExpirationHostedService arrêté.");
        }

        private async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CongoTravelDbContext>();
            var runner = scope.ServiceProvider.GetRequiredService<IRestaurantHoldExpirationRunner>();
            await runner.ExpireHoldsAsync(context, cancellationToken);
        }
    }
}
