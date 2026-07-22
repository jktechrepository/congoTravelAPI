using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Service d'audit pour tracer toutes les modifications dans le système
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Enregistre la création d'une entité
        /// </summary>
        Task LogCreateAsync<T>(
            T entity, 
            int userId, 
            string userName,
            string? userRole = null,
            int? idSociete = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? commentaire = null) where T : class;

        /// <summary>
        /// Enregistre la modification d'une entité
        /// </summary>
        Task LogUpdateAsync<T>(
            T oldEntity, 
            T newEntity, 
            int userId, 
            string userName,
            string? userRole = null,
            int? idSociete = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? commentaire = null) where T : class;

        /// <summary>
        /// Enregistre la suppression d'une entité
        /// </summary>
        Task LogDeleteAsync<T>(
            T entity, 
            int userId, 
            string userName,
            string? userRole = null,
            int? idSociete = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? commentaire = null) where T : class;

        /// <summary>
        /// Récupère l'historique complet d'un enregistrement
        /// </summary>
        Task<List<AuditLog>> GetEntityHistoryAsync(string tableName, int recordId);

        /// <summary>
        /// Récupère toutes les actions d'un utilisateur
        /// </summary>
        Task<List<AuditLog>> GetUserActionsAsync(
            int userId, 
            DateTime? from = null, 
            DateTime? to = null,
            int page = 1,
            int pageSize = 50);

        /// <summary>
        /// Récupère les modifications récentes (toutes tables)
        /// </summary>
        Task<List<AuditLog>> GetRecentActivitiesAsync(
            int limit = 50,
            string? tableName = null,
            string? action = null);

        /// <summary>
        /// Récupère toutes les actions dans une école
        /// </summary>
        Task<List<AuditLog>> GetSchoolActivitiesAsync(
            int idSociete,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 50);

        /// <summary>
        /// Recherche avancée dans les audits
        /// </summary>
        Task<List<AuditLog>> SearchAsync(
            string? tableName = null,
            int? recordId = null,
            int? userId = null,
            string? action = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 50);

        /// <summary>
        /// Obtient les statistiques d'audit
        /// </summary>
        Task<AuditStatistics> GetStatisticsAsync(
            DateTime? from = null,
            DateTime? to = null,
            int? idSociete = null);

        /// <summary>
        /// Détecte les modifications suspectes (>N modifications en X minutes)
        /// </summary>
        Task<List<AuditLog>> DetectSuspiciousActivitiesAsync(
            int threshold = 10,
            int windowMinutes = 5);
    }

    /// <summary>
    /// Statistiques d'audit
    /// </summary>
    public class AuditStatistics
    {
        public int TotalActions { get; set; }
        public int Creates { get; set; }
        public int Updates { get; set; }
        public int Deletes { get; set; }
        public Dictionary<string, int> ActionsByTable { get; set; } = new();
        public Dictionary<int, int> ActionsByUser { get; set; } = new();
        public DateTime? FirstAction { get; set; }
        public DateTime? LastAction { get; set; }
    }
}

