namespace CongoTravel.Models.DTOs.Authentification
{
    /// <summary>
    /// DTO simplifié pour les informations du client dans la réponse d'authentification
    /// </summary>
    public class ClientInfoDto
    {
        public int IdClient { get; set; }
        public string NomClient { get; set; } = string.Empty;
        public string? CodeCons { get; set; }
        public string? Telephone { get; set; }
        public string? EmailClient { get; set; }
        public string? GenreClient { get; set; }
        public string? AdresseClient { get; set; }
        public bool Statut { get; set; }
        public bool IsActif { get; set; }
        public int? IdAxe { get; set; }

        /// <summary>
        /// ✨ NOUVEAU : Liste des usages du client (récupérés depuis ClientUsage)
        /// </summary>
        public List<AuthentificationUsageInfoDto> Usages { get; set; } = new List<AuthentificationUsageInfoDto>();
    }
}
