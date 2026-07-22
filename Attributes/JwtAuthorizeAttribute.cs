using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace CongoTravel.Attributes
{
    public class JwtAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Vérifier si l'action ou le contrôleur a l'attribut [AllowAnonymous]
            var allowAnonymous = context.ActionDescriptor.EndpointMetadata
                .OfType<AllowAnonymousAttribute>()
                .Any();

            if (allowAnonymous)
            {
                return;
            }

            // Vérifier si l'utilisateur est authentifié
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Token JWT requis pour accéder à cette ressource",
                    error = "UNAUTHORIZED"
                });
                return;
            }

            // Vérifier si l'utilisateur a un ID valide
            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "Token JWT invalide - ID utilisateur manquant",
                    error = "INVALID_TOKEN"
                });
                return;
            }
        }
    }
}
