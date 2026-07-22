using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class CreateReservationDto
    {
        [Required]
        public int IdUtilisateur { get; set; }

        [Required]
        public int IdClient { get; set; }

        [Required]
        public int IdVoyage { get; set; }

        [Required]
        [StringLength(20)]
        public string StatutReservation { get; set; } = "EN_ATTENTE";

        public bool Statut { get; set; } = true;

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateReservation { get; set; }

        [Required]
        public int IdSociete { get; set; }

        /// <summary>Site (optionnel, même société).</summary>
        public int? IdSite { get; set; }
    }
}
