using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueUpdateLieuRequestDto
    {
        [Required]
        [MaxLength(255)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(1, int.MaxValue)]
        public int? IdSite { get; set; }
    }
}
