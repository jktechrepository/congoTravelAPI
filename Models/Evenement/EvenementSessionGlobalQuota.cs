using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Evenement
{
    /// <summary>Quota global d'une session (mode GlobalQuota). PK partagée avec la session.</summary>
    public class EvenementSessionGlobalQuota
    {
        [Key]
        [ForeignKey(nameof(Session))]
        public int IdEvenementSession { get; set; }

        [Required]
        public int CapaciteTotale { get; set; }

        public int QuantiteHold { get; set; }

        public int QuantiteVendue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        [JsonIgnore]
        [ValidateNever]
        public EvenementSession? Session { get; set; }
    }
}
