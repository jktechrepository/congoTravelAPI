using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using CongoTravel.Models.DTOs.Authentification;
using CongoTravel.Models.Options;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    public class AppleTokenValidator : IAppleTokenValidator
    {
        private const string AppleIssuer = "https://appleid.apple.com";
        private const string AppleOidcConfig = "https://appleid.apple.com/.well-known/openid-configuration";

        private readonly AppleAuthOptions _options;
        private readonly ILogger<AppleTokenValidator> _logger;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

        public AppleTokenValidator(IOptions<AppleAuthOptions> options, ILogger<AppleTokenValidator> logger)
        {
            _options = options.Value;
            _logger = logger;
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                AppleOidcConfig,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = true });
        }

        public async Task<ExternalAuthIdentity> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ExternalAuthException(401, "ID token Apple manquant.");

            var clientIds = (_options.ClientIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (clientIds.Count == 0)
            {
                _logger.LogError("AppleAuth:ClientIds est vide — impossible de valider les identity tokens.");
                throw new ExternalAuthException(500, "Authentification Apple non configurée.");
            }

            try
            {
                var oidc = await _configurationManager.GetConfigurationAsync(cancellationToken);
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = AppleIssuer,
                    ValidateAudience = true,
                    ValidAudiences = clientIds,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = oidc.SigningKeys,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };

                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(idToken, parameters, out _);

                var sub = ClaimValue(principal, "sub") ?? ClaimValue(principal, ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(sub))
                    throw new ExternalAuthException(401, "ID token Apple invalide (sub manquant).");

                var email = ClaimValue(principal, "email") ?? ClaimValue(principal, ClaimTypes.Email);
                var emailVerifiedClaim = ClaimValue(principal, "email_verified");
                var emailVerified = ParseEmailVerified(emailVerifiedClaim, email);

                return new ExternalAuthIdentity
                {
                    Sub = sub,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                    EmailVerified = emailVerified,
                    Name = ClaimValue(principal, "name")
                };
            }
            catch (ExternalAuthException)
            {
                throw;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "ID token Apple rejeté");
                throw new ExternalAuthException(401, "ID token Apple invalide ou expiré.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec validation ID token Apple");
                throw new ExternalAuthException(401, "ID token Apple invalide ou expiré.");
            }
        }

        private static string? ClaimValue(ClaimsPrincipal principal, string type) =>
            principal.FindFirst(type)?.Value;

        private static bool ParseEmailVerified(string? claim, string? email)
        {
            if (string.Equals(claim, "true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (bool.TryParse(claim, out var parsed) && parsed)
                return true;
            // Apple fournit en général un email déjà vérifié ; claim parfois omis.
            return !string.IsNullOrWhiteSpace(email) && string.IsNullOrEmpty(claim);
        }
    }
}
