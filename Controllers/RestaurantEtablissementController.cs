using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/restaurants/etablissements")]
    [Authorize]
    public class RestaurantEtablissementController : ControllerBase
    {
        private readonly IRestaurantEtablissementService _etablissementService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RestaurantEtablissementController> _logger;

        public RestaurantEtablissementController(
            IRestaurantEtablissementService etablissementService,
            ICurrentUserService currentUserService,
            ILogger<RestaurantEtablissementController> logger)
        {
            _etablissementService = etablissementService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<RestaurantEtablissementListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<RestaurantEtablissementListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] string? status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isStaffTenant = RestaurantTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (!isStaffTenant || !effectiveSocieteId.HasValue)
                {
                    var published = await _etablissementService.ListPublishedGlobalAsync(
                        new RestaurantEtablissementListFilter
                        {
                            IdSociete = idSociete is > 0 ? idSociete : null
                        },
                        cancellationToken);
                    return Ok(published);
                }

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var etablissements = await _etablissementService.ListAsync(
                    effectiveSocieteId.Value,
                    new RestaurantEtablissementListFilter { Status = parsedStatus },
                    cancellationToken);
                return Ok(etablissements);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste établissements restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RestaurantEtablissementResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantEtablissementResponseDto>> GetById(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isStaffTenant = RestaurantTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (isStaffTenant && effectiveSocieteId.HasValue)
                {
                    var etablissement = await _etablissementService.GetByIdAsync(
                        id, effectiveSocieteId.Value, cancellationToken);
                    if (etablissement == null)
                        return NotFound(new { message = $"Établissement {id} introuvable." });
                    return Ok(etablissement);
                }

                var published = await _etablissementService.GetPublishedByIdAsync(id, cancellationToken);
                if (published == null)
                    return NotFound(new { message = $"Établissement Published {id} introuvable." });
                return Ok(published);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET établissement restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantEtablissementResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<RestaurantEtablissementResponseDto>> CreateDraft(
            [FromBody] RestaurantCreateEtablissementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var created = await _etablissementService.CreateDraftAsync(request, idSociete, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.IdRestaurant }, created);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (RestaurantConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur POST établissement restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}")]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantEtablissementResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantEtablissementResponseDto>> Update(
            int id,
            [FromBody] RestaurantUpdateEtablissementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var updated = await _etablissementService.UpdateAsync(id, request, idSociete, cancellationToken);
                if (updated == null)
                    return NotFound(new { message = $"Établissement {id} introuvable." });
                return Ok(updated);
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
                _logger.LogError(ex, "Erreur PUT établissement restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/publish")]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantEtablissementResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantEtablissementResponseDto>> Publish(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var published = await _etablissementService.PublishAsync(id, idSociete, cancellationToken);
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
                _logger.LogError(ex, "Erreur publish établissement restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        private static bool TryParseOptionalStatus(
            string? status,
            out RestaurantStatus? parsed,
            out string? error)
        {
            parsed = null;
            error = null;
            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (!Enum.TryParse<RestaurantStatus>(status.Trim(), ignoreCase: true, out var value))
            {
                error = $"Statut invalide '{status}'. Valeurs : Draft, Published, Closed, Cancelled.";
                return false;
            }

            parsed = value;
            return true;
        }
    }
}
