using CongoTravel.Models.DTOs;
using CongoTravel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            DashboardService dashboardService,
            ICurrentUserService currentUserService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard Admin société (transport + collecte). La société demandée doit correspondre au token JWT.
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <returns>Statistiques du dashboard pour la société</returns>
        [HttpGet("{idSociete}")]
        [Permission("Dashboard.ReadAll")]
        [ProducesResponseType(typeof(DashboardDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DashboardDto>> GetDashboardStats(int idSociete)
        {
            try
            {
                if (idSociete <= 0)
                {
                    return BadRequest("ID de société invalide");
                }

                var tokenSocieteId = _currentUserService.SocieteId;
                if (tokenSocieteId <= 0)
                {
                    _logger.LogWarning("Accès dashboard refusé: claim SocieteId invalide pour l'utilisateur {UserId}", _currentUserService.UserId);
                    return StatusCode(403, "Accès refusé: société du token invalide");
                }

                if (idSociete != tokenSocieteId)
                {
                    _logger.LogWarning(
                        "Accès dashboard refusé: mismatch société route/token. route={RouteSocieteId}, token={TokenSocieteId}, user={UserId}",
                        idSociete,
                        tokenSocieteId,
                        _currentUserService.UserId);
                    return StatusCode(403, "Accès refusé: la société demandée ne correspond pas au token");
                }

                _logger.LogInformation("Récupération des statistiques du dashboard pour la société {SocieteId}", idSociete);
                
                var dashboard = await _dashboardService.GetDashboardDataAsync(idSociete);
                
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques du dashboard pour la société {SocieteId}", idSociete);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }
    }
}
