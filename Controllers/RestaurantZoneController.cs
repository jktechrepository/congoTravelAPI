using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/restaurants/zones")]
    [Authorize]
    public class RestaurantZoneController : ControllerBase
    {
        private readonly IRestaurantZoneService _zoneService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RestaurantZoneController> _logger;

        public RestaurantZoneController(
            IRestaurantZoneService zoneService,
            ICurrentUserService currentUserService,
            ILogger<RestaurantZoneController> logger)
        {
            _zoneService = zoneService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<RestaurantZoneResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<RestaurantZoneResponseDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] int? idRestaurant,
            [FromQuery] bool actifsSeulement = false,
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
                    var published = await _zoneService.ListPublishedGlobalAsync(
                        idSociete is > 0 ? idSociete : null,
                        idRestaurant is > 0 ? idRestaurant : null,
                        actifsSeulement,
                        cancellationToken);
                    return Ok(published);
                }

                var zones = await _zoneService.ListAsync(
                    effectiveSocieteId.Value,
                    idRestaurant,
                    actifsSeulement,
                    cancellationToken);
                return Ok(zones);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste zones restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RestaurantZoneResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantZoneResponseDto>> GetById(
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
                    var zone = await _zoneService.GetByIdAsync(id, effectiveSocieteId.Value, cancellationToken);
                    if (zone == null)
                        return NotFound(new { message = $"Zone restaurant {id} introuvable." });
                    return Ok(zone);
                }

                var published = await _zoneService.GetPublishedByIdAsync(id, cancellationToken);
                if (published == null)
                    return NotFound(new { message = $"Zone restaurant Published {id} introuvable." });

                return Ok(published);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET zone restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("Restaurant.Zone.Write")]
        [ProducesResponseType(typeof(RestaurantZoneResponseDto), 201)]
        public async Task<ActionResult<RestaurantZoneResponseDto>> Create(
            [FromBody] RestaurantCreateZoneRequestDto request)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var created = await _zoneService.CreateAsync(request, idSociete);
                return CreatedAtAction(nameof(GetById), new { id = created.IdRestaurantZone }, created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (RestaurantZoneConflictException ex)
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
                _logger.LogError(ex, "Erreur POST zone restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}")]
        [Permission("Restaurant.Zone.Write")]
        [ProducesResponseType(typeof(RestaurantZoneResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantZoneResponseDto>> Update(
            int id,
            [FromBody] RestaurantUpdateZoneRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var updated = await _zoneService.UpdateAsync(id, request, idSociete, cancellationToken);
                if (updated == null)
                    return NotFound(new { message = $"Zone restaurant {id} introuvable." });

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
                _logger.LogError(ex, "Erreur PUT zone restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/toggle-statut")]
        [Permission("Restaurant.Zone.Write")]
        [ProducesResponseType(typeof(RestaurantZoneResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantZoneResponseDto>> ToggleStatut(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var updated = await _zoneService.ToggleStatutAsync(id, idSociete, cancellationToken);
                if (updated == null)
                    return NotFound(new { message = $"Zone restaurant {id} introuvable." });

                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur toggle-statut zone restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }
    }
}
