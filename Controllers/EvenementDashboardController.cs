using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/events/dashboard")]
    [Authorize]
    public class EvenementDashboardController : ControllerBase
    {
        private readonly IEvenementDashboardService _dashboardService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<EvenementDashboardController> _logger;

        public EvenementDashboardController(
            IEvenementDashboardService dashboardService,
            ICurrentUserService currentUserService,
            ILogger<EvenementDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Dashboard billetterie événement pour la société du JWT (ou société ciblée si Super-Admin).</summary>
        [HttpGet]
        [Permission("Evenement.Dashboard.Read")]
        [ProducesResponseType(typeof(EvenementDashboardResponseDto), 200)]
        public async Task<ActionResult<EvenementDashboardResponseDto>> GetSocieteDashboard(
            [FromQuery] int? idSociete,
            [FromQuery] string? month,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(
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
                _logger.LogError(ex, "Erreur dashboard événement société");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Vue agrégée multi-sociétés (Super-Admin uniquement).</summary>
        [HttpGet("super-admin")]
        [Permission("Evenement.Dashboard.Read")]
        [ProducesResponseType(typeof(EvenementSuperAdminDashboardResponseDto), 200)]
        public async Task<ActionResult<EvenementSuperAdminDashboardResponseDto>> GetSuperAdminDashboard(
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
                _logger.LogError(ex, "Erreur dashboard événement super-admin");
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
