using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    /// <summary>Allotment d'une nuit × type de chambre (capacité inventaire ClassQuota).</summary>
    public class HotelNightAllotment
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelNightAllotment { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdHotel { get; set; }

        [Required]
        public int IdHotelRoomType { get; set; }

        /// <summary>Date calendaire de la nuit (timezone société — pas un Instant).</summary>
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

        /// <summary>Template de planification ayant généré cet allotment (nullable).</summary>
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
        public HotelRoomType? RoomType { get; set; }

        [JsonIgnore, ValidateNever]
        public HotelPlanification? Planification { get; set; }
    }
}
