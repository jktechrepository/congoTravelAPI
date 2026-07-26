using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    public interface IEmailVerificationService
    {
        /// <summary>
        /// Émet un token, envoie le lien de vérification. Retourne false si email synthétique / déjà vérifié / échec SMTP.
        /// </summary>
        Task<bool> IssueAndSendAsync(Utilisateur utilisateur, CancellationToken cancellationToken = default);

        Task<(bool Success, int StatusCode, string Message)> VerifyAsync(
            string rawToken,
            CancellationToken cancellationToken = default);

        Task<(bool Success, int StatusCode, string Message)> ResendAsync(
            string email,
            CancellationToken cancellationToken = default);
    }
}
