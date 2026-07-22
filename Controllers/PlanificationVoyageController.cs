using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Models.DTOs.PlanificationVoyage;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlanificationVoyageController : ControllerBase
    {
        private readonly IPlanificationVoyageService _planificationService;
        private readonly IVoyageGenerationService _generationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<PlanificationVoyageController> _logger;

        public PlanificationVoyageController(
            IPlanificationVoyageService planificationService,
            IVoyageGenerationService generationService,
            ICurrentUserService currentUserService,
            ILogger<PlanificationVoyageController> logger)
        {
            _planificationService = planificationService;
            _generationService = generationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [Permission("Voyage.Read")]
        [ProducesResponseType(typeof(IReadOnlyList<PlanificationVoyageResponseDto>), 200)]
        public async Task<ActionResult<IReadOnlyList<PlanificationVoyageResponseDto>>> GetBySociete(
            [FromQuery] int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (idSociete <= 0)
                return BadRequest(new { message = "idSociete invalide" });

            var forbid = EnsureSocieteScope(idSociete);
            if (forbid != null)
                return forbid;

            var items = await _planificationService.GetBySocieteAsync(idSociete, cancellationToken);
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        [Permission("Voyage.Read")]
        [ProducesResponseType(typeof(PlanificationVoyageResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PlanificationVoyageResponseDto>> GetById(
            int id,
            CancellationToken cancellationToken = default)
        {
            var item = await _planificationService.GetByIdAsync(id, cancellationToken);
            if (item == null)
                return NotFound(new { message = "Planification introuvable" });

            var forbid = EnsureSocieteScope(item.IdSociete);
            if (forbid != null)
                return forbid;

            return Ok(item);
        }

        [HttpPost]
        [Permission("Voyage.Create")]
        [ProducesResponseType(typeof(PlanificationVoyageResponseDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PlanificationVoyageResponseDto>> Create(
            [FromBody] CreatePlanificationVoyageDto dto,
            CancellationToken cancellationToken = default)
        {
            var forbid = EnsureSocieteScope(dto.IdSociete);
            if (forbid != null)
                return forbid;

            try
            {
                var created = await _planificationService.CreateAsync(dto, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.IdPlanificationVoyage }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Permission("Voyage.Update")]
        [ProducesResponseType(typeof(PlanificationVoyageResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PlanificationVoyageResponseDto>> Update(
            int id,
            [FromBody] UpdatePlanificationVoyageDto dto,
            CancellationToken cancellationToken = default)
        {
            if (id != dto.IdPlanificationVoyage)
                return BadRequest(new { message = "ID route et corps incohérents" });

            var existing = await _planificationService.GetByIdAsync(id, cancellationToken);
            if (existing == null)
                return NotFound(new { message = "Planification introuvable" });

            var forbid = EnsureSocieteScope(existing.IdSociete);
            if (forbid != null)
                return forbid;

            if (dto.IdSociete != existing.IdSociete)
                return BadRequest(new { message = "Impossible de changer la société du template" });

            try
            {
                var updated = await _planificationService.UpdateAsync(dto, cancellationToken);
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}/toggle-statut")]
        [Permission("Voyage.Update")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ToggleStatut(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _planificationService.GetByIdAsync(id, cancellationToken);
            if (existing == null)
                return NotFound(new { message = "Planification introuvable" });

            var forbid = EnsureSocieteScope(existing.IdSociete);
            if (forbid != null)
                return forbid;

            await _planificationService.ToggleStatutAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Permission("Voyage.Delete")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _planificationService.GetByIdAsync(id, cancellationToken);
            if (existing == null)
                return NotFound(new { message = "Planification introuvable" });

            var forbid = EnsureSocieteScope(existing.IdSociete);
            if (forbid != null)
                return forbid;

            try
            {
                await _planificationService.DeleteAsync(id, cancellationToken);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/generer")]
        [Permission("Voyage.Create")]
        [ProducesResponseType(typeof(PlanificationGenerationResultDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PlanificationGenerationResultDto>> Generer(
            int id,
            [FromBody] GenererPlanificationVoyageDto request,
            CancellationToken cancellationToken = default)
        {
            var existing = await _planificationService.GetByIdAsync(id, cancellationToken);
            if (existing == null)
                return NotFound(new { message = "Planification introuvable" });

            var forbid = EnsureSocieteScope(existing.IdSociete);
            if (forbid != null)
                return forbid;

            try
            {
                var result = await _generationService.GenererAsync(
                    id,
                    request,
                    _currentUserService.UserId,
                    cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private ActionResult? EnsureSocieteScope(int idSociete)
        {
            if (_currentUserService.IsSuperAdmin)
                return null;

            if (_currentUserService.SocieteId != idSociete)
            {
                _logger.LogWarning(
                    "Accès planification refusé: mismatch société. route/query={SocieteId}, token={TokenSocieteId}",
                    idSociete,
                    _currentUserService.SocieteId);
                return StatusCode(403, new { message = "Accès refusé: société non autorisée" });
            }

            return null;
        }
    }
}
