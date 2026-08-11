namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueClasseResponseDto
    {
        public int IdSiteTouristiqueClasse { get; set; }
        public int IdSociete { get; set; }
        public string? Code { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Actif { get; set; }
        public DateTime DateCreation { get; set; }
    }
}
