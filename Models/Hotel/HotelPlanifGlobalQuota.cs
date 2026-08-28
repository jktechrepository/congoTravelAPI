using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    /// <summary>Quota global du template de planification (mode GlobalQuota). PK partagée avec la planification.</summary>
    public class HotelPlanifGlobalQuota
    {
        [Key]
        [ForeignKey(nameof(Planification))]
        public int IdHotelPlanification { get; set; }

        [Required]
        public int CapaciteTotale { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixNuit { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public HotelPlanification? Planification { get; set; }
    }
}
