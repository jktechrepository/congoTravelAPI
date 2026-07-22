namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le service d'envoi d'emails
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Envoie un email de bienvenue avec les identifiants de connexion
        /// </summary>
        Task<bool> SendWelcomeEmailAsync(string email, string nomComplet, string defaultUsername, string telephone, string motDePasseParDefaut, string role, string nomSociete, string genre = "Masculin", string fonction = null, string matricule = null, string nomEnfant = null, string classeEnfant = null, string matriculeEnfant = null);

        /// <summary>
        /// Envoie un email de réinitialisation de mot de passe
        /// </summary>
        Task<bool> SendPasswordResetEmailAsync(string email, string nomComplet, string resetToken);

        /// <summary>
        /// Envoie un email de confirmation de changement de mot de passe
        /// </summary>
        Task<bool> SendPasswordChangedConfirmationEmailAsync(string email, string nomComplet, DateTime dateChangement, string adresseIP = null);

        /// <summary>
        /// Envoie un email générique
        /// </summary>
        Task<bool> SendGenericEmailAsync(string toEmail, string toName, string subject, string plainTextBody, string htmlBody);
    }
}
