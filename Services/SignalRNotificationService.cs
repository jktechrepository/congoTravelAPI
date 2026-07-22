using Microsoft.AspNetCore.SignalR;
using CongoTravel.Hubs;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    /// <summary>Implémentation SignalR basée sur <see cref="NotificationHub"/>.</summary>
    public class SignalRNotificationService : ISignalRNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<SignalRNotificationService> _logger;

        public SignalRNotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<SignalRNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public Task SendNotificationToUserAsync(int userId, Notification notification) =>
            _hubContext.Clients.Group(UserGroup(userId))
                .SendAsync("ReceiveNotification", MapNotification(notification));

        public async Task SendNotificationToUsersAsync(List<int> userIds, Notification notification)
        {
            foreach (var userId in userIds.Distinct())
                await SendNotificationToUserAsync(userId, notification);
        }

        public Task SendNotificationToSocieteAsync(int societeId, Notification notification) =>
            _hubContext.Clients.Group(SocieteGroup(societeId))
                .SendAsync("ReceiveNotification", MapNotification(notification));

        public Task SendNotificationToClasseAsync(int classeId, Notification notification) =>
            _hubContext.Clients.Group(ClasseGroup(classeId))
                .SendAsync("ReceiveNotification", MapNotification(notification));

        public Task SendNotificationToAllAsync(Notification notification) =>
            _hubContext.Clients.Group("all_users")
                .SendAsync("ReceiveNotification", MapNotification(notification));

        public Task SendCustomNotificationAsync(int userId, string title, string message, string type = "info") =>
            _hubContext.Clients.Group(UserGroup(userId))
                .SendAsync("ReceiveNotification", new { title, message, type, timestampUtc = DateTime.UtcNow });

        public Task NotifyStatusChangeAsync(int userId, string entityType, int entityId, string newStatus) =>
            _hubContext.Clients.Group(UserGroup(userId))
                .SendAsync("StatusChanged", new { entityType, entityId, newStatus, timestampUtc = DateTime.UtcNow });

        public Task NotifyNewMessageAsync(int recipientId, int senderId, string senderName, string messageContent) =>
            _hubContext.Clients.Group(UserGroup(recipientId))
                .SendAsync("NewMessage", new { senderId, senderName, messageContent, timestampUtc = DateTime.UtcNow });

        public Task NotifyNewGradeAsync(int studentId, string courseName, decimal? grade) =>
            _hubContext.Clients.Group(UserGroup(studentId))
                .SendAsync("NewGrade", new { courseName, grade, timestampUtc = DateTime.UtcNow });

        public Task NotifyNewPaiementAsync(int societeId, object paiementData) =>
            _hubContext.Clients.Group(SocieteGroup(societeId))
                .SendAsync("NewPaiement", paiementData);

        public Task NotifyDashboardStatusChangeAsync(int societeId, string entityType, int entityId, string newStatus) =>
            _hubContext.Clients.Group(SocieteGroup(societeId))
                .SendAsync("DashboardStatusChanged", new { entityType, entityId, newStatus, timestampUtc = DateTime.UtcNow });

        public Task NotifySuperAdminDashboardUpdatedAsync(object dashboardData) =>
            _hubContext.Clients.Group("super_admin")
                .SendAsync("SuperAdminDashboardUpdated", dashboardData);

        public Task NotifySuperAdminAlerteCritiqueAsync(object alerteData) =>
            _hubContext.Clients.Group("super_admin")
                .SendAsync("SuperAdminAlerteCritique", alerteData);

        public Task NotifySuperAdminStatistiquesUpdatedAsync(object statistiquesData) =>
            _hubContext.Clients.Group("super_admin")
                .SendAsync("SuperAdminStatistiquesUpdated", statistiquesData);

        private static string UserGroup(int userId) => $"user_{userId}";
        private static string SocieteGroup(int societeId) => $"societe_{societeId}";
        private static string ClasseGroup(int classeId) => $"classe_{classeId}";

        private static object MapNotification(Notification n) => new
        {
            n.IdNotification,
            n.Titre,
            n.Contenu,
            n.TypeNotification,
            n.LienAction,
            timestampUtc = DateTime.UtcNow
        };
    }
}
