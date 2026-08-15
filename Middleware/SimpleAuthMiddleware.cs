using System.Security.Claims;
using CongoTravel.Services;

namespace CongoTravel.Middleware
{
    public class SimpleAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public SimpleAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Vérifier si c'est l'endpoint d'authentification
            if (context.Request.Path.StartsWithSegments("/api/Utilisateur/authentifier"))
            {
                await _next(context);
                return;
            }

            var token = ExtractTokenFromHeader(context);

            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("{\"success\":false,\"message\":\"Token JWT requis\",\"error\":\"UNAUTHORIZED\"}");
                return;
            }

            // Récupérer le service JWT depuis le conteneur de dépendances
            var jwtService = context.RequestServices.GetRequiredService<ISimpleJwtService>();
            
            if (!jwtService.ValidateToken(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("{\"success\":false,\"message\":\"Token JWT invalide\",\"error\":\"INVALID_TOKEN\"}");
                return;
            }

            // Extraire les informations du token et les ajouter au contexte
            var claims = ExtractClaimsFromToken(token);
            if (claims != null)
            {
                var identity = new ClaimsIdentity(claims, "jwt");
                context.User = new ClaimsPrincipal(identity);
            }

            await _next(context);
        }

        private string? ExtractTokenFromHeader(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
            return null;
        }

        private List<Claim>? ExtractClaimsFromToken(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3) return null;

                var payloadEncoded = parts[1];
                var payloadJson = DecodeBase64Url(payloadEncoded);
                var payload = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(payloadJson);

                var claims = new List<Claim>();

                if (payload.TryGetProperty("sub", out var sub))
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.GetString() ?? ""));

                if (payload.TryGetProperty("email", out var email))
                    claims.Add(new Claim(ClaimTypes.Email, email.GetString() ?? ""));

                if (payload.TryGetProperty("name", out var name))
                    claims.Add(new Claim(ClaimTypes.Name, name.GetString() ?? ""));

                if (payload.TryGetProperty("idSociete", out var idSociete))
                {
                    var idSocieteValue = idSociete.GetString();
                    // Ne créer le claim que si la valeur n'est pas vide
                    if (!string.IsNullOrWhiteSpace(idSocieteValue))
                    {
                        claims.Add(new Claim("IdSociete", idSocieteValue));
                    }
                }

                if (payload.TryGetProperty("idRole", out var idRole))
                    claims.Add(new Claim("IdRole", idRole.GetString() ?? ""));

                if (payload.TryGetProperty("role", out var role))
                    claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? ""));

                if (payload.TryGetProperty("societe", out var societe))
                    claims.Add(new Claim("Societe", societe.GetString() ?? ""));

                return claims;
            }
            catch
            {
                return null;
            }
        }

        private string DecodeBase64Url(string input)
        {
            var base64 = input
                .Replace('-', '+')
                .Replace('_', '/');
            
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            
            var bytes = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
