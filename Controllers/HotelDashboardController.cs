using System.Globalization;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController, Route("api/hotels/dashboard"), Authorize]
    public class HotelDashboardController : ControllerBase
    {
        private readonly IHotelDashboardService _dashboard;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<HotelDashboardController> _logger;

        public HotelDashboardController(
            IHotelDashboardService dashboard,
            ICurrentUserService currentUser,
            ILogger<HotelDashboardController> logger)
        {
            _dashboard = dashboard;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet, Permission("Hotel.Dashboard.Read")]
        public async Task<ActionResult<HotelDashboardResponseDto>> Get(
            [FromQuery] int? idSociete,
            [FromQuery] string? month,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var company = HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser, idSociete);
                var (start, end) = ResolveMonthRange(month);
                return Ok(await _dashboard.GetSocieteDashboardAsync(
                    company, start, end, cancellationToken));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dashboard hôtel société");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("super-admin"), Permission("Hotel.Dashboard.Read")]
        public async Task<ActionResult<HotelSuperAdminDashboardResponseDto>> GetSuperAdmin(
            [FromQuery] string? month,
            CancellationToken cancellationToken = default)
        {
            if (!_currentUser.IsSuperAdmin)
                return Forbid();
            try
            {
                var (start, end) = ResolveMonthRange(month);
                return Ok(await _dashboard.GetSuperAdminDashboardAsync(start, end, cancellationToken));
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dashboard hôtel super-admin");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("widget"), Permission("Hotel.Dashboard.Read")]
        public async Task<ActionResult<HotelDashboardWidgetDto>> GetWidget(
            [FromQuery] int? idSociete,
            [FromQuery] string? month,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var company = HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser, idSociete);
                var (start, end) = ResolveMonthRange(month);
                return Ok(await _dashboard.GetWidgetAsync(company, start, end, cancellationToken));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur widget dashboard hôtel");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        private static (DateTime Start, DateTime End) ResolveMonthRange(string? month)
        {
            DateTime reference;
            if (string.IsNullOrWhiteSpace(month))
                reference = DateTime.UtcNow;
            else if (!DateTime.TryParseExact(
                month.Trim(), "yyyy-MM", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out reference))
                throw new ArgumentException("Le paramètre month doit être au format yyyy-MM.");

            var (_, start, _) = SocieteTransportMetricsHelper.GetUtcBoundaries(reference);
            return (start, start.AddMonths(1));
        }
    }
}
