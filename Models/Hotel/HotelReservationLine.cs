using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    public class HotelReservationLine
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelReservationLine { get; set; }
        public int IdHotelReservation { get; set; }

        [Required]
        public HotelReservationLineType LineType { get; set; } = HotelReservationLineType.ClassQuota;

        public int? IdHotelRoomType { get; set; }

        /// <summary>Référence audit optionnelle vers une nuit globale (mode GlobalQuota).</summary>
        public int? IdHotelNight { get; set; }

        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixSejourUnitaire { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantLigne { get; set; }
        [Required, MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";
        [JsonIgnore, ValidateNever]
        public HotelReservation? Reservation { get; set; }
        [JsonIgnore, ValidateNever]
        public HotelRoomType? RoomType { get; set; }
        [JsonIgnore, ValidateNever]
        public HotelNight? Night { get; set; }
        [JsonIgnore, ValidateNever]
        public ICollection<HotelRoomAssignment> RoomAssignments { get; set; } = new List<HotelRoomAssignment>();
    }
}
