using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/sites-touristiques/journees")]
    [Authorize]
    public class SiteTouristiqueJourneeController : ControllerBase
    {
        private readonly ISiteTouristiqueJourneeService _journeeService;
        private readonly ISiteTouristiqueAvailabilityService _availabilityService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SiteTouristiqueJourneeController> _logger;

        public SiteTouristiqueJourneeController(
            ISiteTouristiqueJourneeService journeeService,
            ISiteTouristiqueAvailabilityService availabilityService,
            ICurrentUserService currentUserService,
            ILogger<SiteTouristiqueJourneeController> logger)
        {
            _journeeService = journeeService;
            _availabilityService = availabilityService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueJourneeListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueJourneeListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] int? idSiteTouristique,
            [FromQuery] string? status,
            [FromQuery] string? inventoryMode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!TryParseOptionalInventoryMode(inventoryMode, out var parsedMode, out var modeError))
                    return BadRequest(new { message = modeError });

                var isStaffTenant = SiteTouristiqueTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (!isStaffTenant || !effectiveSocieteId.HasValue)
                {
                    var published = await _journeeService.ListPublishedGlobalAsync(
                        new SiteTouristiqueJourneeListFilter
                        {
                            InventoryMode = parsedMode,
                            IdSociete = idSociete is > 0 ? idSociete : null,
                            IdSiteTouristique = idSiteTouristique is > 0 ? idSiteTouristique : null
                        },
                        cancellationToken);
                    return Ok(published);
                }

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var journees = await _journeeService.ListAsync(
                    effectiveSocieteId.Value,
                    new SiteTouristiqueJourneeListFilter
                    {
                        Status = parsedStatus,
                        InventoryMode = parsedMode,
                        IdSiteTouristique = idSiteTouristique is > 0 ? idSiteTouristique : null
                    },
                    cancellationToken);
                return Ok(journees);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste journées site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SiteTouristiqueJourneeResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueJourneeResponseDto>> GetById(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isStaffTenant = SiteTouristiqueTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (isStaffTenant && effectiveSocieteId.HasValue)
                {
                    var journee = await _journeeService.GetByIdAsync(id, effectiveSocieteId.Value, cancellationToken);
                    if (journee == null)
                        return NotFound(new { message = $"Journée {id} introuvable." });
                    return Ok(journee);
                }

                var published = await _journeeService.GetPublishedByIdAsync(id, cancellationToken);
                if (published == null)
                    return NotFound(new { message = $"Journée Published {id} introuvable." });
                return Ok(published);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET journée {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}/availability")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SiteTouristiqueAvailabilityResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueAvailabilityResponseDto>> GetAvailability(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var journee = await _journeeService.GetPublishedByIdAsync(id, cancellationToken);
                if (journee == null)
                {
                    // Staff : lecture y compris Draft
                    if (SiteTouristiqueTenancyGuard.TryResolveStaffTenantForCatalogList(
                            _currentUserService,
                            null,
                            out var staffSociete)
                        && staffSociete.HasValue)
                    {
                        journee = await _journeeService.GetByIdAsync(id, staffSociete.Value, cancellationToken);
                    }
                }

                if (journee == null)
                    return NotFound(new { message = $"Journée {id} introuvable." });

                var availability = await _availabilityService.GetJourneeAvailabilityAsync(
                    id,
                    journee.IdSociete,
                    cancellationToken);

                if (availability == null)
                    return NotFound(new { message = $"Disponibilités journée {id} introuvables." });

                return Ok(availability);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET availability journée {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("date/{date}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueJourneeListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueJourneeListItemDto>>> GetByDate(
            string date,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!DateOnly.TryParse(date, out var dateVisite))
                    return BadRequest(new { message = "Format de date invalide (attendu yyyy-MM-dd)." });

                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var journees = await _journeeService.ListByDateAsync(
                    dateVisite,
                    effectiveSocieteId,
                    cancellationToken);
                return Ok(journees);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET journées date {Date}", date);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueJourneeResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<SiteTouristiqueJourneeResponseDto>> CreateDraft(
            [FromBody] SiteTouristiqueCreateJourneeRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var created = await _journeeService.CreateDraftAsync(request, idSociete, cancellationToken: cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.IdSiteTouristiqueJournee }, created);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (SiteTouristiqueJourneeConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur POST journée site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Met à jour une journée Draft (date, devise, fenêtres, quotas) ou Published
        /// (fenêtres ; capacité/prix seulement sans vente active).
        /// </summary>
        [HttpPut("{id:int}")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueJourneeResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<SiteTouristiqueJourneeResponseDto>> Update(
            int id,
            [FromBody] SiteTouristiqueUpdateJourneeRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var updated = await _journeeService.UpdateAsync(id, request, idSociete, cancellationToken);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (SiteTouristiqueJourneeConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur PUT journée site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Supprime une journée (Draft ou Published) sans vente active ni commande en attente.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                await _journeeService.DeleteAsync(id, idSociete, cancellationToken);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (SiteTouristiqueJourneeConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur DELETE journée site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/publish")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueJourneeResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueJourneeResponseDto>> Publish(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var published = await _journeeService.PublishAsync(id, idSociete, cancellationToken);
                return Ok(published);
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
                _logger.LogError(ex, "Erreur publish journée {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Soft-delete : passe la journée en Cancelled (Draft ou Published).
        /// Idempotent si déjà Cancelled. Closed → 400.
        /// </summary>
        [HttpPut("{id:int}/cancel")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueJourneeResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueJourneeResponseDto>> Cancel(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var cancelled = await _journeeService.CancelAsync(id, idSociete, cancellationToken);
                return Ok(cancelled);
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
                _logger.LogError(ex, "Erreur cancel journée {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Clôture opérationnelle : passe la journée en Closed (Draft ou Published).
        /// Idempotent si déjà Closed. Cancelled → 400.
        /// </summary>
        [HttpPut("{id:int}/close")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueJourneeResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueJourneeResponseDto>> Close(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var closed = await _journeeService.CloseAsync(id, idSociete, cancellationToken);
                return Ok(closed);
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
                _logger.LogError(ex, "Erreur close journée {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        private static bool TryParseOptionalStatus(
            string? status,
            out SiteTouristiqueStatus? parsed,
            out string? error)
        {
            parsed = null;
            error = null;
            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (!Enum.TryParse<SiteTouristiqueStatus>(status.Trim(), ignoreCase: true, out var value))
            {
                error = $"Statut invalide '{status}'. Valeurs : Draft, Published, Closed, Cancelled.";
                return false;
            }

            parsed = value;
            return true;
        }

        private static bool TryParseOptionalInventoryMode(
            string? inventoryMode,
            out SiteTouristiqueInventoryMode? parsed,
            out string? error)
        {
            parsed = null;
            error = null;
            if (string.IsNullOrWhiteSpace(inventoryMode))
                return true;

            if (!Enum.TryParse<SiteTouristiqueInventoryMode>(inventoryMode.Trim(), ignoreCase: true, out var value))
            {
                error = $"InventoryMode invalide '{inventoryMode}'. Valeurs : GlobalQuota, ClassQuota.";
                return false;
            }

            parsed = value;
            return true;
        }
    }
}
