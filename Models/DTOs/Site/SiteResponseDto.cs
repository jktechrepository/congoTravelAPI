namespace CongoTravel.Models.DTOs.Site
{
    public class SiteResponseDto
    {
        public int IdSite { get; set; }
        public int IdSociete { get; set; }
        public string CodeSite { get; set; } = string.Empty;
        public string NomSite { get; set; } = string.Empty;
        public string? Ville { get; set; }
        public string? Adresse { get; set; }
        public string? Telephone { get; set; }
        public string? NumeroMobileMoney { get; set; }
        public string NomResponsableSite { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Genre { get; set; } = "Masculin";
        public bool Statut { get; set; }
        public bool IsSitePrincipal { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }
}
