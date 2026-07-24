using CongoTravel.Models.DTOs.Authentification;

namespace CongoTravel.Services.Repositories
{
    public interface IGoogleTokenValidator
    {
        /// <summary>Valide l'ID token Google et retourne l'identité ; lève si invalide.</summary>
        Task<GoogleIdentity> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
    }
}
