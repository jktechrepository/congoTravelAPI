using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.SiteTouristique
{
    public class SiteTouristiquePlanifClassQuota
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSiteTouristiquePlanifClassQuota { get; set; }

        [Required]
        public int IdSiteTouristiquePlanification { get; set; }

        [Required]
        public int IdSiteTouristiqueClasse { get; set; }

        [Required]
        public int CapaciteTotale { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiquePlanification? Planification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiqueClasse? Classe { get; set; }
    }
}
