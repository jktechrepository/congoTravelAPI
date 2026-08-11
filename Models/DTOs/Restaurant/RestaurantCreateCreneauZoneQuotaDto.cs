using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantCreateCreneauZoneQuotaDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdRestaurantZone { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int CapaciteTotale { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PrixUnitaire { get; set; }
    }
}
