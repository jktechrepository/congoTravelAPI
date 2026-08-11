using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Restaurant
{
    /// <summary>Quota par zone d'une plage de planification (mode ClassQuota).</summary>
    public class RestaurantPlanifPlageZoneQuota
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantPlanifPlageZoneQuota { get; set; }

        [Required]
        public int IdRestaurantPlanificationPlage { get; set; }

        [Required]
        public int IdRestaurantZone { get; set; }

        [Required]
        public int CapaciteTotale { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantPlanificationPlage? Plage { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantZone? Zone { get; set; }
    }
}
