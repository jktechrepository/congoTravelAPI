using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.Restaurant
{
    public class RestaurantTicket
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantTicket { get; set; }

        [Required]
        public int IdRestaurantReservationLine { get; set; }

        [Required]
        [MaxLength(100)]
        public string TicketCode { get; set; } = string.Empty;

        [Required]
        public RestaurantTicketStatus Status { get; set; } = RestaurantTicketStatus.ISSUED;

        [Required]
        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UsedAtUtc { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantReservationLine? ReservationLine { get; set; }
    }
}
