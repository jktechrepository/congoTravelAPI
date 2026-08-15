using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le service de gestion des notifications avancées (convocations, réunions, alertes)
    /// </summary>
    public interface INotificationService
    {
        // ═══════════════════════════════════════════════════════════════
        // 📨 CONVOCATIONS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Envoie une convocation à un parent (PUSH + SMS selon priorité)
        /// </summary>
        /// <param name="idUtilisateur">ID de l'utilisateur parent à convoquer</param>
        /// <param name="motif">Motif de la convocation</param>
        /// <param name="dateRdv">Date et heure du rendez-vous</param>
        /// <param name="lieu">Lieu du rendez-vous</param>
        /// <param name="contact">Contact pour informations</param>
        /// <param name="priorite">Priorité (BASSE, NORMALE, HAUTE)</param>
        /// <returns>True si au moins une notification a été envoyée</returns>
        Task<bool> EnvoyerConvocationAsync(
            int idUtilisateur,
            string motif,
            DateTime dateRdv,
            string lieu,
            string? contact = null,
            string priorite = "NORMALE");

        /// <summary>
        /// Envoie une convocation à plusieurs parents
        /// </summary>
        Task<int> EnvoyerConvocationEnMasseAsync(
            List<int> idsUtilisateurs,
            string motif,
            DateTime dateRdv,
            string lieu,
            string? contact = null,
            string priorite = "NORMALE");

        // ═══════════════════════════════════════════════════════════════
        // 🏫 RÉUNIONS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Envoie une invitation à une réunion (parents d'une classe, d'une école, etc.)
        /// </summary>
        /// <param name="titre">Titre de la réunion</param>
        /// <param name="description">Description et ordre du jour</param>
        /// <param name="dateReunion">Date et heure de la réunion</param>
        /// <param name="lieu">Lieu de la réunion</param>
        /// <param name="idSociete">ID de l'école (pour tous les parents de l'école)</param>
        /// <param name="idClasse">ID de la classe (pour tous les parents de la classe)</param>
        /// <param name="priorite">Priorité</param>
        /// <returns>Nombre de notifications envoyées</returns>
        Task<int> EnvoyerInvitationReunionAsync(
            string titre,
            string description,
            DateTime dateReunion,
            string lieu,
            int? idSociete = null,
            int? idClasse = null,
            string priorite = "NORMALE");

        /// <summary>
        /// Envoie un rappel de réunion
        /// </summary>
        Task<int> EnvoyerRappelReunionAsync(
            string titre,
            DateTime dateReunion,
            string lieu,
            int? idSociete = null,
            int? idClasse = null);

        // ═══════════════════════════════════════════════════════════════
        // 🚨 ALERTES URGENTES
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Envoie une alerte urgente (PUSH + SMS systématique pour tous)
        /// </summary>
        /// <param name="titre">Titre de l'alerte</param>
        /// <param name="message">Message détaillé</param>
        /// <param name="idSociete">ID de l'école (si null, tous)</param>
        /// <param name="idClasse">ID de la classe (si null, tous parents école)</param>
        /// <returns>Nombre de notifications envoyées</returns>
        Task<int> EnvoyerAlerteUrgenteAsync(
            string titre,
            string message,
            int? idSociete = null,
            int? idClasse = null);

        // ═══════════════════════════════════════════════════════════════
        // 📢 COMMUNICATION ADMINISTRATIVE
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Envoie une communication administrative à un ou plusieurs parents
        /// </summary>
        /// <param name="titre">Titre de la communication</param>
        /// <param name="message">Message</param>
        /// <param name="idsUtilisateurs">Liste des IDs utilisateurs destinataires</param>
        /// <param name="priorite">Priorité</param>
        /// <returns>Nombre de notifications envoyées</returns>
        Task<int> EnvoyerCommunicationAdminAsync(
            string titre,
            string message,
            List<int> idsUtilisateurs,
            string priorite = "NORMALE");
    }
}

