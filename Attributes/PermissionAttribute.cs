using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace CongoTravel.Attributes
{
    /// <summary>
    /// Attribut d'autorisation personnalisé basé sur les permissions RBAC
    /// Utilisation: [Permission("Societe.Create")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class PermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string _permission;

        /// <summary>
        /// Initialise un nouvel attribut de permission
        /// </summary>
        /// <param name="permission">Nom de la permission requise (ex: "Societe.Create", "Paiement.Delete")</param>
        public PermissionAttribute(string permission)
        {
            _permission = permission ?? throw new ArgumentNullException(nameof(permission));
        }

        /// <summary>
        /// Méthode appelée par ASP.NET Core pour vérifier l'autorisation
        /// </summary>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Vérifier si l'utilisateur est authentifié
            var user = context.HttpContext.User;
            
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Récupérer l'ID de l'utilisateur
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Récupérer le service de permissions
            var permissionService = context.HttpContext.RequestServices
                .GetRequiredService<IPermissionService>();

            // Vérifier si l'utilisateur a la permission
            var hasPermission = permissionService.UserHasPermissionAsync(userId, _permission)
                .GetAwaiter()
                .GetResult();

            if (!hasPermission)
            {
                // Log de l'accès refusé
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<PermissionAttribute>>();

                var userRole = user.FindFirst(ClaimTypes.Role)?.Value ?? "Inconnu";
                logger.LogWarning($"❌ Accès refusé: Utilisateur {userId} ({userRole}) n'a pas la permission '{_permission}' pour {context.HttpContext.Request.Method} {context.HttpContext.Request.Path}");

                context.Result = new ForbidResult();
            }
        }
    }
}

