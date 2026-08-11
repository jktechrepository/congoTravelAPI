using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueCreateLieuRequestDto
    {
        [Required]
        [MaxLength(64)]
        public string CodeLieu { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdSite { get; set; }
    }
}
