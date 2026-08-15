namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueLieuListItemDto
    {
        public int IdSiteTouristique { get; set; }
        public int IdSociete { get; set; }
        public string? NomSociete { get; set; }
        public int? IdSite { get; set; }
        public string? NomSite { get; set; }
        public string CodeLieu { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Province { get; set; }
        public string? Ville { get; set; }
        public string? Adresse { get; set; }
        public string? Telephone { get; set; }
        public TimeOnly? HeureOuverture { get; set; }
        public TimeOnly? HeureFermeture { get; set; }
        public string? JourOuverture { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }

        public SiteTouristiqueLieuPhotoDto? PhotoCouverture { get; set; }
    }
}
