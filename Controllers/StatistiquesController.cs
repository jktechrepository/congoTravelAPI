using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Models.DTOs.Statistiques;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StatistiquesController : ControllerBase
    {
        private readonly IStatistiquesService _statistiquesService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<StatistiquesController> _logger;

        public StatistiquesController(
            IStatistiquesService statistiquesService,
            ICurrentUserService currentUserService,
            ILogger<StatistiquesController> logger)
        {
            _statistiquesService = statistiquesService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Statistiques transport consolidées pour une société (période optionnelle).
        /// </summary>
        [HttpGet("{idSociete:int}")]
        [Permission("Statistiques.ReadAll")]
        [ProducesResponseType(typeof(StatistiquesTransportDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<StatistiquesTransportDto>> GetStatistiques(
            int idSociete,
            [FromQuery] DateTime? debut = null,
            [FromQuery] DateTime? fin = null,
            CancellationToken cancellationToken = default)
        {
            if (idSociete <= 0)
                return BadRequest(new { message = "ID de société invalide" });

            if (!_currentUserService.IsSuperAdmin && _currentUserService.SocieteId != idSociete)
            {
                _logger.LogWarning(
                    "Accès Statistiques refusé: mismatch société route/token. route={RouteSocieteId}, token={TokenSocieteId}, user={UserId}",
                    idSociete,
                    _currentUserService.SocieteId,
                    _currentUserService.UserId);
                return StatusCode(403, new { message = "Accès refusé: la société demandée ne correspond pas au token" });
            }

            try
            {
                var statistiques = await _statistiquesService.GetStatistiquesAsync(
                    idSociete, debut, fin, cancellationToken);
                return Ok(statistiques);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur Statistiques transport société {SocieteId}", idSociete);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
    }
}
