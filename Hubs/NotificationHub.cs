using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CongoTravel.Hubs
{
    /// <summary>
    /// Hub SignalR pour les notifications en temps réel
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Appelé quand un client se connecte
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("sub")?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Ajouter l'utilisateur à son groupe personnel
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                
                // Ajouter l'utilisateur au groupe général
                await Groups.AddToGroupAsync(Context.ConnectionId, "all_users");
                
                _logger.LogInformation($"User {userName} (ID: {userId}) connected to NotificationHub. ConnectionId: {Context.ConnectionId}");
            }
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Appelé quand un client se déconnecte
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("sub")?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation($"User {userName} (ID: {userId}) disconnected from NotificationHub. ConnectionId: {Context.ConnectionId}");
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Permet à un utilisateur de rejoindre un groupe spécifique
        /// </summary>
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"User {Context.User?.Identity?.Name} joined group: {groupName}");
        }

        /// <summary>
        /// Permet à un utilisateur de quitter un groupe spécifique
        /// </summary>
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"User {Context.User?.Identity?.Name} left group: {groupName}");
        }

        /// <summary>
        /// Marquer une notification comme lue
        /// </summary>
        public async Task MarkNotificationAsRead(int notificationId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Ici, vous pourriez appeler un service pour marquer la notification comme lue
                _logger.LogInformation($"User {userId} marked notification {notificationId} as read");
                
                // Notifier le client que la notification a été marquée comme lue
                await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
            }
        }

        /// <summary>
        /// Obtenir le statut de connexion
        /// </summary>
        public async Task GetConnectionStatus()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
            
            await Clients.Caller.SendAsync("ConnectionStatus", new
            {
                IsConnected = true,
                UserId = userId,
                UserName = userName,
                ConnectionId = Context.ConnectionId,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
