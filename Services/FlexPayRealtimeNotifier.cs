using Microsoft.AspNetCore.SignalR;
using CongoTravel.Hubs;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    /// <summary>
    /// Envoie les événements FlexPay au groupe SignalR <c>user_{userId}</c>.
    /// </summary>
    public class FlexPayRealtimeNotifier : IFlexPayRealtimeNotifier
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<FlexPayRealtimeNotifier> _logger;

        public FlexPayRealtimeNotifier(
            IHubContext<NotificationHub> hubContext,
            ILogger<FlexPayRealtimeNotifier> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyPaymentConfirmedAsync(
            int userId,
            string orderNumber,
            int idReservation,
            int idPaiement,
            CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                orderNumber,
                idReservation,
                idPaiement,
                status = "confirmed",
                timestampUtc = DateTime.UtcNow
            };

            await _hubContext.Clients
                .Group(UserGroup(userId))
                .SendAsync("FlexPayPaymentConfirmed", payload, cancellationToken);

            _logger.LogInformation(
                "SignalR FlexPayPaymentConfirmed — user={UserId}, order={OrderNumber}, reservation={IdReservation}",
                userId, orderNumber, idReservation);
        }

        public async Task NotifyPaymentFailedAsync(
            int userId,
            string orderNumber,
            string message,
            CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                orderNumber,
                message,
                status = "failed",
                timestampUtc = DateTime.UtcNow
            };

            await _hubContext.Clients
                .Group(UserGroup(userId))
                .SendAsync("FlexPayPaymentFailed", payload, cancellationToken);

            _logger.LogInformation(
                "SignalR FlexPayPaymentFailed — user={UserId}, order={OrderNumber}",
                userId, orderNumber);
        }

        public Task NotifyPaymentConfirmedForDomainAsync(
            int userId, string orderNumber, int idReservation, int idPaiement,
            string domain, CancellationToken cancellationToken = default) =>
            _hubContext.Clients.Group(UserGroup(userId)).SendAsync(
                "FlexPayPaymentConfirmed",
                new { orderNumber, idReservation, idPaiement, domain, status = "confirmed", timestampUtc = DateTime.UtcNow },
                cancellationToken);

        public Task NotifyPaymentFailedForDomainAsync(
            int userId, string orderNumber, string message, string domain,
            CancellationToken cancellationToken = default) =>
            _hubContext.Clients.Group(UserGroup(userId)).SendAsync(
                "FlexPayPaymentFailed",
                new { orderNumber, message, domain, status = "failed", timestampUtc = DateTime.UtcNow },
                cancellationToken);

        private static string UserGroup(int userId) => $"user_{userId}";
    }
}
