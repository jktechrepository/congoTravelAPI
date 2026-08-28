using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    /// <summary>Snapshot ClassQuota d'un type de chambre sur un template de planification.</summary>
    public class HotelPlanificationLigne
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelPlanificationLigne { get; set; }

        [Required]
        public int IdHotelPlanification { get; set; }

        [Required]
        public int IdHotelRoomType { get; set; }

        [Required]
        public int CapaciteTotale { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixNuit { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public HotelPlanification? Planification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public HotelRoomType? RoomType { get; set; }
    }
}
