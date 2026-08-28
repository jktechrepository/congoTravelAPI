using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    public class HotelReservation
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelReservation { get; set; }
        public int IdSociete { get; set; }
        public int IdHotel { get; set; }
        public int? IdSite { get; set; }
        public int? IdUtilisateur { get; set; }
        public int? IdClient { get; set; }
        [Required, MaxLength(64)]
        public string ReferenceReservation { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? CustomerRef { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NombreNuits { get; set; }
        public HotelReservationStatus Status { get; set; } = HotelReservationStatus.HOLD;
        public DateTime? ExpiresAtUtc { get; set; }
        public DateTime? CheckedInAtUtc { get; set; }
        public DateTime? CheckedOutAtUtc { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantSejour { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantSousTotal { get; set; }
        [Required, MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";
        [Required]
        public HotelInventoryMode InventoryMode { get; set; } = HotelInventoryMode.ClassQuota;
        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public DateTime? DateModification { get; set; }
        [JsonIgnore, ValidateNever]
        public Societe? Societe { get; set; }
        [JsonIgnore, ValidateNever]
        public Client? Client { get; set; }
        [JsonIgnore, ValidateNever]
        public Site? Site { get; set; }
        [JsonIgnore, ValidateNever]
        public Hotel? Hotel { get; set; }
        [JsonIgnore, ValidateNever]
        public ICollection<HotelReservationLine> Lines { get; set; } = new List<HotelReservationLine>();
        [JsonIgnore, ValidateNever]
        public ICollection<HotelPayment> Payments { get; set; } = new List<HotelPayment>();
        [JsonIgnore, ValidateNever]
        public ICollection<HotelRoomAssignment> RoomAssignments { get; set; } = new List<HotelRoomAssignment>();
        [JsonIgnore, ValidateNever]
        public ICollection<HotelReservationExtra> ReservationExtras { get; set; } = new List<HotelReservationExtra>();
    }

    public class HotelHoldConflictException : InvalidOperationException
    {
        public HotelHoldConflictException(string message) : base(message) { }
    }
}
