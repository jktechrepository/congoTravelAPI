using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service d'audit pour tracer toutes les modifications dans le système
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<AuditService> _logger;

        // Liste des champs à EXCLURE de l'audit (sensibles)
        private readonly HashSet<string> _excludedFields = new()
        {
            "MotDePasseHash",
            "SerialNumber",
            "DateCreation",
            "DateModification"
        };

        public AuditService(CongoTravelDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Enregistre la création d'une entité
        /// </summary>
        public async Task LogCreateAsync<T>(
            T entity,
            int userId,
            string userName,
            string? userRole = null,
            int? idSociete = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? commentaire = null) where T : class
        {
            try
            {
                var tableName = typeof(T).Name;
                var recordId = GetEntityId(entity);

                var auditLog = new AuditLog
                {
                    TableName = tableName,
                    RecordId = recordId,
                    Action = "CREATE",
                    UserId = userId,
                    UserName = userName,
                    UserRole = userRole,
                    IdSociete = idSociete,
                    DateAction = DateTime.Now,
                    OldValues = null, // Pas de valeurs anciennes pour CREATE
                    NewValues = SerializeEntity(entity),
                    ChangedFields = null, // Tous les champs sont nouveaux
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Commentaire = commentaire,
                    HttpMethod = "POST",
                    Success = true
                };

                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Audit CREATE enregistré: {tableName}#{recordId} par {userName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'enregistrement de l'audit CREATE pour {typeof(T).Name}");
                // Ne pas propager l'erreur - l'audit ne doit pas bloquer l'opération principale
            }
        }

        /// <summary>
        /// Enregistre la modification d'une entité
        /// </summary>
        public async Task LogUpdateAsync<T>(
            T oldEntity,
            T newEntity,
            int userId,
            string userName,
            string? userRole = null,
            int? idSociete = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? commentaire = null) where T : class
        {
            try
            {
                var tableName = typeof(T).Name;
                var recordId = GetEntityId(newEntity);

                // Comparer les entités pour détecter les changements
                var comparison = CompareEntities(oldEntity, newEntity);

                // Si aucun changement, ne pas créer d'audit
                if (comparison.ChangedFields.Count == 0)
                {
                    _logger.LogDebug($"Aucun changement détecté pour {tableName}#{recordId}");
                    return;
                }

                var auditLog = new AuditLog
                {
                    TableName = tableName,
                    RecordId = recordId,
                    Action = "UPDATE",
                    UserId = userId,
                    UserName = userName,
                    UserRole = userRole,
                    IdSociete = idSociete,
                    DateAction = DateTime.Now,
                    OldValues = JsonSerializer.Serialize(comparison.OldValues),
                    NewValues = JsonSerializer.Serialize(comparison.NewValues),
                    ChangedFields = string.Join(",", comparison.ChangedFields),
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Commentaire = commentaire,
                    HttpMethod = "PUT",
                    Success = true
                };

                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Audit UPDATE enregistré: {tableName}#{recordId} - Champs: {auditLog.ChangedFields}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'enregistrement de l'audit UPDATE pour {typeof(T).Name}");
            }
        }

        /// <summary>
        /// Enregistre la suppression d'une entité
        /// </summary>
        public async Task LogDeleteAsync<T>(
            T entity,
            int userId,
            string userName,
            string? userRole = null,
            int? idSociete = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? commentaire = null) where T : class
        {
            try
            {
                var tableName = typeof(T).Name;
                var recordId = GetEntityId(entity);

                var auditLog = new AuditLog
                {
                    TableName = tableName,
                    RecordId = recordId,
                    Action = "DELETE",
                    UserId = userId,
                    UserName = userName,
                    UserRole = userRole,
                    IdSociete = idSociete,
                    DateAction = DateTime.Now,
                    OldValues = SerializeEntity(entity),
                    NewValues = null, // Pas de nouvelles valeurs pour DELETE
                    ChangedFields = null,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Commentaire = commentaire,
                    HttpMethod = "DELETE",
                    Success = true
                };

                await _context.AuditLogs.AddAsync(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Audit DELETE enregistré: {tableName}#{recordId} par {userName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'enregistrement de l'audit DELETE pour {typeof(T).Name}");
            }
        }

        /// <summary>
        /// Récupère l'historique complet d'un enregistrement
        /// </summary>
        public async Task<List<AuditLog>> GetEntityHistoryAsync(string tableName, int recordId)
        {
            return await _context.AuditLogs
                .Where(a => a.TableName == tableName && a.RecordId == recordId)
                .OrderByDescending(a => a.DateAction)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère toutes les actions d'un utilisateur
        /// </summary>
        public async Task<List<AuditLog>> GetUserActionsAsync(
            int userId,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 50)
        {
            var query = _context.AuditLogs.Where(a => a.UserId == userId);

            if (from.HasValue)
                query = query.Where(a => a.DateAction >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.DateAction <= to.Value);

            return await query
                .OrderByDescending(a => a.DateAction)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les modifications récentes
        /// </summary>
        public async Task<List<AuditLog>> GetRecentActivitiesAsync(
            int limit = 50,
            string? tableName = null,
            string? action = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(tableName))
                query = query.Where(a => a.TableName == tableName);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action == action);

            return await query
                .OrderByDescending(a => a.DateAction)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère toutes les actions dans une école
        /// </summary>
        public async Task<List<AuditLog>> GetSchoolActivitiesAsync(
            int idSociete,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 50)
        {
            var query = _context.AuditLogs.Where(a => a.IdSociete == idSociete);

            if (from.HasValue)
                query = query.Where(a => a.DateAction >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.DateAction <= to.Value);

            return await query
                .OrderByDescending(a => a.DateAction)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Recherche avancée dans les audits
        /// </summary>
        public async Task<List<AuditLog>> SearchAsync(
            string? tableName = null,
            int? recordId = null,
            int? userId = null,
            string? action = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 50)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(tableName))
                query = query.Where(a => a.TableName == tableName);

            if (recordId.HasValue)
                query = query.Where(a => a.RecordId == recordId.Value);

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action == action);

            if (from.HasValue)
                query = query.Where(a => a.DateAction >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.DateAction <= to.Value);

            return await query
                .OrderByDescending(a => a.DateAction)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Obtient les statistiques d'audit
        /// </summary>
        public async Task<AuditStatistics> GetStatisticsAsync(
            DateTime? from = null,
            DateTime? to = null,
            int? idSociete = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (from.HasValue)
                query = query.Where(a => a.DateAction >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.DateAction <= to.Value);

            if (idSociete.HasValue)
                query = query.Where(a => a.IdSociete == idSociete.Value);

            var stats = new AuditStatistics
            {
                TotalActions = await query.CountAsync(),
                Creates = await query.CountAsync(a => a.Action == "CREATE"),
                Updates = await query.CountAsync(a => a.Action == "UPDATE"),
                Deletes = await query.CountAsync(a => a.Action == "DELETE"),
                ActionsByTable = await query
                    .GroupBy(a => a.TableName)
                    .Select(g => new { Table = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Table, x => x.Count),
                ActionsByUser = await query
                    .GroupBy(a => a.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count),
                FirstAction = await query.MinAsync(a => (DateTime?)a.DateAction),
                LastAction = await query.MaxAsync(a => (DateTime?)a.DateAction)
            };

            return stats;
        }

        /// <summary>
        /// Détecte les modifications suspectes
        /// </summary>
        public async Task<List<AuditLog>> DetectSuspiciousActivitiesAsync(
            int threshold = 10,
            int windowMinutes = 5)
        {
            var sinceDate = DateTime.Now.AddMinutes(-windowMinutes);

            var suspiciousUsers = await _context.AuditLogs
                .Where(a => a.DateAction >= sinceDate)
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .Where(x => x.Count >= threshold)
                .Select(x => x.UserId)
                .ToListAsync();

            if (suspiciousUsers.Count == 0)
                return new List<AuditLog>();

            return await _context.AuditLogs
                .Where(a => suspiciousUsers.Contains(a.UserId) && a.DateAction >= sinceDate)
                .OrderByDescending(a => a.DateAction)
                .ToListAsync();
        }

        // ============================================
        // MÉTHODES PRIVÉES - HELPERS
        // ============================================

        /// <summary>
        /// Extrait l'ID d'une entité par réflexion
        /// </summary>
        private int GetEntityId<T>(T entity) where T : class
        {
            var idProperty = typeof(T).GetProperties()
                .FirstOrDefault(p => p.Name.StartsWith("Id") && p.PropertyType == typeof(int));

            if (idProperty == null)
                throw new InvalidOperationException($"Impossible de trouver la propriété ID pour {typeof(T).Name}");

            var value = idProperty.GetValue(entity);
            return value != null ? (int)value : 0;
        }

        /// <summary>
        /// Sérialise une entité en JSON (sans les champs exclus)
        /// </summary>
        private string SerializeEntity<T>(T entity) where T : class
        {
            var properties = typeof(T).GetProperties()
                .Where(p => !_excludedFields.Contains(p.Name))
                .Where(p => IsSimpleType(p.PropertyType))
                .ToDictionary(
                    p => p.Name,
                    p => p.GetValue(entity)
                );

            return JsonSerializer.Serialize(properties);
        }

        /// <summary>
        /// Compare deux entités et retourne les changements
        /// </summary>
        private EntityComparison CompareEntities<T>(T oldEntity, T newEntity) where T : class
        {
            var comparison = new EntityComparison();

            var properties = typeof(T).GetProperties()
                .Where(p => !_excludedFields.Contains(p.Name))
                .Where(p => IsSimpleType(p.PropertyType));

            foreach (var property in properties)
            {
                var oldValue = property.GetValue(oldEntity);
                var newValue = property.GetValue(newEntity);

                // Comparer les valeurs
                if (!AreEqual(oldValue, newValue))
                {
                    comparison.ChangedFields.Add(property.Name);
                    comparison.OldValues[property.Name] = oldValue;
                    comparison.NewValues[property.Name] = newValue;
                }
            }

            return comparison;
        }

        /// <summary>
        /// Vérifie si deux valeurs sont égales
        /// </summary>
        private bool AreEqual(object? oldValue, object? newValue)
        {
            if (oldValue == null && newValue == null) return true;
            if (oldValue == null || newValue == null) return false;

            // Pour les types numériques, convertir en string pour éviter les erreurs de précision
            if (IsNumericType(oldValue.GetType()))
            {
                return oldValue.ToString() == newValue.ToString();
            }

            return oldValue.Equals(newValue);
        }

        /// <summary>
        /// Vérifie si un type est simple (pas de navigation properties)
        /// </summary>
        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateOnly)
                || type == typeof(TimeOnly)
                || type == typeof(TimeSpan)
                || type == typeof(Guid)
                || type == typeof(int?)
                || type == typeof(long?)
                || type == typeof(double?)
                || type == typeof(decimal?)
                || type == typeof(DateTime?)
                || type == typeof(bool?)
                || type == typeof(Guid?);
        }

        /// <summary>
        /// Vérifie si un type est numérique
        /// </summary>
        private bool IsNumericType(Type type)
        {
            return type == typeof(int)
                || type == typeof(long)
                || type == typeof(double)
                || type == typeof(decimal)
                || type == typeof(float)
                || type == typeof(short)
                || type == typeof(byte);
        }
    }

    /// <summary>
    /// Résultat de la comparaison de deux entités
    /// </summary>
    internal class EntityComparison
    {
        public List<string> ChangedFields { get; set; } = new();
        public Dictionary<string, object?> OldValues { get; set; } = new();
        public Dictionary<string, object?> NewValues { get; set; } = new();
    }
}

