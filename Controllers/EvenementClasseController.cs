using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/events/classes")]
    [Authorize]
    public class EvenementClasseController : ControllerBase
    {
        private readonly IEvenementClasseService _classeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<EvenementClasseController> _logger;

        public EvenementClasseController(
            IEvenementClasseService classeService,
            ICurrentUserService currentUserService,
            ILogger<EvenementClasseController> logger)
        {
            _classeService = classeService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Liste les classes événement de la société (JWT ou idSociete Super-Admin).</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<EvenementClasseResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementClasseResponseDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] bool actifsSeulement = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(
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
                _logger.LogError(ex, "Erreur GET liste classes événement");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les classes événement d'une société (alias explicite, comme CategorieSiege).</summary>
        [HttpGet("societe/{idSociete:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<EvenementClasseResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementClasseResponseDto>>> GetBySociete(
            int idSociete,
            [FromQuery] bool actifsSeulement = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(
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
                _logger.LogError(ex, "Erreur GET classes événement société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Recherche une classe par libellé exact (insensible à la casse) dans la société.</summary>
        [HttpGet("by-libelle")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EvenementClasseResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementClasseResponseDto>> GetByLibelle(
            [FromQuery] string libelle,
            [FromQuery] int? idSociete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(libelle))
                    return BadRequest(new { message = "Le paramètre libelle est obligatoire." });

                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var classe = await _classeService.GetByLibelleAsync(
                    libelle,
                    effectiveSocieteId,
                    cancellationToken);

                if (classe == null)
                    return NotFound(new { message = $"Aucune classe événement avec le libellé '{libelle.Trim()}'." });

                return Ok(classe);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET classe événement par libellé {Libelle}", libelle);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<EvenementClasseResponseDto>> GetById(int id)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var classe = await _classeService.GetByIdAsync(id, idSociete);
                if (classe == null)
                    return NotFound(new { message = $"Classe événement {id} introuvable." });

                return Ok(classe);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET classe événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("Evenement.Session.Write")]
        public async Task<ActionResult<EvenementClasseResponseDto>> Create(
            [FromBody] EvenementCreateClasseRequestDto request)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var created = await _classeService.CreateAsync(request, idSociete);
                return CreatedAtAction(nameof(GetById), new { id = created.IdEvenementClasse }, created);
            }
            catch (EvenementClasseConflictException ex)
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
                _logger.LogError(ex, "Erreur POST classe événement");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}")]
        [Permission("Evenement.Session.Write")]
        [ProducesResponseType(typeof(EvenementClasseResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementClasseResponseDto>> Update(
            int id,
            [FromBody] EvenementUpdateClasseRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var updated = await _classeService.UpdateAsync(id, request, idSociete, cancellationToken);
                if (updated == null)
                    return NotFound(new { message = $"Classe événement {id} introuvable." });

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
                _logger.LogError(ex, "Erreur PUT classe événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/toggle-statut")]
        [Permission("Evenement.Session.Write")]
        [ProducesResponseType(typeof(EvenementClasseResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementClasseResponseDto>> ToggleStatut(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var updated = await _classeService.ToggleStatutAsync(id, idSociete, cancellationToken);
                if (updated == null)
                    return NotFound(new { message = $"Classe événement {id} introuvable." });

                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur toggle-statut classe événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }
    }
}
