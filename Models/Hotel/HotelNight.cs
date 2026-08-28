using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    /// <summary>Pool GlobalQuota d'une nuit × hôtel (sans type de chambre).</summary>
    public class HotelNight
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelNight { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdHotel { get; set; }

        [Column(TypeName = "date")]
        public DateTime NightDate { get; set; }

        public int CapaciteTotale { get; set; }

        public int QuantiteHold { get; set; }

        public int QuantiteVendue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixNuit { get; set; }

        [Required, MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        [Required]
        public HotelStatus Status { get; set; } = HotelStatus.Draft;

        public int? IdHotelPlanification { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore, ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore, ValidateNever]
        public Hotel? Hotel { get; set; }

        [JsonIgnore, ValidateNever]
        public HotelPlanification? Planification { get; set; }
    }

    public class HotelNightConflictException : Exception
    {
        public HotelNightConflictException(string message) : base(message) { }
    }
}
