using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    public class Voyage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("date_depart")]
        public DateTime DateDepart { get; set; }

        [Required]
        [Column("heure_depart")]
        public TimeSpan HeureDepart { get; set; }

        [Required]
        [Column("prix")]
        public int Prix { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevisePrix { get; set; } = "CDF";

        [Required]
        [MaxLength(3)]
        public string CodeDevisePrincipale { get; set; } = "CDF";

        [Column(TypeName = "decimal(18,8)")]
        public decimal TauxVersDevisePrincipale { get; set; } = 1m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixDevisePrincipale { get; set; }

        [Required]
        [Column("IdVehicule")]
        public int IdVehicule { get; set; }

        [Required]
        [Column("IdDestination")]
        public int IdDestination { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Column("IdSite")]
        public int? IdSite { get; set; }

        /// <summary>Planification source si le voyage a été généré automatiquement.</summary>
        public int? IdPlanificationVoyage { get; set; }

        public bool? Statut { get; set; } = true;

        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        public Vehicule? Vehicule { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Destination? Destination { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Site? Site { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<VoyageDestination>? VoyageDestinations { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<VoyageTarifCategorieSiege>? VoyageTarifsCategorieSiege { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public PlanificationVoyage? PlanificationVoyage { get; set; }
    }
}
