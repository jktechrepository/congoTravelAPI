using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/sites-touristiques/classes")]
    [Authorize]
    public class SiteTouristiqueClasseController : ControllerBase
    {
        private readonly ISiteTouristiqueClasseService _classeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SiteTouristiqueClasseController> _logger;

        public SiteTouristiqueClasseController(
            ISiteTouristiqueClasseService classeService,
            ICurrentUserService currentUserService,
            ILogger<SiteTouristiqueClasseController> logger)
        {
            _classeService = classeService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Liste les classes site touristique de la société (JWT ou idSociete Super-Admin).</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueClasseResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueClasseResponseDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] bool actifsSeulement = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var classes = await _classeService.ListAsync(
                    effectiveSocieteId,
                    actifsSeulement,
                    cancellationToken);
                return Ok(classes);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste classes site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les classes site touristique d'une société (alias explicite, comme CategorieSiege).</summary>
        [HttpGet("societe/{idSociete:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueClasseResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueClasseResponseDto>>> GetBySociete(
            int idSociete,
            [FromQuery] bool actifsSeulement = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var classes = await _classeService.ListAsync(
                    effectiveSocieteId,
                    actifsSeulement,
                    cancellationToken);
                return Ok(classes);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET classes site touristique société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Recherche une classe par libellé exact (insensible à la casse) dans la société.</summary>
        [HttpGet("by-libelle")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SiteTouristiqueClasseResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueClasseResponseDto>> GetByLibelle(
            [FromQuery] string libelle,
            [FromQuery] int? idSociete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(libelle))
                    return BadRequest(new { message = "Le paramètre libelle est obligatoire." });

                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var classe = await _classeService.GetByLibelleAsync(
                    libelle,
                    effectiveSocieteId,
                    cancellationToken);

                if (classe == null)
                    return NotFound(new { message = $"Aucune classe site touristique avec le libellé '{libelle.Trim()}'." });

                return Ok(classe);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET classe site touristique par libellé {Libelle}", libelle);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<SiteTouristiqueClasseResponseDto>> GetById(int id)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var classe = await _classeService.GetByIdAsync(id, idSociete);
                if (classe == null)
                    return NotFound(new { message = $"Classe site touristique {id} introuvable." });

                return Ok(classe);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET classe site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("SiteTouristique.Classe.Write")]
        public async Task<ActionResult<SiteTouristiqueClasseResponseDto>> Create(
            [FromBody] SiteTouristiqueCreateClasseRequestDto request)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var created = await _classeService.CreateAsync(request, idSociete);
                return CreatedAtAction(nameof(GetById), new { id = created.IdSiteTouristiqueClasse }, created);
            }
            catch (SiteTouristiqueClasseConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur POST classe site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}")]
        [Permission("SiteTouristique.Classe.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueClasseResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueClasseResponseDto>> Update(
            int id,
            [FromBody] SiteTouristiqueUpdateClasseRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var updated = await _classeService.UpdateAsync(id, request, idSociete, cancellationToken);
                if (updated == null)
                    return NotFound(new { message = $"Classe site touristique {id} introuvable." });

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur PUT classe site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/toggle-statut")]
        [Permission("SiteTouristique.Classe.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueClasseResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueClasseResponseDto>> ToggleStatut(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var updated = await _classeService.ToggleStatutAsync(id, idSociete, cancellationToken);
                if (updated == null)
                    return NotFound(new { message = $"Classe site touristique {id} introuvable." });

                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur toggle-statut classe site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }
    }
}
