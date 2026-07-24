using CongoTravel.Models;
using CongoTravel.Models.DTOs.Authentification;

namespace CongoTravel.Services.Repositories
{
    public interface IAppleAuthService
    {
        Task<AuthentificationResponse> SignInWithAppleAsync(
            string idToken,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default);
    }
}
