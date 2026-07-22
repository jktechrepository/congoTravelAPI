using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    /// <summary>
    /// Controller pour tester l'authentification JWT
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthTestController : ControllerBase
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AuthTestController> _logger;

        public AuthTestController(ICurrentUserService currentUserService, ILogger<AuthTestController> logger)
        {
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint public pour tester si l'API fonctionne
        /// </summary>
        [HttpGet("public")]
        public IActionResult PublicEndpoint()
        {
            return Ok(new { message = "API fonctionne correctement", timestamp = DateTime.Now });
        }

        /// <summary>
        /// Endpoint protégé pour tester l'authentification JWT
        /// </summary>
        [HttpGet("protected")]
        [Authorize]
        public IActionResult ProtectedEndpoint()
        {
            try
            {
                var userInfo = new
                {
                    IsAuthenticated = _currentUserService.IsAuthenticated,
                    UserId = _currentUserService.UserId,
                    UserName = _currentUserService.UserName,
                    UserRole = _currentUserService.UserRole,
                    SocieteId = _currentUserService.SocieteId,
                    IsSuperAdmin = _currentUserService.IsSuperAdmin,
                    IsAdmin = _currentUserService.IsAdmin,
                    Timestamp = DateTime.Now
                };

                _logger.LogInformation("Test authentification réussi pour l'utilisateur {UserId}", _currentUserService.UserId);

                return Ok(new 
                { 
                    message = "Authentification JWT réussie !", 
                    user = userInfo,
                    note = "Le middleware AutoBearer a ajouté automatiquement le préfixe Bearer si nécessaire"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test d'authentification");
                return StatusCode(500, new { message = "Erreur lors du test d'authentification" });
            }
        }

        /// <summary>
        /// Endpoint pour vérifier les permissions de l'utilisateur
        /// </summary>
        [HttpGet("permissions")]
        [Authorize]
        public IActionResult CheckPermissions()
        {
            var permissions = new
            {
                IsAuthenticated = _currentUserService.IsAuthenticated,
                UserId = _currentUserService.UserId,
                UserRole = _currentUserService.UserRole,
                SocieteId = _currentUserService.SocieteId,
                IsSuperAdmin = _currentUserService.IsSuperAdmin,
                IsAdmin = _currentUserService.IsAdmin,
                IsStaff = _currentUserService.IsStaff,
                HasFinanceAccess = _currentUserService.HasFinanceAccess,
                HasPedagogieAccess = _currentUserService.HasPedagogieAccess,
                AgentId = _currentUserService.AgentId,
                ClientId = _currentUserService.TuteurId
            };

            return Ok(new 
            { 
                message = "Permissions de l'utilisateur", 
                permissions = permissions 
            });
        }
    }
}
