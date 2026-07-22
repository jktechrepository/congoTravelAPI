using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>Template récurrent pour génération batch de voyages.</summary>
    public class PlanificationVoyage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPlanificationVoyage { get; set; }

        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdSite { get; set; }

        [Required]
        public int IdVehicule { get; set; }

        [Required]
        public TimeSpan HeureDepart { get; set; }

        [Required]
        public int Prix { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevisePrix { get; set; } = "CDF";

        /// <summary>Jours de la semaine (.NET DayOfWeek: 0=Dimanche … 6=Samedi), stockés en JSON.</summary>
        [Required]
        public List<int> JoursSemaine { get; set; } = new();

        public bool Statut { get; set; } = true;

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Site? Site { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Vehicule? Vehicule { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<PlanificationVoyageEtape>? Etapes { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<PlanificationVoyageTarif>? Tarifs { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<Voyage>? VoyagesGeneres { get; set; }
    }
}
