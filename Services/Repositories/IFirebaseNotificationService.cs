namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le service de notifications Firebase Cloud Messaging
    /// </summary>
    public interface IFirebaseNotificationService
    {
        /// <summary>
        /// Envoie une notification à un utilisateur spécifique
        /// </summary>
        Task<bool> EnvoyerNotificationAUtilisateurAsync(int idUtilisateur, string titre, string corps, Dictionary<string, string>? donnees = null);

        /// <summary>
        /// Envoie une notification à tous les utilisateurs ayant un rôle spécifique
        /// </summary>
        Task<int> EnvoyerNotificationParRoleAsync(int idRole, string titre, string corps, Dictionary<string, string>? donnees = null);

        /// <summary>
        /// Envoie une notification à tous les utilisateurs d'une école
        /// </summary>
        Task<int> EnvoyerNotificationParSocieteAsync(int idSociete, string titre, string corps, Dictionary<string, string>? donnees = null);

        /// <summary>
        /// Envoie une notification à tous les utilisateurs d'une classe
        /// </summary>
        Task<int> EnvoyerNotificationParClasseAsync(int idClasse, string titre, string corps, Dictionary<string, string>? donnees = null);

        /// <summary>
        /// Envoie une notification à un token FCM spécifique
        /// </summary>
        Task<bool> EnvoyerNotificationATokenAsync(string fcmToken, string titre, string corps, Dictionary<string, string>? donnees = null);

        /// <summary>
        /// Envoie une notification push personnalisée avec des paramètres avancés
        /// </summary>
        Task<bool> EnvoyerNotificationAvanceeAsync(string fcmToken, string titre, string corps, string? imageUrl = null, string? clickAction = null, Dictionary<string, string>? donnees = null, string? sound = null, string? badge = null);
    }
}
