using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Evenement
{
    public class EvenementUpdateClasseRequestDto
    {
        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool Statut { get; set; } = true;
    }
}
