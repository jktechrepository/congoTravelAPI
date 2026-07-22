namespace CongoTravel.Models.DTOs.Metrics
{
    /// <summary>
    /// DTO pour les métriques système
    /// </summary>
    public class SystemMetricsDto
    {
        /// <summary>
        /// Timestamp de la mesure
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Mémoire utilisée (MB)
        /// </summary>
        public double MemoryUsedMB { get; set; }

        /// <summary>
        /// Mémoire totale (MB)
        /// </summary>
        public double MemoryTotalMB { get; set; }

        /// <summary>
        /// Pourcentage mémoire utilisée
        /// </summary>
        public double MemoryUsagePercent { get; set; }

        /// <summary>
        /// CPU utilisé (%)
        /// </summary>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        /// Espace disque utilisé (GB)
        /// </summary>
        public double DiskUsedGB { get; set; }

        /// <summary>
        /// Espace disque total (GB)
        /// </summary>
        public double DiskTotalGB { get; set; }

        /// <summary>
        /// Pourcentage disque utilisé
        /// </summary>
        public double DiskUsagePercent { get; set; }

        /// <summary>
        /// Temps d'activité du serveur (heures)
        /// </summary>
        public double UptimeHours { get; set; }
    }

    /// <summary>
    /// DTO pour les métriques d'application
    /// </summary>
    public class ApplicationMetricsDto
    {
        /// <summary>
        /// Timestamp de la mesure
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Requêtes par seconde
        /// </summary>
        public double RequestsPerSecond { get; set; }

        /// <summary>
        /// Temps de réponse moyen (ms)
        /// </summary>
        public double AverageResponseTimeMs { get; set; }

        /// <summary>
        /// Nombre d'erreurs dans la dernière minute
        /// </summary>
        public int ErrorsLastMinute { get; set; }

        /// <summary>
        /// Nombre total d'utilisateurs connectés
        /// </summary>
        public int ActiveUsers { get; set; }

        /// <summary>
        /// Nombre total de requêtes depuis démarrage
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// Nombre d'exports réalisés aujourd'hui
        /// </summary>
        public int ExportsToday { get; set; }
    }

    /// <summary>
    /// DTO pour les métriques de base de données
    /// </summary>
    public class DatabaseMetricsDto
    {
        /// <summary>
        /// Timestamp de la mesure
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Nombre de connexions actives
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// Temps de réponse moyen (ms)
        /// </summary>
        public double AverageQueryTimeMs { get; set; }

        /// <summary>
        /// Nombre de requêtes par seconde
        /// </summary>
        public double QueriesPerSecond { get; set; }

        /// <summary>
        /// Taille de la base de données (MB)
        /// </summary>
        public double DatabaseSizeMB { get; set; }

        /// <summary>
        /// Nombre de tables
        /// </summary>
        public int TableCount { get; set; }

        /// <summary>
        /// Nombre total d'enregistrements
        /// </summary>
        public long TotalRecords { get; set; }
    }

    /// <summary>
    /// DTO pour les métriques business
    /// </summary>
    public class BusinessMetricsDto
    {
        /// <summary>
        /// Timestamp de la mesure
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Nombre total de clients
        /// </summary>
        public int TotalClients { get; set; }

        /// <summary>
        /// Nombre de clients actifs
        /// </summary>
        public int ActiveClients { get; set; }

        /// <summary>
        /// Nombre de sociétés
        /// </summary>
        public int TotalSocietes { get; set; }

        /// <summary>
        /// Nombre d'exports ce mois-ci
        /// </summary>
        public int ExportsThisMonth { get; set; }

        /// <summary>
        /// Nombre d'utilisateurs connectés aujourd'hui
        /// </summary>
        public int ActiveUsersToday { get; set; }

        /// <summary>
        /// Croissance des clients ce mois (%)
        /// </summary>
        public double ClientGrowthPercent { get; set; }
    }

    /// <summary>
    /// DTO principal contenant toutes les métriques
    /// </summary>
    public class MetricsResponseDto
    {
        /// <summary>
        /// Métriques système
        /// </summary>
        public SystemMetricsDto System { get; set; } = new();

        /// <summary>
        /// Métriques application
        /// </summary>
        public ApplicationMetricsDto Application { get; set; } = new();

        /// <summary>
        /// Métriques base de données
        /// </summary>
        public DatabaseMetricsDto Database { get; set; } = new();

        /// <summary>
        /// Métriques business
        /// </summary>
        public BusinessMetricsDto Business { get; set; } = new();

        /// <summary>
        /// Statut général du système
        /// </summary>
        public string HealthStatus { get; set; } = "Healthy";
    }
}
