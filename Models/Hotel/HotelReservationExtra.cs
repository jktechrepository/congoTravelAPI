using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    public class HotelReservationExtra
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelReservationExtra { get; set; }
        [Required]
        public int IdHotelReservation { get; set; }
        [Required]
        public int IdHotelExtra { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaireSnapshot { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantLigne { get; set; }
        [Required, MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";
        [JsonIgnore, ValidateNever]
        public HotelReservation? Reservation { get; set; }
        [JsonIgnore, ValidateNever]
        public HotelExtra? Extra { get; set; }
    }
}
