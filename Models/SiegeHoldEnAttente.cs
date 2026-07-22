using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongoTravel.Models
{
    /// <summary>
    /// Verrou temporaire d'un siège pendant l'attente de confirmation FlexPay (sans réservation officielle).
    /// </summary>
    public class SiegeHoldEnAttente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSiegeHoldEnAttente { get; set; }

        [Required]
        public int IdVoyage { get; set; }

        [Required]
        public int IdSiege { get; set; }

        [Required]
        public Guid IdCommandeReservationEnAttente { get; set; }

        [Required]
        public DateTime ExpireAt { get; set; }

        [Required]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    }
}
