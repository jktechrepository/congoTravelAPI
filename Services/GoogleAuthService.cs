using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Authentification;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IGoogleTokenValidator _tokenValidator;
        private readonly ExternalAuthAccountService _accounts;
        private readonly AuthentificationResponseBuilder _responseBuilder;

        public GoogleAuthService(
            IGoogleTokenValidator tokenValidator,
            ExternalAuthAccountService accounts,
            AuthentificationResponseBuilder responseBuilder)
        {
            _tokenValidator = tokenValidator;
            _accounts = accounts;
            _responseBuilder = responseBuilder;
        }

        public async Task<AuthentificationResponse> SignInWithGoogleAsync(
            string idToken,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            var google = await _tokenValidator.ValidateAsync(idToken, cancellationToken);
            var identity = new ExternalAuthIdentity
            {
                Sub = google.Sub,
                Email = google.Email,
                EmailVerified = google.EmailVerified,
                Name = google.Name,
                Picture = google.Picture
            };

            var utilisateur = await _accounts.ResolveUserAsync(
                AuthProviders.Google,
                identity,
                requireEmailVerifiedForCreateOrLink: true,
                cancellationToken);

            return await _responseBuilder.BuildAsync(utilisateur, deviceInfo, ipAddress, cancellationToken);
        }
    }
}
