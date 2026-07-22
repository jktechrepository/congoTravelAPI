using CongoTravel.Models.DTOs.Metrics;
using CongoTravel.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour collecter les métriques système et application
    /// </summary>
    public class MetricsService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<MetricsService> _logger;
        private readonly Process _process;
        private readonly DateTime _startTime;

        // Compteurs pour les métriques d'application
        private static long _totalRequests = 0;
        private static readonly List<DateTime> _errorTimestamps = new();
        private static readonly List<DateTime> _requestTimestamps = new();
        private static int _exportsToday = 0;
        private static readonly Dictionary<string, long> _rateLimitBlocksByScope = new(StringComparer.OrdinalIgnoreCase);

        public MetricsService(CongoTravelDbContext context, ILogger<MetricsService> logger)
        {
            _context = context;
            _logger = logger;
            _process = Process.GetCurrentProcess();
            _startTime = DateTime.Now;
        }

        /// <summary>
        /// Collecte toutes les métriques
        /// </summary>
        public async Task<MetricsResponseDto> GetAllMetricsAsync()
        {
            try
            {
                var metrics = new MetricsResponseDto
                {
                    System = await GetSystemMetricsAsync(),
                    Application = GetApplicationMetrics(),
                    Database = await GetDatabaseMetricsAsync(),
                    Business = await GetBusinessMetricsAsync()
                };

                // Déterminer le statut de santé global
                metrics.HealthStatus = DetermineHealthStatus(metrics);

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la collecte des métriques");
                return new MetricsResponseDto
                {
                    HealthStatus = "Error",
                    System = new SystemMetricsDto { Timestamp = DateTime.Now },
                    Application = new ApplicationMetricsDto { Timestamp = DateTime.Now },
                    Database = new DatabaseMetricsDto { Timestamp = DateTime.Now },
                    Business = new BusinessMetricsDto { Timestamp = DateTime.Now }
                };
            }
        }

        /// <summary>
        /// Métriques système
        /// </summary>
        private async Task<SystemMetricsDto> GetSystemMetricsAsync()
        {
            var memoryMetrics = GetMemoryMetrics();
            var cpuMetrics = GetCpuMetrics();
            var diskMetrics = GetDiskMetrics();

            return new SystemMetricsDto
            {
                Timestamp = DateTime.Now,
                MemoryUsedMB = memoryMetrics.Used,
                MemoryTotalMB = memoryMetrics.Total,
                MemoryUsagePercent = memoryMetrics.Percent,
                CpuUsagePercent = cpuMetrics,
                DiskUsedGB = diskMetrics.Used,
                DiskTotalGB = diskMetrics.Total,
                DiskUsagePercent = diskMetrics.Percent,
                UptimeHours = (DateTime.Now - _startTime).TotalHours
            };
        }

        /// <summary>
        /// Métriques application
        /// </summary>
        private ApplicationMetricsDto GetApplicationMetrics()
        {
            // Nettoyer les anciennes timestamps (plus d'une minute)
            var oneMinuteAgo = DateTime.Now.AddMinutes(-1);
            _errorTimestamps.RemoveAll(t => t < oneMinuteAgo);
            _requestTimestamps.RemoveAll(t => t < oneMinuteAgo);

            return new ApplicationMetricsDto
            {
                Timestamp = DateTime.Now,
                RequestsPerSecond = _requestTimestamps.Count / 60.0,
                AverageResponseTimeMs = CalculateAverageResponseTime(),
                ErrorsLastMinute = _errorTimestamps.Count,
                ActiveUsers = GetActiveUsersCount(),
                TotalRequests = _totalRequests,
                ExportsToday = _exportsToday
            };
        }

        /// <summary>
        /// Métriques base de données
        /// </summary>
        private async Task<DatabaseMetricsDto> GetDatabaseMetricsAsync()
        {
            try
            {
                // Obtenir la taille de la base de données
                var dbSize = await GetDatabaseSizeAsync();
                
                return new DatabaseMetricsDto
                {
                    Timestamp = DateTime.Now,
                    ActiveConnections = GetActiveConnections(),
                    AverageQueryTimeMs = GetAverageQueryTime(),
                    QueriesPerSecond = GetQueriesPerSecond(),
                    DatabaseSizeMB = dbSize,
                    TableCount = await GetTableCountAsync(),
                    TotalRecords = await GetTotalRecordsAsync()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur lors de la collecte des métriques DB");
                return new DatabaseMetricsDto
                {
                    Timestamp = DateTime.Now,
                    ActiveConnections = 0,
                    AverageQueryTimeMs = 0,
                    QueriesPerSecond = 0,
                    DatabaseSizeMB = 0,
                    TableCount = 0,
                    TotalRecords = 0
                };
            }
        }

        /// <summary>
        /// Métriques business
        /// </summary>
        private async Task<BusinessMetricsDto> GetBusinessMetricsAsync()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            return new BusinessMetricsDto
            {
                Timestamp = DateTime.Now,
                TotalClients = await GetTotalClientsAsync(),
                ActiveClients = await GetActiveClientsAsync(),
                TotalSocietes = await GetTotalSocietesAsync(),
                ExportsThisMonth = await GetExportsThisMonthAsync(),
                ActiveUsersToday = await GetActiveUsersTodayAsync(),
                ClientGrowthPercent = await GetClientGrowthPercentAsync()
            };
        }

        /// <summary>
        /// Métriques mémoire
        /// </summary>
        private (double Used, double Total, double Percent) GetMemoryMetrics()
        {
            var gc = GC.GetTotalMemory(false);
            var workingSet = _process.WorkingSet64;
            var totalMemory = workingSet;

            return (
                Used: Math.Round(workingSet / 1024.0 / 1024.0, 2), // MB
                Total: Math.Round(totalMemory / 1024.0 / 1024.0, 2), // MB
                Percent: Math.Round((workingSet / (double)totalMemory) * 100, 2)
            );
        }

        /// <summary>
        /// Métriques CPU
        /// </summary>
        private double GetCpuMetrics()
        {
            try
            {
                _process.Refresh();
                return Math.Round(_process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / 100.0, 2);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Métriques disque
        /// </summary>
        private (double Used, double Total, double Percent) GetDiskMetrics()
        {
            try
            {
                var drive = new DriveInfo(Path.GetDirectoryName(AppContext.BaseDirectory) ?? "/");
                var total = drive.TotalSize;
                var free = drive.AvailableFreeSpace;
                var used = total - free;

                return (
                    Used: Math.Round(used / 1024.0 / 1024.0 / 1024.0, 2), // GB
                    Total: Math.Round(total / 1024.0 / 1024.0 / 1024.0, 2), // GB
                    Percent: Math.Round((used / (double)total) * 100, 2)
                );
            }
            catch
            {
                return (0, 0, 0);
            }
        }

        /// <summary>
        /// Taille de la base de données
        /// </summary>
        private async Task<double> GetDatabaseSizeAsync()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT ROUND(SUM(data_length + index_length) / 1024 / 1024, 2) AS size_mb
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE()";
                
                var result = await command.ExecuteScalarAsync();
                await connection.CloseAsync();
                
                return Convert.ToDouble(result ?? 0);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Nombre de connexions actives
        /// </summary>
        private int GetActiveConnections()
        {
            try
            {
                return _context.Database.GetDbConnection()?.State == System.Data.ConnectionState.Open ? 1 : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Temps moyen de réponse
        /// </summary>
        private double CalculateAverageResponseTime()
        {
            // Simulation - à implémenter avec un vrai middleware de timing
            return Random.Shared.Next(50, 200);
        }

        /// <summary>
        /// Temps moyen de requête DB
        /// </summary>
        private double GetAverageQueryTime()
        {
            // Simulation - à implémenter avec un vrai compteur
            return Random.Shared.Next(10, 100);
        }

        /// <summary>
        /// Requêtes par seconde
        /// </summary>
        private double GetQueriesPerSecond()
        {
            // Simulation - à implémenter avec un vrai compteur
            return Random.Shared.Next(10, 50);
        }

        /// <summary>
        /// Nombre de tables
        /// </summary>
        private async Task<int> GetTableCountAsync()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE()";
                
                var result = await command.ExecuteScalarAsync();
                await connection.CloseAsync();
                
                return Convert.ToInt32(result ?? 0);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Total des enregistrements
        /// </summary>
        private async Task<long> GetTotalRecordsAsync()
        {
            try
            {
                var tables = new[] { "Clients", "Societes", "Usages", "Factures", "Utilisateurs" };
                long total = 0;

                foreach (var table in tables)
                {
                    try
                    {
                        var connection = _context.Database.GetDbConnection();
                        await connection.OpenAsync();
                        
                        using var command = connection.CreateCommand();
                        command.CommandText = $"SELECT COUNT(*) FROM {table}";
                        
                        var result = await command.ExecuteScalarAsync();
                        total += Convert.ToInt64(result ?? 0);
                        await connection.CloseAsync();
                    }
                    catch { }
                }

                return total;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Nombre d'utilisateurs actifs
        /// </summary>
        private int GetActiveUsersCount()
        {
            // Simulation - à implémenter avec un vrai tracking
            return Random.Shared.Next(5, 20);
        }

        /// <summary>
        /// Total clients
        /// </summary>
        private async Task<int> GetTotalClientsAsync()
        {
            try
            {
                return await _context.Clients.CountAsync(c => c.Statut == true);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Clients actifs
        /// </summary>
        private async Task<int> GetActiveClientsAsync()
        {
            try
            {
                return await _context.Clients.CountAsync(c => c.Statut == true && c.IsActif == true);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Total sociétés
        /// </summary>
        private async Task<int> GetTotalSocietesAsync()
        {
            try
            {
                return await _context.Societes.CountAsync();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Exports ce mois
        /// </summary>
        private async Task<int> GetExportsThisMonthAsync()
        {
            try
            {
                var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                return await _context.AuditLogs
                    .CountAsync(a => a.Action.Contains("EXPORT") && a.DateAction >= thisMonth);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Utilisateurs actifs aujourd'hui
        /// </summary>
        private async Task<int> GetActiveUsersTodayAsync()
        {
            try
            {
                var today = DateTime.Today;
                return await _context.AuditLogs
                    .Where(a => a.DateAction >= today)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Croissance des clients
        /// </summary>
        private async Task<double> GetClientGrowthPercentAsync()
        {
            try
            {
                var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var lastMonth = thisMonth.AddMonths(-1);
                
                var thisMonthCount = await _context.Clients.CountAsync(c => c.Statut == true && c.DateCreation >= thisMonth);
                var lastMonthCount = await _context.Clients.CountAsync(c => c.Statut == true && c.DateCreation >= lastMonth && c.DateCreation < thisMonth);
                
                if (lastMonthCount == 0) return 0;
                
                return Math.Round(((thisMonthCount - lastMonthCount) / (double)lastMonthCount) * 100, 2);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Détermine le statut de santé
        /// </summary>
        private string DetermineHealthStatus(MetricsResponseDto metrics)
        {
            var issues = new List<string>();

            if (metrics.System.MemoryUsagePercent > 90) issues.Add("Mémoire élevée");
            if (metrics.System.CpuUsagePercent > 80) issues.Add("CPU élevé");
            if (metrics.System.DiskUsagePercent > 85) issues.Add("Disque plein");
            if (metrics.Application.ErrorsLastMinute > 10) issues.Add("Erreurs fréquentes");
            if (metrics.Database.AverageQueryTimeMs > 1000) issues.Add("DB lente");

            return issues.Count == 0 ? "Healthy" : 
                   issues.Count <= 2 ? "Warning" : "Critical";
        }

        /// <summary>
        /// Enregistre une requête (à appeler depuis un middleware)
        /// </summary>
        public static void RecordRequest()
        {
            Interlocked.Increment(ref _totalRequests);
            _requestTimestamps.Add(DateTime.Now);
        }

        /// <summary>
        /// Enregistre une erreur (à appeler depuis un middleware)
        /// </summary>
        public static void RecordError()
        {
            _errorTimestamps.Add(DateTime.Now);
        }

        /// <summary>
        /// Enregistre un export
        /// </summary>
        public static void RecordExport()
        {
            if (DateTime.Today.Date == DateTime.Now.Date)
            {
                Interlocked.Increment(ref _exportsToday);
            }
        }

        /// <summary>
        /// Enregistre un blocage rate-limit par scope (email/device/ip).
        /// </summary>
        public static void RecordRateLimitBlock(string scope)
        {
            var key = string.IsNullOrWhiteSpace(scope) ? "unknown" : scope.Trim().ToLowerInvariant();
            lock (_rateLimitBlocksByScope)
            {
                _rateLimitBlocksByScope.TryGetValue(key, out var current);
                _rateLimitBlocksByScope[key] = current + 1;
            }
        }
    }
}
