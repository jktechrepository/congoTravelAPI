using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.Restaurant
{
    /// <summary>Template récurrent pour génération batch de créneaux restaurant (multi-plages).</summary>
    public class RestaurantPlanification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantPlanification { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdRestaurant { get; set; }

        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        /// <summary>Jours de la semaine (.NET DayOfWeek: 0=Dimanche … 6=Samedi), stockés en JSON.</summary>
        [Required]
        public List<int> JoursSemaine { get; set; } = new();

        [Required]
        public RestaurantInventoryMode InventoryMode { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        /// <summary>Montant d'acompte fixe optionnel recopié sur chaque créneau généré.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontantAcompte { get; set; }

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
        public Restaurant? Restaurant { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantPlanificationPlage> Plages { get; set; } = new List<RestaurantPlanificationPlage>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantCreneau> CreneauxGeneres { get; set; } = new List<RestaurantCreneau>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantPlanifGenerationLog> GenerationLogs { get; set; } = new List<RestaurantPlanifGenerationLog>();
    }
}
