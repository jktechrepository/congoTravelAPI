using Microsoft.Extensions.Primitives;

namespace CongoTravel.Middleware
{
    /// <summary>
    /// Middleware qui ajoute automatiquement le préfixe "Bearer" aux tokens JWT si absent
    /// </summary>
    public class AutoBearerMiddleware
    {
        private readonly RequestDelegate _next;

        public AutoBearerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Vérifier si c'est une requête authentifiée sans le préfixe "Bearer"
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                
                // Si le header commence par "Bearer " ou est vide, ne rien faire
                if (!string.IsNullOrWhiteSpace(authHeader) && 
                    !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    // Ajouter le préfixe "Bearer" automatiquement
                    var newAuthHeader = $"Bearer {authHeader}";
                    context.Request.Headers["Authorization"] = new StringValues(newAuthHeader);
                }
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method pour ajouter le middleware au pipeline
    /// </summary>
    public static class AutoBearerMiddlewareExtensions
    {
        public static IApplicationBuilder UseAutoBearer(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AutoBearerMiddleware>();
        }
    }
}
