using CongoTravel.Attributes;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/hotels/planifications")]
    [Authorize]
    public class HotelPlanificationController : ControllerBase
    {
        private readonly IHotelPlanificationService _planificationService;
        private readonly IHotelAllotmentGenerationService _generationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<HotelPlanificationController> _logger;

        public HotelPlanificationController(
            IHotelPlanificationService planificationService,
            IHotelAllotmentGenerationService generationService,
            ICurrentUserService currentUserService,
            ILogger<HotelPlanificationController> logger)
        {
            _planificationService = planificationService;
            _generationService = generationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [Permission("Hotel.Etablissement.Read")]
        [ProducesResponseType(typeof(IReadOnlyList<HotelPlanificationListItemDto>), 200)]
        public async Task<ActionResult<IReadOnlyList<HotelPlanificationListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] int? idHotel,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = HotelTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var items = await _planificationService.ListAsync(
                    effectiveSocieteId,
                    idHotel,
                    cancellationToken);
                return Ok(items);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste planifications hôtel");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [Permission("Hotel.Etablissement.Read")]
        [ProducesResponseType(typeof(HotelPlanificationResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<HotelPlanificationResponseDto>> GetById(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var item = await _planificationService.GetByIdAsync(id, cancellationToken: cancellationToken);
                if (item == null)
                    return NotFound(new { message = "Planification introuvable" });

                var effectiveSocieteId = HotelTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? item.IdSociete);
                HotelTenancyGuard.EnsureResourceBelongsToSociete(
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
                _logger.LogError(ex, "Erreur GET planification hôtel {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("Hotel.Etablissement.Write")]
        [ProducesResponseType(typeof(HotelPlanificationResponseDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<HotelPlanificationResponseDto>> Create(
            [FromBody] HotelCreatePlanificationRequestDto request,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = HotelTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var created = await _planificationService.CreateAsync(request, effectiveSocieteId, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.IdHotelPlanification }, created);
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
                _logger.LogError(ex, "Erreur POST planification hôtel");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}")]
        [Permission("Hotel.Etablissement.Write")]
        [ProducesResponseType(typeof(HotelPlanificationResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<HotelPlanificationResponseDto>> Update(
            int id,
            [FromBody] HotelUpdatePlanificationRequestDto request,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            if (id != request.IdHotelPlanification)
                return BadRequest(new { message = "ID route et corps incohérents" });

            try
            {
                var existing = await _planificationService.GetByIdAsync(id, cancellationToken: cancellationToken);
                if (existing == null)
                    return NotFound(new { message = "Planification introuvable" });

                var effectiveSocieteId = HotelTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                HotelTenancyGuard.EnsureResourceBelongsToSociete(
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
                _logger.LogError(ex, "Erreur PUT planification hôtel {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/toggle-statut")]
        [Permission("Hotel.Etablissement.Write")]
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

                var effectiveSocieteId = HotelTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                HotelTenancyGuard.EnsureResourceBelongsToSociete(
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
                _logger.LogError(ex, "Erreur toggle statut planification hôtel {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpDelete("{id:int}")]
        [Permission("Hotel.Etablissement.Write")]
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

                var effectiveSocieteId = HotelTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                HotelTenancyGuard.EnsureResourceBelongsToSociete(
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
                _logger.LogError(ex, "Erreur DELETE planification hôtel {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost("{id:int}/generer")]
        [Permission("Hotel.Etablissement.Write")]
        [ProducesResponseType(typeof(HotelPlanificationGenerationResultDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<HotelPlanificationGenerationResultDto>> Generer(
            int id,
            [FromBody] GenererHotelPlanificationDto request,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _planificationService.GetByIdAsync(id, cancellationToken: cancellationToken);
                if (existing == null)
                    return NotFound(new { message = "Planification introuvable" });

                var effectiveSocieteId = HotelTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                HotelTenancyGuard.EnsureResourceBelongsToSociete(
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
                _logger.LogError(ex, "Erreur génération planification hôtel {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }
    }
}
