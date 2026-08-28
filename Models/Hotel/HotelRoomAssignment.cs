using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    public class HotelRoomAssignment
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelRoomAssignment { get; set; }
        [Required]
        public int IdHotelReservation { get; set; }
        [Required]
        public int IdHotelReservationLine { get; set; }
        [Required]
        public int IdHotelRoom { get; set; }
        public DateTime DateAttributionUtc { get; set; } = DateTime.UtcNow;
        [JsonIgnore, ValidateNever]
        public HotelReservation? Reservation { get; set; }
        [JsonIgnore, ValidateNever]
        public HotelReservationLine? ReservationLine { get; set; }
        [JsonIgnore, ValidateNever]
        public HotelRoom? Room { get; set; }
    }

    public class HotelRoomAssignmentConflictException : InvalidOperationException
    {
        public HotelRoomAssignmentConflictException(string message) : base(message) { }
    }
}
