using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    public class Hotel
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotel { get; set; }
        [Required]
        public int IdSociete { get; set; }
        public int? IdSite { get; set; }
        [Required, MaxLength(64)]
        public string CodeHotel { get; set; } = string.Empty;
        [Required, MaxLength(255)]
        public string Nom { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? Description { get; set; }
        [MaxLength(500)]
        public string? Adresse { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal AcomptePourcentDefaut { get; set; }
        [Required]
        public HotelStatus Status { get; set; } = HotelStatus.Draft;
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public DateTime? DateModification { get; set; }
        [JsonIgnore, ValidateNever]
        public Societe? Societe { get; set; }
        [JsonIgnore, ValidateNever]
        public Site? Site { get; set; }
        [JsonIgnore, ValidateNever]
        public ICollection<HotelRoomType> RoomTypes { get; set; } = new List<HotelRoomType>();
        [JsonIgnore, ValidateNever]
        public ICollection<HotelRoom> Rooms { get; set; } = new List<HotelRoom>();
        [JsonIgnore, ValidateNever]
        public ICollection<HotelExtra> Extras { get; set; } = new List<HotelExtra>();
        [JsonIgnore, ValidateNever]
        public ICollection<HotelNightAllotment> NightAllotments { get; set; } = new List<HotelNightAllotment>();
        [JsonIgnore, ValidateNever]
        public ICollection<HotelNight> Nights { get; set; } = new List<HotelNight>();
        [JsonIgnore, ValidateNever]
        public ICollection<HotelPhoto> Photos { get; set; } = new List<HotelPhoto>();
        [JsonIgnore, ValidateNever]
        public ICollection<HotelReservation> Reservations { get; set; } = new List<HotelReservation>();

        [JsonIgnore, ValidateNever]
        public ICollection<HotelPlanification> Planifications { get; set; } = new List<HotelPlanification>();
    }
}
