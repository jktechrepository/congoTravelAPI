using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Models.DTOs.Metrics;
using CongoTravel.Services;
using CongoTravel.Attributes;

namespace CongoTravel.Controllers
{
    /// <summary>
    /// Controller pour les métriques système et application
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MetricsController : ControllerBase
    {
        private readonly MetricsService _metricsService;
        private readonly ILogger<MetricsController> _logger;

        public MetricsController(MetricsService metricsService, ILogger<MetricsController> logger)
        {
            _metricsService = metricsService;
            _logger = logger;
        }

        /// <summary>
        /// Obtient toutes les métriques système et application
        /// </summary>
        [HttpGet]
        [Permission("Metrics.ReadAll")]
        public async Task<ActionResult<MetricsResponseDto>> GetMetrics()
        {
            try
            {
                _logger.LogInformation("Demande des métriques système");
                
                var metrics = await _metricsService.GetAllMetricsAsync();
                
                _logger.LogInformation("Métriques récupérées - Statut: {Status}, CPU: {CPU}%, Mémoire: {Memory}%", 
                    metrics.HealthStatus, metrics.System.CpuUsagePercent, metrics.System.MemoryUsagePercent);
                
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des métriques");
                return StatusCode(500, new { message = "Erreur lors de la récupération des métriques" });
            }
        }

        /// <summary>
        /// Obtient uniquement les métriques système
        /// </summary>
        [HttpGet("system")]
        [Permission("Metrics.ReadAll")]
        public async Task<ActionResult<SystemMetricsDto>> GetSystemMetrics()
        {
            try
            {
                var allMetrics = await _metricsService.GetAllMetricsAsync();
                return Ok(allMetrics.System);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des métriques système");
                return StatusCode(500, new { message = "Erreur lors de la récupération des métriques système" });
            }
        }

        /// <summary>
        /// Obtient uniquement les métriques application
        /// </summary>
        [HttpGet("application")]
        [Permission("Metrics.ReadAll")]
        public async Task<ActionResult<ApplicationMetricsDto>> GetApplicationMetrics()
        {
            try
            {
                var allMetrics = await _metricsService.GetAllMetricsAsync();
                return Ok(allMetrics.Application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des métriques application");
                return StatusCode(500, new { message = "Erreur lors de la récupération des métriques application" });
            }
        }

        /// <summary>
        /// Obtient uniquement les métriques base de données
        /// </summary>
        [HttpGet("database")]
        [Permission("Metrics.ReadAll")]
        public async Task<ActionResult<DatabaseMetricsDto>> GetDatabaseMetrics()
        {
            try
            {
                var allMetrics = await _metricsService.GetAllMetricsAsync();
                return Ok(allMetrics.Database);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des métriques base de données");
                return StatusCode(500, new { message = "Erreur lors de la récupération des métriques base de données" });
            }
        }

        /// <summary>
        /// Obtient uniquement les métriques business
        /// </summary>
        [HttpGet("business")]
        [Permission("Metrics.ReadAll")]
        public async Task<ActionResult<BusinessMetricsDto>> GetBusinessMetrics()
        {
            try
            {
                var allMetrics = await _metricsService.GetAllMetricsAsync();
                return Ok(allMetrics.Business);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des métriques business");
                return StatusCode(500, new { message = "Erreur lors de la récupération des métriques business" });
            }
        }

        /// <summary>
        /// Endpoint de santé simple (pour les health checks)
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetHealth()
        {
            try
            {
                var metrics = await _metricsService.GetAllMetricsAsync();
                
                var healthResponse = new
                {
                    Status = metrics.HealthStatus,
                    Timestamp = DateTime.Now,
                    Uptime = TimeSpan.FromHours(metrics.System.UptimeHours).ToString(@"hh\:mm\:ss"),
                    Version = "2.0.0",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                };

                return metrics.HealthStatus == "Healthy" ? Ok(healthResponse) : 
                       StatusCode(503, healthResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du health check");
                return StatusCode(503, new 
                { 
                    Status = "Unhealthy", 
                    Timestamp = DateTime.Now,
                    Error = "Health check failed"
                });
            }
        }

        /// <summary>
        /// Statut détaillé avec seuils d'alerte
        /// </summary>
        [HttpGet("status")]
        [Permission("Metrics.ReadAll")]
        public async Task<ActionResult<object>> GetDetailedStatus()
        {
            try
            {
                var metrics = await _metricsService.GetAllMetricsAsync();
                
                var status = new
                {
                    Overall = metrics.HealthStatus,
                    Timestamp = metrics.System.Timestamp,
                    Alerts = GetAlerts(metrics),
                    System = new
                    {
                        Status = GetSystemStatus(metrics.System),
                        Cpu = $"{metrics.System.CpuUsagePercent:F1}%",
                        Memory = $"{metrics.System.MemoryUsagePercent:F1}%",
                        Disk = $"{metrics.System.DiskUsagePercent:F1}%",
                        Uptime = $"{TimeSpan.FromHours(metrics.System.UptimeHours).ToString(@"dd\:hh\:mm")}"
                    },
                    Application = new
                    {
                        Status = GetApplicationStatus(metrics.Application),
                        RequestsPerSecond = $"{metrics.Application.RequestsPerSecond:F1}",
                        AverageResponseTime = $"{metrics.Application.AverageResponseTimeMs:F0}ms",
                        ErrorsLastMinute = metrics.Application.ErrorsLastMinute,
                        ActiveUsers = metrics.Application.ActiveUsers
                    },
                    Database = new
                    {
                        Status = GetDatabaseStatus(metrics.Database),
                        Connections = metrics.Database.ActiveConnections,
                        AverageQueryTime = $"{metrics.Database.AverageQueryTimeMs:F0}ms",
                        Size = $"{metrics.Database.DatabaseSizeMB:F1}MB"
                    }
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du statut détaillé");
                return StatusCode(500, new { message = "Erreur lors du statut détaillé" });
            }
        }

        /// <summary>
        /// Génère les alertes basées sur les métriques
        /// </summary>
        private List<string> GetAlerts(MetricsResponseDto metrics)
        {
            var alerts = new List<string>();

            if (metrics.System.CpuUsagePercent > 80)
                alerts.Add($"CPU élevé: {metrics.System.CpuUsagePercent:F1}%");
            
            if (metrics.System.MemoryUsagePercent > 85)
                alerts.Add($"Mémoire élevée: {metrics.System.MemoryUsagePercent:F1}%");
            
            if (metrics.System.DiskUsagePercent > 90)
                alerts.Add($"Disque presque plein: {metrics.System.DiskUsagePercent:F1}%");
            
            if (metrics.Application.ErrorsLastMinute > 5)
                alerts.Add($"Erreurs fréquentes: {metrics.Application.ErrorsLastMinute}/min");
            
            if (metrics.Database.AverageQueryTimeMs > 500)
                alerts.Add($"Base de données lente: {metrics.Database.AverageQueryTimeMs:F0}ms");

            return alerts;
        }

        /// <summary>
        /// Détermine le statut système
        /// </summary>
        private string GetSystemStatus(SystemMetricsDto system)
        {
            if (system.CpuUsagePercent > 90 || system.MemoryUsagePercent > 95 || system.DiskUsagePercent > 95)
                return "Critical";
            if (system.CpuUsagePercent > 70 || system.MemoryUsagePercent > 80 || system.DiskUsagePercent > 80)
                return "Warning";
            return "Healthy";
        }

        /// <summary>
        /// Détermine le statut application
        /// </summary>
        private string GetApplicationStatus(ApplicationMetricsDto app)
        {
            if (app.ErrorsLastMinute > 10 || app.AverageResponseTimeMs > 1000)
                return "Critical";
            if (app.ErrorsLastMinute > 3 || app.AverageResponseTimeMs > 500)
                return "Warning";
            return "Healthy";
        }

        /// <summary>
        /// Détermine le statut base de données
        /// </summary>
        private string GetDatabaseStatus(DatabaseMetricsDto db)
        {
            if (db.AverageQueryTimeMs > 1000 || db.ActiveConnections > 100)
                return "Critical";
            if (db.AverageQueryTimeMs > 500 || db.ActiveConnections > 50)
                return "Warning";
            return "Healthy";
        }
    }
}
