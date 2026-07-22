namespace CongoTravel.Models.DTOs.Authentification
{
    /// <summary>
    /// DTO simplifié pour les informations de l'agent dans la réponse d'authentification
    /// </summary>
    public class AgentInfoDto
    {
        public int IdAgent { get; set; }
        public string? Matricule { get; set; }
        public string? NomComplet { get; set; }
        public string? Genre { get; set; }
        public DateTime? DateNaissance { get; set; }
        public string? TelephoneAgent { get; set; }
        public string? EmailAgent { get; set; }
        public bool? Statut { get; set; }
        public string? Fonction { get; set; }
        public string? RoleAgent { get; set; }
        public string? PhotoUrl { get; set; }
        public int? IdSociete { get; set; }
        public int? IdSite { get; set; }
        public string? AdresseResidence { get; set; }
        public string? Zone { get; set; }
    }
}
