using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Attribution d’un siège à un passager pour un voyage donné (anti doubles réservations concurrentes via contrainte DB).
    /// </summary>
    public class VoyageSeatAllocation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdVoyageSeatAllocation { get; set; }

        /// <summary>
        /// Référence Voyage.Id (PK du voyage).
        /// </summary>
        [Required]
        public int IdVoyage { get; set; }

        [Required]
        public int IdSiege { get; set; }

        [Required]
        public int IdReservationPassenger { get; set; }

        [Required]
        [StringLength(20)]
        public string Statut { get; set; } = "CONFIRME";

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Voyage? Voyage { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Siege? Siege { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ReservationPassenger? ReservationPassenger { get; set; }
    }
}
