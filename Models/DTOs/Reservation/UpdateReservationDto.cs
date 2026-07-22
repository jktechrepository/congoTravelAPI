using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class UpdateReservationDto
    {
        [Required]
        public int IdReservation { get; set; }

        [Required]
        public int IdUtilisateur { get; set; }

        [Required]
        public int IdClient { get; set; }

        [Required]
        public int IdVoyage { get; set; }

        [Required]
        [StringLength(20)]
        public string StatutReservation { get; set; }

        public bool Statut { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateReservation { get; set; }

        [Required]
        public int IdSociete { get; set; }

        /// <summary>Site (optionnel, même société).</summary>
        public int? IdSite { get; set; }
    }
}
