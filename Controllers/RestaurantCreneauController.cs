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
    [Route("api/restaurants/creneaux")]
    [Authorize]
    public class RestaurantCreneauController : ControllerBase
    {
        private readonly IRestaurantCreneauService _creneauService;
        private readonly IRestaurantAvailabilityService _availabilityService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RestaurantCreneauController> _logger;

        public RestaurantCreneauController(
            IRestaurantCreneauService creneauService,
            IRestaurantAvailabilityService availabilityService,
            ICurrentUserService currentUserService,
            ILogger<RestaurantCreneauController> logger)
        {
            _creneauService = creneauService;
            _availabilityService = availabilityService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<RestaurantCreneauListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<RestaurantCreneauListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] int? idRestaurant,
            [FromQuery] string? date,
            [FromQuery] string? status,
            [FromQuery] string? inventoryMode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!TryParseOptionalInventoryMode(inventoryMode, out var parsedMode, out var modeError))
                    return BadRequest(new { message = modeError });

                DateOnly? dateService = null;
                if (!string.IsNullOrWhiteSpace(date))
                {
                    if (!DateOnly.TryParse(date, out var parsedDate))
                        return BadRequest(new { message = "Format de date invalide (attendu yyyy-MM-dd)." });
                    dateService = parsedDate;
                }

                var isStaffTenant = RestaurantTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (!isStaffTenant || !effectiveSocieteId.HasValue)
                {
                    var published = await _creneauService.ListPublishedGlobalAsync(
                        new RestaurantCreneauListFilter
                        {
                            InventoryMode = parsedMode,
                            IdSociete = idSociete is > 0 ? idSociete : null,
                            IdRestaurant = idRestaurant is > 0 ? idRestaurant : null,
                            DateService = dateService
                        },
                        cancellationToken);
                    return Ok(published);
                }

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var creneaux = await _creneauService.ListAsync(
                    effectiveSocieteId.Value,
                    new RestaurantCreneauListFilter
                    {
                        Status = parsedStatus,
                        InventoryMode = parsedMode,
                        IdRestaurant = idRestaurant is > 0 ? idRestaurant : null,
                        DateService = dateService
                    },
                    cancellationToken);
                return Ok(creneaux);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste créneaux restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RestaurantCreneauResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantCreneauResponseDto>> GetById(
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
                    var creneau = await _creneauService.GetByIdAsync(id, effectiveSocieteId.Value, cancellationToken);
                    if (creneau == null)
                        return NotFound(new { message = $"Créneau {id} introuvable." });
                    return Ok(creneau);
                }

                var published = await _creneauService.GetPublishedByIdAsync(id, cancellationToken);
                if (published == null)
                    return NotFound(new { message = $"Créneau Published {id} introuvable." });
                return Ok(published);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET créneau restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}/availability")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RestaurantAvailabilityResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantAvailabilityResponseDto>> GetAvailability(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var creneau = await _creneauService.GetPublishedByIdAsync(id, cancellationToken);
                if (creneau == null)
                {
                    if (RestaurantTenancyGuard.TryResolveStaffTenantForCatalogList(
                            _currentUserService,
                            null,
                            out var staffSociete)
                        && staffSociete.HasValue)
                    {
                        creneau = await _creneauService.GetByIdAsync(id, staffSociete.Value, cancellationToken);
                    }
                }

                if (creneau == null)
                    return NotFound(new { message = $"Créneau {id} introuvable." });

                var availability = await _availabilityService.GetAvailabilityAsync(
                    id,
                    creneau.IdSociete,
                    cancellationToken);

                if (availability == null)
                    return NotFound(new { message = $"Disponibilités créneau {id} introuvables." });

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
            catch (NotSupportedException ex)
            {
                return StatusCode(StatusCodes.Status501NotImplemented, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET availability créneau restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantCreneauResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<RestaurantCreneauResponseDto>> CreateDraft(
            [FromBody] RestaurantCreateCreneauRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var created = await _creneauService.CreateDraftAsync(
                    request, idSociete, cancellationToken: cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.IdRestaurantCreneau }, created);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (RestaurantCreneauConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur POST créneau restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/publish")]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantCreneauResponseDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<RestaurantCreneauResponseDto>> Publish(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var published = await _creneauService.PublishAsync(id, idSociete, cancellationToken);
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
            catch (RestaurantCreneauConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur publish créneau restaurant {Id}", id);
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

        private static bool TryParseOptionalInventoryMode(
            string? inventoryMode,
            out RestaurantInventoryMode? parsed,
            out string? error)
        {
            parsed = null;
            error = null;
            if (string.IsNullOrWhiteSpace(inventoryMode))
                return true;

            if (!Enum.TryParse<RestaurantInventoryMode>(inventoryMode.Trim(), ignoreCase: true, out var value))
            {
                error = $"InventoryMode invalide '{inventoryMode}'. Valeurs : GlobalQuota, ClassQuota.";
                return false;
            }

            parsed = value;
            return true;
        }
    }
}
