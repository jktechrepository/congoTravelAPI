using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueCreateClasseRequestDto
    {
        [MaxLength(50)]
        public string? Code { get; set; }

        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
