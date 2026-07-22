using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Notifications
{
    /// <summary>
    /// Repli lorsque les credentials Firebase sont absents — évite les erreurs DI au démarrage.
    /// </summary>
    public class NullFirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly ILogger<NullFirebaseNotificationService> _logger;

        public NullFirebaseNotificationService(ILogger<NullFirebaseNotificationService> logger)
        {
            _logger = logger;
        }

        public Task<bool> EnvoyerNotificationAUtilisateurAsync(
            int idUtilisateur, string titre, string corps, Dictionary<string, string>? donnees = null)
        {
            _logger.LogWarning("Push Firebase désactivé (NoOp) — utilisateur {IdUtilisateur}", idUtilisateur);
            return Task.FromResult(false);
        }

        public Task<int> EnvoyerNotificationParRoleAsync(
            int idRole, string titre, string corps, Dictionary<string, string>? donnees = null)
        {
            _logger.LogWarning("Push Firebase désactivé (NoOp) — rôle {IdRole}", idRole);
            return Task.FromResult(0);
        }

        public Task<int> EnvoyerNotificationParSocieteAsync(
            int idSociete, string titre, string corps, Dictionary<string, string>? donnees = null)
        {
            _logger.LogWarning("Push Firebase désactivé (NoOp) — société {IdSociete}", idSociete);
            return Task.FromResult(0);
        }

        public Task<int> EnvoyerNotificationParClasseAsync(
            int idClasse, string titre, string corps, Dictionary<string, string>? donnees = null)
        {
            _logger.LogWarning("Push Firebase désactivé (NoOp) — classe {IdClasse}", idClasse);
            return Task.FromResult(0);
        }

        public Task<bool> EnvoyerNotificationATokenAsync(
            string fcmToken, string titre, string corps, Dictionary<string, string>? donnees = null)
        {
            _logger.LogWarning("Push Firebase désactivé (NoOp) — token");
            return Task.FromResult(false);
        }

        public Task<bool> EnvoyerNotificationAvanceeAsync(
            string fcmToken, string titre, string corps, string? imageUrl = null,
            string? clickAction = null, Dictionary<string, string>? donnees = null,
            string? sound = null, string? badge = null)
        {
            _logger.LogWarning("Push Firebase désactivé (NoOp) — notification avancée");
            return Task.FromResult(false);
        }
    }
}
