using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using CongoTravel.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;

namespace CongoTravel.Controllers
{
    /// <summary>
    /// Controller pour consulter les logs d'audit
    /// Accessible uniquement aux Admins et Super-Admins
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IAuditService auditService, ILogger<AuditController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère l'historique complet d'un enregistrement spécifique
        /// </summary>
        /// <param name="tableName">Nom de la table (ex: "Paiement", "Note")</param>
        /// <param name="recordId">ID de l'enregistrement</param>
        [HttpGet("history/{tableName}/{recordId}")]
        [Permission("Audit.ReadAll")]
        [ProducesResponseType(typeof(List<AuditLog>), 200)]
        public async Task<ActionResult<List<AuditLog>>> GetEntityHistory(string tableName, int recordId)
        {
            try
            {
                var history = await _auditService.GetEntityHistoryAsync(tableName, recordId);
                
                return Ok(new
                {
                    tableName,
                    recordId,
                    totalChanges = history.Count,
                    history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération de l'historique {tableName}#{recordId}");
                return StatusCode(500, new { message = "Erreur lors de la récupération de l'historique" });
            }
        }

        /// <summary>
        /// Récupère toutes les actions effectuées par un utilisateur
        /// </summary>
        [HttpGet("user/{userId}")]
        [Permission("Audit.ReadAll")]
        [ProducesResponseType(typeof(List<AuditLog>), 200)]
        public async Task<ActionResult<List<AuditLog>>> GetUserActions(
            int userId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var actions = await _auditService.GetUserActionsAsync(userId, from, to, page, pageSize);
                
                return Ok(new
                {
                    userId,
                    from,
                    to,
                    page,
                    pageSize,
                    totalResults = actions.Count,
                    actions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des actions de l'utilisateur {userId}");
                return StatusCode(500, new { message = "Erreur lors de la récupération des actions" });
            }
        }

        /// <summary>
        /// Récupère les modifications récentes (toutes tables ou filtrées)
        /// </summary>
        [HttpGet("recent")]
        [Permission("Audit.ReadAll")]
        [ProducesResponseType(typeof(List<AuditLog>), 200)]
        public async Task<ActionResult<List<AuditLog>>> GetRecentActivities(
            [FromQuery] int limit = 50,
            [FromQuery] string? tableName = null,
            [FromQuery] string? action = null)
        {
            try
            {
                var activities = await _auditService.GetRecentActivitiesAsync(limit, tableName, action);
                
                return Ok(new
                {
                    limit,
                    tableName,
                    action,
                    totalResults = activities.Count,
                    activities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des activités récentes");
                return StatusCode(500, new { message = "Erreur lors de la récupération des activités" });
            }
        }

        /// <summary>
        /// Récupère toutes les actions dans une école
        /// </summary>
        [HttpGet("school/{idSociete}")]
        [Permission("Audit.ReadAll")]
        [ProducesResponseType(typeof(List<AuditLog>), 200)]
        public async Task<ActionResult<List<AuditLog>>> GetSchoolActivities(
            int idSociete,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // Vérifier que l'admin a accès à cette école
                var currentUserSchoolId = this.GetCurrentUserSchoolId();
                var currentUserRole = this.GetCurrentUserRole();

                if (currentUserRole != "Super-Admin" && currentUserSchoolId != idSociete)
                {
                    return Forbid();
                }

                var activities = await _auditService.GetSchoolActivitiesAsync(idSociete, from, to, page, pageSize);
                
                return Ok(new
                {
                    idSociete,
                    from,
                    to,
                    page,
                    pageSize,
                    totalResults = activities.Count,
                    activities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des activités de l'école {idSociete}");
                return StatusCode(500, new { message = "Erreur lors de la récupération des activités" });
            }
        }

        /// <summary>
        /// Recherche avancée dans les audits
        /// </summary>
        [HttpGet("search")]
        [Permission("Audit.ReadAll")]
        [ProducesResponseType(typeof(List<AuditLog>), 200)]
        public async Task<ActionResult<List<AuditLog>>> Search(
            [FromQuery] string? tableName = null,
            [FromQuery] int? recordId = null,
            [FromQuery] int? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var results = await _auditService.SearchAsync(
                    tableName, recordId, userId, action, from, to, page, pageSize);
                
                return Ok(new
                {
                    filters = new { tableName, recordId, userId, action, from, to },
                    page,
                    pageSize,
                    totalResults = results.Count,
                    results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche dans les audits");
                return StatusCode(500, new { message = "Erreur lors de la recherche" });
            }
        }

        /// <summary>
        /// Obtient les statistiques d'audit
        /// </summary>
        [HttpGet("statistics")]
        [Permission("Audit.ReadAll")]
        [ProducesResponseType(typeof(AuditStatistics), 200)]
        public async Task<ActionResult<AuditStatistics>> GetStatistics(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int? idSociete = null)
        {
            try
            {
                // Vérifier accès école si spécifié
                if (idSociete.HasValue)
                {
                    var currentUserSchoolId = this.GetCurrentUserSchoolId();
                    var currentUserRole = this.GetCurrentUserRole();

                    if (currentUserRole != "Super-Admin" && currentUserSchoolId != idSociete)
                    {
                        return Forbid();
                    }
                }

                var stats = await _auditService.GetStatisticsAsync(from, to, idSociete);
                
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques d'audit");
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques" });
            }
        }

        /// <summary>
        /// Détecte les activités suspectes (modifications massives)
        /// Accessible uniquement aux Super-Admins
        /// </summary>
        [HttpGet("suspicious")]
        [Permission("Audit.DetectSuspicious")]
        [ProducesResponseType(typeof(List<AuditLog>), 200)]
        public async Task<ActionResult<List<AuditLog>>> DetectSuspiciousActivities(
            [FromQuery] int threshold = 10,
            [FromQuery] int windowMinutes = 5)
        {
            try
            {
                var suspicious = await _auditService.DetectSuspiciousActivitiesAsync(threshold, windowMinutes);
                
                return Ok(new
                {
                    threshold,
                    windowMinutes,
                    alertCount = suspicious.Count,
                    suspicious
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la détection d'activités suspectes");
                return StatusCode(500, new { message = "Erreur lors de la détection" });
            }
        }

        /// <summary>
        /// Récupère les activités de l'utilisateur connecté
        /// </summary>
        [HttpGet("me")]
        [Authorize] // Tous les utilisateurs authentifiés
        [ProducesResponseType(typeof(List<AuditLog>), 200)]
        public async Task<ActionResult<List<AuditLog>>> GetMyActions(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = this.GetCurrentUserId();
                var actions = await _auditService.GetUserActionsAsync(userId, from, to, page, pageSize);
                
                return Ok(new
                {
                    userId,
                    userName = this.GetCurrentUserName(),
                    from,
                    to,
                    page,
                    pageSize,
                    totalResults = actions.Count,
                    actions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de mes actions");
                return StatusCode(500, new { message = "Erreur lors de la récupération" });
            }
        }
    }
}

