using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// 📱 Interface pour le service de notifications SMS via Twilio
    /// Définit le contrat que toute implémentation SMS doit respecter
    /// </summary>
    public interface ISmsNotificationService
    {
        // ═══════════════════════════════════════════════════════════════
        // 📤 ENVOI DE SMS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Envoyer un SMS à un numéro de téléphone
        /// </summary>
        /// <param name="numeroTelephone">Numéro destinataire (format: +243999123456)</param>
        /// <param name="message">Contenu du message</param>
        /// <param name="typeNotification">Type de notification (ex: PRESENCE_ELEVE, PAIEMENT)</param>
        /// <returns>SmsLog avec MessageSid et statut</returns>
        Task<SmsLog?> EnvoyerSmsAsync(string numeroTelephone, string message, string? typeNotification = null);

        /// <summary>
        /// Envoyer un SMS à un utilisateur (cherche automatiquement son numéro)
        /// </summary>
        /// <param name="idUtilisateur">ID de l'utilisateur</param>
        /// <param name="message">Contenu du message</param>
        /// <param name="typeNotification">Type de notification</param>
        /// <returns>SmsLog ou null si utilisateur sans numéro</returns>
        Task<SmsLog?> EnvoyerSmsAUtilisateurAsync(int idUtilisateur, string message, string? typeNotification = null);

        /// <summary>
        /// Envoyer un SMS en masse à plusieurs numéros
        /// </summary>
        /// <param name="numerosDestination">Liste des numéros destinataires</param>
        /// <param name="message">Contenu du message</param>
        /// <param name="typeNotification">Type de notification</param>
        /// <returns>Liste des SmsLog pour chaque envoi</returns>
        Task<List<SmsLog>> EnvoyerSmsEnMasseAsync(List<string> numerosDestination, string message, string? typeNotification = null);

        /// <summary>
        /// Envoyer un SMS à tous les utilisateurs d'un rôle spécifique
        /// </summary>
        /// <param name="role">Rôle cible (TUTEUR, ENSEIGNANT, ADMINISTRATEUR)</param>
        /// <param name="message">Contenu du message</param>
        /// <param name="typeNotification">Type de notification</param>
        /// <returns>Liste des SmsLog</returns>
        Task<List<SmsLog>> EnvoyerSmsParRoleAsync(string role, string message, string? typeNotification = null);

        /// <summary>
        /// Envoyer un SMS à tous les tuteurs d'une école
        /// </summary>
        /// <param name="idSociete">ID de l'école</param>
        /// <param name="message">Contenu du message</param>
        /// <param name="typeNotification">Type de notification</param>
        /// <returns>Liste des SmsLog</returns>
        Task<List<SmsLog>> EnvoyerSmsParSocieteAsync(int idSociete, string message, string? typeNotification = null);

        // ═══════════════════════════════════════════════════════════════
        // 🔍 VÉRIFICATION ET TRACKING
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Vérifier le statut d'un SMS auprès de Twilio
        /// </summary>
        /// <param name="messageSid">MessageSid Twilio</param>
        /// <returns>SmsLog mis à jour avec le statut actuel</returns>
        Task<SmsLog?> VerifierStatutSmsAsync(string messageSid);

        /// <summary>
        /// Mettre à jour le statut de tous les SMS en attente
        /// (Tâche de fond à exécuter périodiquement)
        /// </summary>
        /// <returns>Nombre de SMS mis à jour</returns>
        Task<int> MettreAJourStatutsSmsEnAttenteAsync();

        // ═══════════════════════════════════════════════════════════════
        // 📊 HISTORIQUE ET RAPPORTS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Récupérer l'historique des SMS avec pagination
        /// </summary>
        /// <param name="request">Paramètres de pagination</param>
        /// <param name="statut">Filtre par statut (optionnel)</param>
        /// <param name="typeNotification">Filtre par type (optionnel)</param>
        /// <param name="idUtilisateur">Filtre par utilisateur (optionnel)</param>
        /// <param name="dateDebut">Date de début (optionnel)</param>
        /// <param name="dateFin">Date de fin (optionnel)</param>
        /// <returns>Historique paginé des SMS</returns>
        Task<PagedResult<SmsLog>> GetHistoriqueSmsAsync(
            PagedRequest request,
            string? statut = null,
            string? typeNotification = null,
            int? idUtilisateur = null,
            DateTime? dateDebut = null,
            DateTime? dateFin = null);

        /// <summary>
        /// Obtenir un rapport des coûts SMS par période
        /// </summary>
        /// <param name="dateDebut">Date de début</param>
        /// <param name="dateFin">Date de fin</param>
        /// <returns>Rapport avec coût total, nombre de SMS, répartition par type</returns>
        Task<object> GetRapportCoutsSmsAsync(DateTime dateDebut, DateTime dateFin);

        /// <summary>
        /// Obtenir des statistiques globales sur les SMS
        /// </summary>
        /// <returns>Statistiques (total envoyés, délivrés, échoués, coûts)</returns>
        Task<object> GetStatistiquesSmsAsync();

        // ═══════════════════════════════════════════════════════════════
        // 🔧 UTILITAIRES
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Valider un numéro de téléphone (format international)
        /// </summary>
        /// <param name="numeroTelephone">Numéro à valider</param>
        /// <returns>True si valide</returns>
        bool ValiderNumeroTelephone(string numeroTelephone);

        /// <summary>
        /// Formater un numéro de téléphone au format international
        /// </summary>
        /// <param name="numeroTelephone">Numéro à formater</param>
        /// <returns>Numéro formaté (+243...)</returns>
        string FormaterNumeroTelephone(string numeroTelephone);

        /// <summary>
        /// Calculer le nombre de segments d'un message SMS
        /// (1 segment = 160 caractères, message long = plusieurs segments)
        /// </summary>
        /// <param name="message">Message à analyser</param>
        /// <returns>Nombre de segments</returns>
        int CalculerNombreSegments(string message);
    }
}

