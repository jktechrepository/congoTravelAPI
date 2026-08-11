using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/restaurants/dashboard")]
    [Authorize]
    public class RestaurantDashboardController : ControllerBase
    {
        private readonly IRestaurantDashboardService _dashboardService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RestaurantDashboardController> _logger;

        public RestaurantDashboardController(
            IRestaurantDashboardService dashboardService,
            ICurrentUserService currentUserService,
            ILogger<RestaurantDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Dashboard réservation restaurant pour la société du JWT (ou société ciblée si Super-Admin).</summary>
        [HttpGet]
        [Permission("Restaurant.Dashboard.Read")]
        [ProducesResponseType(typeof(RestaurantDashboardResponseDto), 200)]
        public async Task<ActionResult<RestaurantDashboardResponseDto>> GetSocieteDashboard(
            [FromQuery] int? idSociete,
            [FromQuery] string? month,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var (monthStartUtc, monthEndUtc) = ResolveMonthRange(month);

                var result = await _dashboardService.GetSocieteDashboardAsync(
                    effectiveSocieteId,
                    monthStartUtc,
                    monthEndUtc,
                    cancellationToken);

                return Ok(result);
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
                _logger.LogError(ex, "Erreur dashboard restaurant société");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Vue agrégée multi-sociétés (Super-Admin uniquement).</summary>
        [HttpGet("super-admin")]
        [Permission("Restaurant.Dashboard.Read")]
        [ProducesResponseType(typeof(RestaurantSuperAdminDashboardResponseDto), 200)]
        public async Task<ActionResult<RestaurantSuperAdminDashboardResponseDto>> GetSuperAdminDashboard(
            [FromQuery] string? month,
            CancellationToken cancellationToken = default)
        {
            if (!_currentUserService.IsSuperAdmin)
                return Forbid();

            try
            {
                var (monthStartUtc, monthEndUtc) = ResolveMonthRange(month);
                var result = await _dashboardService.GetSuperAdminDashboardAsync(
                    monthStartUtc,
                    monthEndUtc,
                    cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dashboard restaurant super-admin");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Widget compact société (période mois) pour injection dans les dashboards transport.</summary>
        [HttpGet("widget")]
        [Permission("Restaurant.Dashboard.Read")]
        [ProducesResponseType(typeof(RestaurantDashboardWidgetDto), 200)]
        public async Task<ActionResult<RestaurantDashboardWidgetDto>> GetWidget(
            [FromQuery] int? idSociete,
            [FromQuery] string? month,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var (monthStartUtc, monthEndUtc) = ResolveMonthRange(month);

                var result = await _dashboardService.GetWidgetAsync(
                    effectiveSocieteId,
                    monthStartUtc,
                    monthEndUtc,
                    cancellationToken);

                return Ok(result);
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
                _logger.LogError(ex, "Erreur widget dashboard restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        private static (DateTime MonthStartUtc, DateTime MonthEndUtc) ResolveMonthRange(string? month)
        {
            DateTime referenceUtc;
            if (string.IsNullOrWhiteSpace(month))
            {
                referenceUtc = DateTime.UtcNow;
            }
            else if (!DateTime.TryParseExact(
                         month.Trim(),
                         "yyyy-MM",
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                         out referenceUtc))
            {
                throw new ArgumentException("Le paramètre month doit être au format yyyy-MM.");
            }

            var (_, monthStartUtc, _) = SocieteTransportMetricsHelper.GetUtcBoundaries(referenceUtc);
            return (monthStartUtc, monthStartUtc.AddMonths(1));
        }
    }
}
