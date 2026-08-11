using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.SiteTouristique
{
    /// <summary>Template récurrent pour génération batch de journées site touristique.</summary>
    public class SiteTouristiquePlanification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSiteTouristiquePlanification { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdSiteTouristique { get; set; }

        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        /// <summary>Jours de la semaine (.NET DayOfWeek: 0=Dimanche … 6=Samedi), stockés en JSON.</summary>
        [Required]
        public List<int> JoursSemaine { get; set; } = new();

        [Required]
        public SiteTouristiqueInventoryMode InventoryMode { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        /// <summary>Heures avant minuit UTC de DateVisite pour ouvrir les ventes.</summary>
        public int? SalesOpenOffsetHours { get; set; }

        /// <summary>Heures avant fin de jour UTC (minuit+24h) pour fermer les ventes.</summary>
        public int? SalesCloseOffsetHours { get; set; }

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
        public SiteTouristiqueLieu? Lieu { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiquePlanifGlobalQuota? GlobalQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiquePlanifClassQuota> ClassQuotas { get; set; } = new List<SiteTouristiquePlanifClassQuota>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiqueJournee> JourneesGenerees { get; set; } = new List<SiteTouristiqueJournee>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiquePlanifGenerationLog> GenerationLogs { get; set; } = new List<SiteTouristiquePlanifGenerationLog>();
    }
}
