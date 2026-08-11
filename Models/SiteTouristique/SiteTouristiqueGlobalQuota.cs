using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.SiteTouristique
{
    /// <summary>Quota global d'une journée (mode GlobalQuota). PK partagée avec la journée.</summary>
    public class SiteTouristiqueGlobalQuota
    {
        [Key]
        [ForeignKey(nameof(Journee))]
        public int IdSiteTouristiqueJournee { get; set; }

        [Required]
        public int CapaciteTotale { get; set; }

        public int QuantiteHold { get; set; }

        public int QuantiteVendue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiqueJournee? Journee { get; set; }
    }
}
