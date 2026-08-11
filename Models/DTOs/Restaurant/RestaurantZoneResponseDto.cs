namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantZoneResponseDto
    {
        public int IdRestaurantZone { get; set; }
        public int IdSociete { get; set; }
        public int IdRestaurant { get; set; }
        public string? Code { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Actif { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }
}
