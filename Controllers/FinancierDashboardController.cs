using CongoTravel.Models.DTOs;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FinancierDashboardController : ControllerBase
    {
        private readonly FinancierDashboardService _financierDashboardService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<FinancierDashboardController> _logger;

        public FinancierDashboardController(
            FinancierDashboardService financierDashboardService,
            ICurrentUserService currentUserService,
            ILogger<FinancierDashboardController> logger)
        {
            _financierDashboardService = financierDashboardService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard finance transport (une requête). SuperAdmin : toutes sociétés ; Financier/Gérant : société du JWT.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(FinancierDashboardDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<FinancierDashboardDto>> GetFinancierDashboard(
            CancellationToken cancellationToken = default)
        {
            if (!_currentUserService.HasFinanceAccess)
            {
                _logger.LogWarning(
                    "Accès FinancierDashboard refusé pour l'utilisateur {UserId}",
                    _currentUserService.UserId);
                return Forbid();
            }

            try
            {
                _logger.LogInformation("Génération du dashboard Financier transport");
                var dashboard = await _financierDashboardService.GetDashboardDataAsync(cancellationToken);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard Financier");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
    }
}
