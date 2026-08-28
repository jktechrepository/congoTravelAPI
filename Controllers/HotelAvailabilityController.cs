using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController, Route("api/hotels/availability"), Authorize]
    public class HotelAvailabilityController : ControllerBase
    {
        private readonly IHotelAvailabilityService _availabilityService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<HotelAvailabilityController> _logger;

        public HotelAvailabilityController(
            IHotelAvailabilityService availabilityService,
            ICurrentUserService currentUser,
            ILogger<HotelAvailabilityController> logger)
        {
            _availabilityService = availabilityService;
            _currentUser = currentUser;
            _logger = logger;
        }

        /// <summary>
        /// Disponibilité Published sur [from, to). Public/Client : Published only.
        /// Staff tenanté : même filtre Published (inventaire vendable) ; idSociete JWT si staff.
        /// </summary>
        [HttpGet, AllowAnonymous]
        [ProducesResponseType(typeof(HotelAvailabilityResponseDto), 200)]
        public async Task<ActionResult<HotelAvailabilityResponseDto>> Get(
            [FromQuery] int idHotel,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int? roomTypeId = null,
            [FromQuery] int? idSociete = null,
            [FromQuery] HotelInventoryMode? inventoryMode = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                int? tenantFilter = null;
                var staff = HotelTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUser, idSociete, out var tenant);
                if (staff && tenant.HasValue)
                    tenantFilter = tenant.Value;
                else if (idSociete is > 0)
                    tenantFilter = idSociete;

                var result = await _availabilityService.GetAvailabilityAsync(
                    idHotel,
                    from,
                    to,
                    roomTypeId,
                    tenantFilter,
                    publishedOnly: true,
                    inventoryMode,
                    cancellationToken);
                return Ok(result);
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
                _logger.LogError(ex, "Erreur GET availability hôtel {IdHotel}", idHotel);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }
    }
}
