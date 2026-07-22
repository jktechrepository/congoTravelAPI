using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le service de notifications SignalR
    /// </summary>
    public interface ISignalRNotificationService
    {
        /// <summary>
        /// Envoyer une notification en temps réel à un utilisateur spécifique
        /// </summary>
        Task SendNotificationToUserAsync(int userId, Notification notification);

        /// <summary>
        /// Envoyer une notification en temps réel à plusieurs utilisateurs
        /// </summary>
        Task SendNotificationToUsersAsync(List<int> userIds, Notification notification);

        /// <summary>
        /// Envoyer une notification à tous les utilisateurs d'une école
        /// </summary>
        Task SendNotificationToSocieteAsync(int societeId, Notification notification);

        /// <summary>
        /// Envoyer une notification à tous les utilisateurs d'une classe
        /// </summary>
        Task SendNotificationToClasseAsync(int classeId, Notification notification);

        /// <summary>
        /// Envoyer une notification à tous les utilisateurs connectés
        /// </summary>
        Task SendNotificationToAllAsync(Notification notification);

        /// <summary>
        /// Envoyer une notification personnalisée à un utilisateur
        /// </summary>
        Task SendCustomNotificationAsync(int userId, string title, string message, string type = "info");

        /// <summary>
        /// Notifier un changement de statut (présence, paiement, etc.)
        /// </summary>
        Task NotifyStatusChangeAsync(int userId, string entityType, int entityId, string newStatus);

        /// <summary>
        /// Notifier un nouveau message
        /// </summary>
        Task NotifyNewMessageAsync(int recipientId, int senderId, string senderName, string messageContent);

        /// <summary>
        /// Notifier une nouvelle note publiée
        /// </summary>
        Task NotifyNewGradeAsync(int studentId, string courseName, decimal? grade);

        /// <summary>
        /// Notifier un nouveau paiement
        /// </summary>
        Task NotifyNewPaiementAsync(int societeId, object paiementData);

        /// <summary>
        /// Notifier un changement de statut sur le dashboard
        /// </summary>
        Task NotifyDashboardStatusChangeAsync(int societeId, string entityType, int entityId, string newStatus);

        /// <summary>
        /// Notifier la mise à jour du dashboard Super-Admin
        /// </summary>
        Task NotifySuperAdminDashboardUpdatedAsync(object dashboardData);

        /// <summary>
        /// Notifier une nouvelle alerte critique pour le Super-Admin
        /// </summary>
        Task NotifySuperAdminAlerteCritiqueAsync(object alerteData);

        /// <summary>
        /// Notifier un changement dans les statistiques globales
        /// </summary>
        Task NotifySuperAdminStatistiquesUpdatedAsync(object statistiquesData);
    }
}
