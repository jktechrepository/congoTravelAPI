using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Evenement
{
    public class EvenementSessionClassQuota
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEvenementSessionClassQuota { get; set; }

        [Required]
        public int IdEvenementSession { get; set; }

        [Required]
        public int IdEvenementClasse { get; set; }

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

        [JsonIgnore]
        [ValidateNever]
        public EvenementClasse? Classe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementReservationLine> ReservationLines { get; set; } = new List<EvenementReservationLine>();
    }
}
