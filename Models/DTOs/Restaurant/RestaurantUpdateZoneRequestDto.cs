using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantUpdateZoneRequestDto
    {
        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool Actif { get; set; } = true;
    }
}
