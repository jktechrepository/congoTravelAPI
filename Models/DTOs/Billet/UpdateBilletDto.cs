using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class UpdateBilletDto
    {
        [Required]
        public int IdBillet { get; set; }

        [Required]
        public int IdReservation { get; set; }

        [Required]
        [StringLength(255)]
        public string QrCode { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateGeneration { get; set; }

        [Required]
        public int IdSociete { get; set; }
    }
}
