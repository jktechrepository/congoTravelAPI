using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.SiteTouristique
{
    public class SiteTouristiqueTicket
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSiteTouristiqueTicket { get; set; }

        [Required]
        public int IdSiteTouristiqueReservationLine { get; set; }

        [Required]
        [MaxLength(100)]
        public string TicketCode { get; set; } = string.Empty;

        [Required]
        public SiteTouristiqueTicketStatus Status { get; set; } = SiteTouristiqueTicketStatus.ISSUED;

        [Required]
        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UsedAtUtc { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiqueReservationLine? ReservationLine { get; set; }
    }
}
