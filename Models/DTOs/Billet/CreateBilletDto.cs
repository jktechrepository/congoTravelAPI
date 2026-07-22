using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class CreateBilletDto
    {
        [Required]
        public int IdReservation { get; set; }

        [Required]
        [StringLength(255)]
        public string QrCode { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateGeneration { get; set; }

        [Required]
        public int IdSociete { get; set; }
    }
}
