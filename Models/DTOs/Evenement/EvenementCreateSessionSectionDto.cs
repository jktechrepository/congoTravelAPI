using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Section du plan de salle avec ses sièges (mode <c>SeatNumbered</c>).</summary>
    public class EvenementCreateSessionSectionDto
    {
        [Required]
        [MaxLength(50)]
        public string CodeSection { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;

        public List<EvenementCreateSessionSeatDto> Seats { get; set; } = new();
    }
}
