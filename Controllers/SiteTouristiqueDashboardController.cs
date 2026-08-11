using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/sites-touristiques/dashboard")]
    [Authorize]
    public class SiteTouristiqueDashboardController : ControllerBase
    {
        private readonly ISiteTouristiqueDashboardService _dashboardService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SiteTouristiqueDashboardController> _logger;

        public SiteTouristiqueDashboardController(
            ISiteTouristiqueDashboardService dashboardService,
            ICurrentUserService currentUserService,
            ILogger<SiteTouristiqueDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Dashboard billetterie site touristique pour la société du JWT (ou société ciblée si Super-Admin).</summary>
        [HttpGet]
        [Permission("SiteTouristique.Dashboard.Read")]
        [ProducesResponseType(typeof(SiteTouristiqueDashboardResponseDto), 200)]
        public async Task<ActionResult<SiteTouristiqueDashboardResponseDto>> GetSocieteDashboard(
            [FromQuery] int? idSociete,
            [FromQuery] string? month,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
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
                _logger.LogError(ex, "Erreur dashboard site touristique société");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Vue agrégée multi-sociétés (Super-Admin uniquement).</summary>
        [HttpGet("super-admin")]
        [Permission("SiteTouristique.Dashboard.Read")]
        [ProducesResponseType(typeof(SiteTouristiqueSuperAdminDashboardResponseDto), 200)]
        public async Task<ActionResult<SiteTouristiqueSuperAdminDashboardResponseDto>> GetSuperAdminDashboard(
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
                _logger.LogError(ex, "Erreur dashboard site touristique super-admin");
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
