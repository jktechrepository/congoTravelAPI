using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using CongoTravel.Models.DTOs.Authentification;
using CongoTravel.Models.Options;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly GoogleAuthOptions _options;
        private readonly ILogger<GoogleTokenValidator> _logger;

        public GoogleTokenValidator(IOptions<GoogleAuthOptions> options, ILogger<GoogleTokenValidator> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<GoogleIdentity> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new GoogleAuthException(401, "ID token Google manquant.");

            var clientIds = (_options.ClientIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (clientIds.Count == 0)
            {
                _logger.LogError("GoogleAuth:ClientIds est vide — impossible de valider les ID tokens.");
                throw new GoogleAuthException(500, "Authentification Google non configurée.");
            }

            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = clientIds
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                if (string.IsNullOrWhiteSpace(payload.Subject))
                    throw new GoogleAuthException(401, "ID token Google invalide (sub manquant).");

                return new GoogleIdentity
                {
                    Sub = payload.Subject,
                    Email = payload.Email?.Trim() ?? string.Empty,
                    EmailVerified = payload.EmailVerified,
                    Name = payload.Name,
                    Picture = payload.Picture
                };
            }
            catch (GoogleAuthException)
            {
                throw;
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "ID token Google rejeté");
                throw new GoogleAuthException(401, "ID token Google invalide ou expiré.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec validation ID token Google");
                throw new GoogleAuthException(401, "ID token Google invalide ou expiré.");
            }
        }
    }
}
