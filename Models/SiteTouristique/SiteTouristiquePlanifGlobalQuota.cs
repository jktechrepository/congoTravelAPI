using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.SiteTouristique
{
    /// <summary>Quota global du template de planification (mode GlobalQuota). PK partagée avec la planification.</summary>
    public class SiteTouristiquePlanifGlobalQuota
    {
        [Key]
        [ForeignKey(nameof(Planification))]
        public int IdSiteTouristiquePlanification { get; set; }

        [Required]
        public int CapaciteTotale { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiquePlanification? Planification { get; set; }
    }
}
