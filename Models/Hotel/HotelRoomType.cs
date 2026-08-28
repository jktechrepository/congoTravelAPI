using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    public class HotelRoomType
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelRoomType { get; set; }
        [Required]
        public int IdSociete { get; set; }
        [Required]
        public int IdHotel { get; set; }
        [Required, MaxLength(64)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }
        public int? CapacitePersonnesMax { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrixNuitReference { get; set; }
        [MaxLength(3)]
        public string? CodeDevise { get; set; }
        [Required]
        public HotelStatus Status { get; set; } = HotelStatus.Draft;
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public DateTime? DateModification { get; set; }
        [JsonIgnore, ValidateNever]
        public Societe? Societe { get; set; }
        [JsonIgnore, ValidateNever]
        public Hotel? Hotel { get; set; }
        [JsonIgnore, ValidateNever]
        public ICollection<HotelNightAllotment> NightAllotments { get; set; } = new List<HotelNightAllotment>();
        [JsonIgnore, ValidateNever]
        public ICollection<HotelRoom> Rooms { get; set; } = new List<HotelRoom>();
        [JsonIgnore, ValidateNever]
        public ICollection<HotelReservationLine> ReservationLines { get; set; } = new List<HotelReservationLine>();
    }
}
