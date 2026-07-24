using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    public class AppleAuthService : IAppleAuthService
    {
        private readonly IAppleTokenValidator _tokenValidator;
        private readonly ExternalAuthAccountService _accounts;
        private readonly AuthentificationResponseBuilder _responseBuilder;

        public AppleAuthService(
            IAppleTokenValidator tokenValidator,
            ExternalAuthAccountService accounts,
            AuthentificationResponseBuilder responseBuilder)
        {
            _tokenValidator = tokenValidator;
            _accounts = accounts;
            _responseBuilder = responseBuilder;
        }

        public async Task<AuthentificationResponse> SignInWithAppleAsync(
            string idToken,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            var identity = await _tokenValidator.ValidateAsync(idToken, cancellationToken);

            // Apple : email souvent omis après la 1ʳᵉ connexion — ResolveUser gère sub d'abord.
            // Pour create/link, exiger email vérifié quand un email est fourni.
            var requireVerified = !string.IsNullOrWhiteSpace(identity.Email);
            var utilisateur = await _accounts.ResolveUserAsync(
                AuthProviders.Apple,
                identity,
                requireEmailVerifiedForCreateOrLink: requireVerified,
                cancellationToken);

            return await _responseBuilder.BuildAsync(utilisateur, deviceInfo, ipAddress, cancellationToken);
        }
    }
}
