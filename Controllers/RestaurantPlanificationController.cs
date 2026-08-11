using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/restaurants/planifications")]
    [Authorize]
    public class RestaurantPlanificationController : ControllerBase
    {
        private readonly IRestaurantPlanificationService _planificationService;
        private readonly IRestaurantCreneauGenerationService _generationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RestaurantPlanificationController> _logger;

        public RestaurantPlanificationController(
            IRestaurantPlanificationService planificationService,
            IRestaurantCreneauGenerationService generationService,
            ICurrentUserService currentUserService,
            ILogger<RestaurantPlanificationController> logger)
        {
            _planificationService = planificationService;
            _generationService = generationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(IReadOnlyList<RestaurantPlanificationListItemDto>), 200)]
        public async Task<ActionResult<IReadOnlyList<RestaurantPlanificationListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] int? idRestaurant,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var items = await _planificationService.ListAsync(
                    effectiveSocieteId,
                    idRestaurant,
                    cancellationToken);
                return Ok(items);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste planifications restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(RestaurantPlanificationResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantPlanificationResponseDto>> GetById(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var item = await _planificationService.GetByIdAsync(id, cancellationToken: cancellationToken);
                if (item == null)
                    return NotFound(new { message = "Planification introuvable" });

                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? item.IdSociete);
                RestaurantTenancyGuard.EnsureResourceBelongsToSociete(
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
                _logger.LogError(ex, "Erreur GET planification restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantPlanificationResponseDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<RestaurantPlanificationResponseDto>> Create(
            [FromBody] RestaurantCreatePlanificationRequestDto request,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var created = await _planificationService.CreateAsync(request, effectiveSocieteId, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.IdRestaurantPlanification }, created);
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
                _logger.LogError(ex, "Erreur POST planification restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}")]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantPlanificationResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantPlanificationResponseDto>> Update(
            int id,
            [FromBody] RestaurantUpdatePlanificationRequestDto request,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            if (id != request.IdRestaurantPlanification)
                return BadRequest(new { message = "ID route et corps incohérents" });

            try
            {
                var existing = await _planificationService.GetByIdAsync(id, cancellationToken: cancellationToken);
                if (existing == null)
                    return NotFound(new { message = "Planification introuvable" });

                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                RestaurantTenancyGuard.EnsureResourceBelongsToSociete(
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
                _logger.LogError(ex, "Erreur PUT planification restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/toggle-statut")]
        [Permission("Restaurant.Etablissement.Write")]
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

                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                RestaurantTenancyGuard.EnsureResourceBelongsToSociete(
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
                _logger.LogError(ex, "Erreur toggle statut planification restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpDelete("{id:int}")]
        [Permission("Restaurant.Etablissement.Write")]
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

                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                RestaurantTenancyGuard.EnsureResourceBelongsToSociete(
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
                _logger.LogError(ex, "Erreur DELETE planification restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost("{id:int}/generer")]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantPlanificationGenerationResultDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantPlanificationGenerationResultDto>> Generer(
            int id,
            [FromBody] GenererRestaurantPlanificationDto request,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _planificationService.GetByIdAsync(id, cancellationToken: cancellationToken);
                if (existing == null)
                    return NotFound(new { message = "Planification introuvable" });

                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete ?? existing.IdSociete);
                RestaurantTenancyGuard.EnsureResourceBelongsToSociete(
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
                _logger.LogError(ex, "Erreur génération planification restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }
    }
}
