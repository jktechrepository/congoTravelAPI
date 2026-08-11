namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantCreneauListItemDto
    {
        public int IdRestaurantCreneau { get; set; }
        public int IdSociete { get; set; }
        public string? NomSociete { get; set; }
        public int IdRestaurant { get; set; }
        public string? CodeRestaurant { get; set; }
        public string? NomRestaurant { get; set; }
        public int? IdSite { get; set; }
        public string? NomSite { get; set; }
        public DateOnly DateService { get; set; }
        public DateTime StartAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; }
        public string InventoryMode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CodeDevise { get; set; } = "CDF";
        public decimal? MontantAcompte { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
        public decimal? PrixUnitaire { get; set; }
    }
}
