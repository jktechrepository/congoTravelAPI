using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Models.Evenement
{
    public class EvenementTicket
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEvenementTicket { get; set; }

        [Required]
        public int IdEvenementReservationLine { get; set; }

        [Required]
        [MaxLength(100)]
        public string TicketCode { get; set; } = string.Empty;

        [Required]
        public EvenementTicketStatus Status { get; set; } = EvenementTicketStatus.ISSUED;

        [Required]
        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UsedAtUtc { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public EvenementReservationLine? ReservationLine { get; set; }
    }
}
