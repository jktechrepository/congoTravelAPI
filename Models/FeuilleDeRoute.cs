using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Manifeste d'embarquement historisé pour un voyage à une date donnée.
    /// Les champs société / voyage sont figés au moment de la génération.
    /// </summary>
    public class FeuilleDeRoute
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdFeuilleDeRoute { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdVoyage { get; set; }

        /// <summary>Date métier d'embarquement (= jour de départ du voyage).</summary>
        [Required]
        [Column(TypeName = "date")]
        public DateTime DateEmbarquement { get; set; }

        [Required]
        public DateTime DateGenerationUtc { get; set; }

        public int? IdUtilisateurGeneration { get; set; }

        // --- Snapshot société ---
        [MaxLength(150)]
        public string? SocieteNom { get; set; }

        [MaxLength(50)]
        public string? SocieteTelephone { get; set; }

        [MaxLength(256)]
        public string? SocieteEmail { get; set; }

        [MaxLength(500)]
        public string? SocieteAdresse { get; set; }

        public string? SocieteLogo { get; set; }

        // --- Snapshot voyage ---
        [Required]
        public DateTime VoyageDateDepart { get; set; }

        [Required]
        public TimeSpan VoyageHeureDepart { get; set; }

        [Required]
        public int VoyagePrix { get; set; }

        [Required]
        [MaxLength(3)]
        public string VoyageCodeDevise { get; set; } = "CDF";

        [Required]
        public int IdDestination { get; set; }

        [MaxLength(450)]
        public string? DestinationLibelle { get; set; }

        [Required]
        public int IdVehicule { get; set; }

        [MaxLength(20)]
        public string? VehiculeImmatriculation { get; set; }

        [MaxLength(100)]
        public string? VehiculeAlias { get; set; }

        public int? IdSite { get; set; }

        [MaxLength(200)]
        public string? SiteNom { get; set; }

        [Required]
        public int NombrePassagers { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Voyage? Voyage { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Utilisateur? UtilisateurGeneration { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<FeuilleDeRoutePassager> Passagers { get; set; } = new List<FeuilleDeRoutePassager>();
    }
}
