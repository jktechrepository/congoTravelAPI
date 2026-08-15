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

        [MaxLength(120)]
        public string? Province { get; set; }

        [MaxLength(120)]
        public string? Ville { get; set; }

        [MaxLength(500)]
        public string? Adresse { get; set; }

        [MaxLength(30)]
        public string? Telephone { get; set; }

        public TimeOnly? HeureOuverture { get; set; }

        public TimeOnly? HeureFermeture { get; set; }

        [MaxLength(100)]
        public string? JourOuverture { get; set; }

        [Range(1, int.MaxValue)]
        public int? IdSite { get; set; }
    }
}
