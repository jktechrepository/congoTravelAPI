using CongoTravel.Models;
using CongoTravel.Models.DTOs.Authentification;

namespace CongoTravel.Services.Repositories
{
    public interface IGoogleAuthService
    {
        /// <summary>
        /// Connexion / inscription Google. Retourne le même contrat que <c>POST /authentifier</c>.
        /// </summary>
        Task<AuthentificationResponse> SignInWithGoogleAsync(
            string idToken,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default);
    }
}
