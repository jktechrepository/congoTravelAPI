namespace CongoTravel.Models.DTOs.Authentification
{
    /// <summary>
    /// DTO simplifié pour les informations d'un usage dans la réponse d'authentification
    /// </summary>
    public class AuthentificationUsageInfoDto
    {
        /// <summary>
        /// Identifiant de l'usage
        /// </summary>
        public int IdUsage { get; set; }

        /// <summary>
        /// Libellé de l'usage (ex: "DOMESTIQUE", "COMMERCIAL")
        /// </summary>
        public string Libelle { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de bâtiments pour cet usage
        /// </summary>
        public int NombreBatiment { get; set; }

        /// <summary>
        /// Date d'attribution de l'usage au client
        /// </summary>
        public DateTime DateAttribution { get; set; }

        /// <summary>
        /// Statut de la relation Client-Usage (true = actif)
        /// </summary>
        public bool Statut { get; set; }
    }
}
