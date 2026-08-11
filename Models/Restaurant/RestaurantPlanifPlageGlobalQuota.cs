using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Restaurant
{
    /// <summary>Quota global d'une plage de planification (mode GlobalQuota). PK partagée avec la plage.</summary>
    public class RestaurantPlanifPlageGlobalQuota
    {
        [Key]
        [ForeignKey(nameof(Plage))]
        public int IdRestaurantPlanificationPlage { get; set; }

        [Required]
        public int CapaciteTotale { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantPlanificationPlage? Plage { get; set; }
    }
}
