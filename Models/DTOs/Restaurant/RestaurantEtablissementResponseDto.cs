namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantEtablissementResponseDto
    {
        public int IdRestaurant { get; set; }
        public int IdSociete { get; set; }
        public string? NomSociete { get; set; }
        public int? IdSite { get; set; }
        public string? NomSite { get; set; }
        public string CodeRestaurant { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Adresse { get; set; }
        public decimal AcomptePourcentDefaut { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
        public int CreneauxCount { get; set; }
    }
}
