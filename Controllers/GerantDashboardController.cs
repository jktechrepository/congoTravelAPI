using CongoTravel.Models.DTOs;
using CongoTravel.Models.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GerantDashboardController : ControllerBase
    {
        private readonly GerantDashboardService _gerantDashboardService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GerantDashboardController> _logger;

        public GerantDashboardController(
            GerantDashboardService gerantDashboardService,
            ICurrentUserService currentUserService,
            ILogger<GerantDashboardController> logger)
        {
            _gerantDashboardService = gerantDashboardService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard Gérant transport (société + site du JWT ; repli société si site absent). Accès : Gérant ou Super-Admin.
        /// Super-Admin : les métriques restent filtrées sur le site du token (pas de vue société globale).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(GerantDashboardDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<GerantDashboardDto>> GetGerantDashboard(
            CancellationToken cancellationToken = default)
        {
            if (_currentUserService.UserRole != UserRoles.GERANT && !_currentUserService.IsSuperAdmin)
            {
                _logger.LogWarning(
                    "Accès GerantDashboard refusé pour l'utilisateur {UserId} (rôle {Role})",
                    _currentUserService.UserId,
                    _currentUserService.UserRole);
                return Forbid();
            }

            if (_currentUserService.SocieteId <= 0)
            {
                _logger.LogWarning(
                    "Accès GerantDashboard refusé: SocieteId invalide pour l'utilisateur {UserId}",
                    _currentUserService.UserId);
                return StatusCode(403, "Accès refusé: société du token invalide");
            }

            try
            {
                var siteLabel = _currentUserService.SiteId is > 0
                    ? _currentUserService.SiteId.Value.ToString()
                    : "fallback société";

                _logger.LogInformation(
                    "Génération du dashboard Gérant pour la société {SocieteId}, site {SiteScope}",
                    _currentUserService.SocieteId,
                    siteLabel);

                var dashboard = await _gerantDashboardService.GetDashboardDataAsync(cancellationToken);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard Gérant");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
    }
}
