using CongoTravel.Models.DTOs.Authentification;

namespace CongoTravel.Services.Repositories
{
    public interface IAppleTokenValidator
    {
        Task<ExternalAuthIdentity> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
    }
}
