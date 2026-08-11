using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantCreateZoneRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdRestaurant { get; set; }

        [MaxLength(64)]
        public string? Code { get; set; }

        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
