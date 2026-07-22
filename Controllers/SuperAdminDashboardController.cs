using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SuperAdminDashboardController : ControllerBase
    {
        private readonly SuperAdminDashboardService _superAdminDashboardService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SuperAdminDashboardController> _logger;

        public SuperAdminDashboardController(
            SuperAdminDashboardService superAdminDashboardService,
            ICurrentUserService currentUserService,
            ILogger<SuperAdminDashboardController> logger)
        {
            _superAdminDashboardService = superAdminDashboardService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Dashboard global multi-sociétés (Super-Admin uniquement).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(SuperAdminDashboardTransportDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SuperAdminDashboardTransportDto>> GetSuperAdminDashboard(
            [FromQuery] PagedRequest? reservationsRequest,
            CancellationToken cancellationToken = default)
        {
            if (!_currentUserService.IsSuperAdmin)
            {
                _logger.LogWarning(
                    "Accès SuperAdminDashboard refusé pour l'utilisateur {UserId}",
                    _currentUserService.UserId);
                return Forbid();
            }

            try
            {
                _logger.LogInformation("Génération du dashboard SuperAdmin transport");
                var dashboard = await _superAdminDashboardService.GetDashboardDataAsync(
                    reservationsRequest,
                    cancellationToken);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard SuperAdmin");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
    }
}
