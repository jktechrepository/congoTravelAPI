using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/sites-touristiques/planifications")]
    [Authorize]
    public class SiteTouristiquePlanificationController : ControllerBase
    {
        private readonly ISiteTouristiquePlanificationService _planificationService;
        private readonly ISiteTouristiqueJourneeGenerationService _generationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SiteTouristiquePlanificationController> _logger;

        public SiteTouristiquePlanificationController(
            ISiteTouristiquePlanificationService planificationService,
            ISiteTouristiqueJourneeGenerationService generationService,
            ICurrentUserService currentUserService,
            ILogger<SiteTouristiquePlanificationController> logger)
        {
            _planificationService = planificationService;
            _generationService = generationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IReadOnlyList<SiteTouristiquePlanificationListItemDto>), 200)]
        public async Task<ActionResult<IReadOnlyList<SiteTouristiquePlanificationListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] int? idSiteTouristique,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var items = await _planificationService.ListAsync(
                    effectiveSocieteId,
                    idSiteTouristique,
                    cancellationToken);
                return Ok(items);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste planifications site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(SiteTouristiquePlanificationResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiquePlanificationResponseDto>> GetById(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var item = await _planificationService.GetByIdAsync(id, cancellationToken: cancellationToken);
                if (item == null)
                    return NotFound(new { message = "Planification introuvable" });

                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? item.IdSociete);
                SiteTouristiqueTenancyGuard.EnsureResourceBelongsToSociete(
                    item.IdSociete,
                    effectiveSocieteId,
                    _currentUserService.IsSuperAdmin);

                return Ok(item);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET planification site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiquePlanificationResponseDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<SiteTouristiquePlanificationResponseDto>> Create(
            [FromBody] SiteTouristiqueCreatePlanificationRequestDto request,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var created = await _planificationService.CreateAsync(request, effectiveSocieteId, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.IdSiteTouristiquePlanification }, created);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur POST planification site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiquePlanificationResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiquePlanificationResponseDto>> Update(
            int id,
            [FromBody] SiteTouristiqueUpdatePlanificationRequestDto request,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            if (id != request.IdSiteTouristiquePlanification)
                return BadRequest(new { message = "ID route et corps incohérents" });

            try
            {
                var existing = await _planificationService.GetByIdAsync(id, cancellationToken: cancellationToken);
                if (existing == null)
                    return NotFound(new { message = "Planification introuvable" });

                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                SiteTouristiqueTenancyGuard.EnsureResourceBelongsToSociete(
                    existing.IdSociete,
                    effectiveSocieteId,
                    _currentUserService.IsSuperAdmin);

                var updated = await _planificationService.UpdateAsync(request, existing.IdSociete, cancellationToken);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur PUT planification site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/toggle-statut")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ToggleStatut(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _planificationService.GetByIdAsync(id, cancellationToken: cancellationToken);
                if (existing == null)
                    return NotFound(new { message = "Planification introuvable" });

                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                SiteTouristiqueTenancyGuard.EnsureResourceBelongsToSociete(
                    existing.IdSociete,
                    effectiveSocieteId,
                    _currentUserService.IsSuperAdmin);

                await _planificationService.ToggleStatutAsync(id, existing.IdSociete, cancellationToken);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur toggle statut planification ST {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpDelete("{id:int}")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _planificationService.GetByIdAsync(id, cancellationToken: cancellationToken);
                if (existing == null)
                    return NotFound(new { message = "Planification introuvable" });

                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                SiteTouristiqueTenancyGuard.EnsureResourceBelongsToSociete(
                    existing.IdSociete,
                    effectiveSocieteId,
                    _currentUserService.IsSuperAdmin);

                await _planificationService.DeleteAsync(id, existing.IdSociete, cancellationToken);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur DELETE planification ST {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost("{id:int}/generer")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiquePlanificationGenerationResultDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiquePlanificationGenerationResultDto>> Generer(
            int id,
            [FromBody] GenererSiteTouristiquePlanificationDto request,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _planificationService.GetByIdAsync(id, cancellationToken: cancellationToken);
                if (existing == null)
                    return NotFound(new { message = "Planification introuvable" });

                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                SiteTouristiqueTenancyGuard.EnsureResourceBelongsToSociete(
                    existing.IdSociete,
                    effectiveSocieteId,
                    _currentUserService.IsSuperAdmin);

                var result = await _generationService.GenererAsync(
                    id,
                    request,
                    _currentUserService.UserId,
                    cancellationToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur génération planification ST {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }
    }
}
