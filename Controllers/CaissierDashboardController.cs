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
    public class CaissierDashboardController : ControllerBase
    {
        private readonly CaissierDashboardService _caissierDashboardService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CaissierDashboardController> _logger;

        public CaissierDashboardController(
            CaissierDashboardService caissierDashboardService,
            ICurrentUserService currentUserService,
            ILogger<CaissierDashboardController> logger)
        {
            _caissierDashboardService = caissierDashboardService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard Caissier transport (société + transactions du caissier JWT). Accès : Caissier ou Super-Admin.
        /// Super-Admin : les métriques restent filtrées sur <c>JWT.UserId</c> (vue personnelle, pas globale).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(CaissierDashboardDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<CaissierDashboardDto>> GetCaissierDashboard(
            CancellationToken cancellationToken = default)
        {
            if (_currentUserService.UserRole != UserRoles.CAISSIER && !_currentUserService.IsSuperAdmin)
            {
                _logger.LogWarning(
                    "Accès CaissierDashboard refusé pour l'utilisateur {UserId} (rôle {Role})",
                    _currentUserService.UserId,
                    _currentUserService.UserRole);
                return Forbid();
            }

            if (_currentUserService.SocieteId <= 0)
            {
                _logger.LogWarning(
                    "Accès CaissierDashboard refusé: SocieteId invalide pour l'utilisateur {UserId}",
                    _currentUserService.UserId);
                return StatusCode(403, "Accès refusé: société du token invalide");
            }

            try
            {
                _logger.LogInformation(
                    "Génération du dashboard Caissier pour la société {SocieteId}",
                    _currentUserService.SocieteId);

                var dashboard = await _caissierDashboardService.GetDashboardDataAsync(cancellationToken);
                return Ok(dashboard);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Accès non autorisé au dashboard Caissier");
                return StatusCode(403, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard Caissier");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Rapport caisse du caissier connecté (espèces vs électronique). Scope JWT : société + utilisateur du token.
        /// Accès : Caissier ou Super-Admin.
        /// </summary>
        [HttpGet("rapport-caisse")]
        [ProducesResponseType(typeof(RapportCaisseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<RapportCaisseDto>> GetRapportCaisse(
            [FromQuery] DateTime? datePrecise,
            [FromQuery] DateTime? dateDebut,
            [FromQuery] DateTime? dateFin,
            CancellationToken cancellationToken = default)
        {
            if (_currentUserService.UserRole != UserRoles.CAISSIER && !_currentUserService.IsSuperAdmin)
            {
                _logger.LogWarning(
                    "Accès rapport caisse caissier refusé pour l'utilisateur {UserId} (rôle {Role})",
                    _currentUserService.UserId,
                    _currentUserService.UserRole);
                return Forbid();
            }

            if (_currentUserService.SocieteId <= 0)
            {
                _logger.LogWarning(
                    "Accès rapport caisse caissier refusé: SocieteId invalide pour l'utilisateur {UserId}",
                    _currentUserService.UserId);
                return StatusCode(403, "Accès refusé: société du token invalide");
            }

            try
            {
                var rapport = await _caissierDashboardService.GetRapportCaisseAsync(
                    datePrecise, dateDebut, dateFin, cancellationToken);
                return Ok(rapport);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Accès non autorisé au rapport caisse caissier");
                return StatusCode(403, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du rapport caisse caissier");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
    }
}
