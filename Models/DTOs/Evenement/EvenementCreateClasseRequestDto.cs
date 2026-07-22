using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Evenement
{
    public class EvenementCreateClasseRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string CodeClasse { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
