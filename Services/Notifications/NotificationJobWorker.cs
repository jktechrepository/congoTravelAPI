using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services.Notifications
{
    public class NotificationJobWorker : BackgroundService
    {
        private readonly INotificationJobQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationJobWorker> _logger;

        public NotificationJobWorker(
            INotificationJobQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationJobWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔄 NotificationJobWorker démarré");

            while (!stoppingToken.IsCancellationRequested)
            {
                NotificationDispatchResult dispatchResult;
                try
                {
                    dispatchResult = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors de la récupération d'un job de notification");
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();
                    await sender.SendAsync(dispatchResult, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors du traitement d'un job de notification");
                }
            }

            _logger.LogInformation("⏹️ NotificationJobWorker arrêté");
        }
    }
}

