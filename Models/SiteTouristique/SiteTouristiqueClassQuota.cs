using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.SiteTouristique
{
    public class SiteTouristiqueClassQuota
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSiteTouristiqueClassQuota { get; set; }

        [Required]
        public int IdSiteTouristiqueJournee { get; set; }

        [Required]
        public int IdSiteTouristiqueClasse { get; set; }

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

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiqueClasse? Classe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiqueReservationLine> ReservationLines { get; set; } = new List<SiteTouristiqueReservationLine>();
    }
}
