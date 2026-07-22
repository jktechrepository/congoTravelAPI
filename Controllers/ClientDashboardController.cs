using CongoTravel.Models.DTOs;
using CongoTravel.Models.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientDashboardController : ControllerBase
    {
        private readonly ClientDashboardService _clientDashboardService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ClientDashboardController> _logger;

        public ClientDashboardController(
            ClientDashboardService clientDashboardService,
            ICurrentUserService currentUserService,
            ILogger<ClientDashboardController> logger)
        {
            _clientDashboardService = clientDashboardService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard Client transport (données du client JWT). Accès : Client ou Super-Admin.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ClientDashboardDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ClientDashboardDto>> GetClientDashboard(
            CancellationToken cancellationToken = default)
        {
            if (_currentUserService.UserRole != UserRoles.CLIENT && !_currentUserService.IsSuperAdmin)
            {
                _logger.LogWarning(
                    "Accès ClientDashboard refusé pour l'utilisateur {UserId} (rôle {Role})",
                    _currentUserService.UserId,
                    _currentUserService.UserRole);
                return Forbid();
            }

            try
            {
                _logger.LogInformation("Génération du dashboard Client transport");
                var dashboard = await _clientDashboardService.GetDashboardDataAsync(cancellationToken);
                return Ok(dashboard);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Accès non autorisé au dashboard Client");
                return StatusCode(403, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard Client");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
    }
}
