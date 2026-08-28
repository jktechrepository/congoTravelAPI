using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    public class HotelExtra
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelExtra { get; set; }
        [Required]
        public int IdSociete { get; set; }
        [Required]
        public int IdHotel { get; set; }
        [Required, MaxLength(64)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }
        [Required, MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";
        [Required]
        public HotelExtraPricingUnit PricingUnit { get; set; } = HotelExtraPricingUnit.PerStay;
        public bool IsActif { get; set; } = true;
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public DateTime? DateModification { get; set; }
        [JsonIgnore, ValidateNever]
        public Societe? Societe { get; set; }
        [JsonIgnore, ValidateNever]
        public Hotel? Hotel { get; set; }
        [JsonIgnore, ValidateNever]
        public ICollection<HotelReservationExtra> ReservationExtras { get; set; } = new List<HotelReservationExtra>();
    }

    public class HotelExtraConflictException : InvalidOperationException
    {
        public HotelExtraConflictException(string message) : base(message) { }
    }
}
