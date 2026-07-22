using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Models.Evenement
{
    public class EvenementReservationLine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEvenementReservationLine { get; set; }

        [Required]
        public int IdEvenementReservation { get; set; }

        [Required]
        public EvenementReservationLineType LineType { get; set; }

        [Required]
        public int Quantite { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        public int? IdEvenementSessionSeat { get; set; }

        public int? IdEvenementSessionClassQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public EvenementReservation? Reservation { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public EvenementSessionSeat? SessionSeat { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public EvenementSessionClassQuota? SessionClassQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementTicket> Tickets { get; set; } = new List<EvenementTicket>();
    }
}
