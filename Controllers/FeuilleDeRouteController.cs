using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Models.DTOs.FeuilleDeRoute;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeuilleDeRouteController : ControllerBase
    {
        private readonly IFeuilleDeRouteService _feuilleDeRouteService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<FeuilleDeRouteController> _logger;

        public FeuilleDeRouteController(
            IFeuilleDeRouteService feuilleDeRouteService,
            ICurrentUserService currentUserService,
            ILogger<FeuilleDeRouteController> logger)
        {
            _feuilleDeRouteService = feuilleDeRouteService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Génère et historise une feuille de route à partir des passagers déjà embarqués du voyage.
        /// </summary>
        [HttpPost("generer")]
        [Permission("FeuilleDeRoute.Generer")]
        [ProducesResponseType(typeof(FeuilleDeRouteDetailDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<FeuilleDeRouteDetailDto>> Generer(
            [FromBody] GenererFeuilleDeRouteDto dto,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var idSocieteVoyage = await _feuilleDeRouteService.GetVoyageSocieteIdAsync(dto.IdVoyage, cancellationToken);
                if (idSocieteVoyage == null)
                    return NotFound(new { message = $"Voyage {dto.IdVoyage} introuvable." });

                var forbid = EnsureSocieteScope(idSocieteVoyage.Value);
                if (forbid != null)
                    return forbid;

                var detail = await _feuilleDeRouteService.GenererAsync(
                    dto.IdVoyage,
                    _currentUserService.IsAuthenticated ? _currentUserService.UserId : null,
                    cancellationToken);

                return CreatedAtAction(nameof(GetById), new { id = detail.IdFeuilleDeRoute }, detail);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur génération FeuilleDeRoute pour voyage {IdVoyage}", dto.IdVoyage);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Détail complet d'une feuille de route (société, voyage, passagers).</summary>
        [HttpGet("{id:int}")]
        [Permission("FeuilleDeRoute.Read")]
        [ProducesResponseType(typeof(FeuilleDeRouteDetailDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<FeuilleDeRouteDetailDto>> GetById(
            int id,
            CancellationToken cancellationToken = default)
        {
            var detail = await _feuilleDeRouteService.GetByIdAsync(id, cancellationToken);
            if (detail == null)
                return NotFound(new { message = "Feuille de route introuvable" });

            var forbid = EnsureSocieteScope(detail.IdSociete);
            if (forbid != null)
                return forbid;

            return Ok(detail);
        }

        /// <summary>Historique des feuilles de route d'une société (filtres idVoyage, dateEmbarquement).</summary>
        [HttpGet("societe/{idSociete:int}")]
        [Permission("FeuilleDeRoute.Read")]
        [ProducesResponseType(typeof(PagedResult<FeuilleDeRouteListItemDto>), 200)]
        public async Task<ActionResult<PagedResult<FeuilleDeRouteListItemDto>>> GetBySociete(
            int idSociete,
            [FromQuery] int? idVoyage = null,
            [FromQuery] DateTime? dateEmbarquement = null,
            [FromQuery] PagedRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            if (idSociete <= 0)
                return BadRequest(new { message = "idSociete invalide" });

            var forbid = EnsureSocieteScope(idSociete);
            if (forbid != null)
                return forbid;

            request ??= new PagedRequest();
            var result = await _feuilleDeRouteService.GetBySocieteAsync(
                idSociete, idVoyage, dateEmbarquement, request, cancellationToken);
            return Ok(result);
        }

        /// <summary>Historique des feuilles de route d'un voyage.</summary>
        [HttpGet("voyage/{idVoyage:int}")]
        [Permission("FeuilleDeRoute.Read")]
        [ProducesResponseType(typeof(IReadOnlyList<FeuilleDeRouteListItemDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IReadOnlyList<FeuilleDeRouteListItemDto>>> GetByVoyage(
            int idVoyage,
            CancellationToken cancellationToken = default)
        {
            if (idVoyage <= 0)
                return BadRequest(new { message = "idVoyage invalide" });

            var idSocieteVoyage = await _feuilleDeRouteService.GetVoyageSocieteIdAsync(idVoyage, cancellationToken);
            if (idSocieteVoyage == null)
                return NotFound(new { message = $"Voyage {idVoyage} introuvable." });

            var forbid = EnsureSocieteScope(idSocieteVoyage.Value);
            if (forbid != null)
                return forbid;

            var items = await _feuilleDeRouteService.GetByVoyageAsync(idVoyage, cancellationToken);
            return Ok(items);
        }

        private ActionResult? EnsureSocieteScope(int idSociete)
        {
            if (_currentUserService.IsSuperAdmin)
                return null;

            if (_currentUserService.SocieteId != idSociete)
            {
                _logger.LogWarning(
                    "Accès FeuilleDeRoute refusé: mismatch société. cible={SocieteId}, token={TokenSocieteId}",
                    idSociete,
                    _currentUserService.SocieteId);
                return StatusCode(403, new { message = "Accès refusé: société non autorisée" });
            }

            return null;
        }
    }
}
