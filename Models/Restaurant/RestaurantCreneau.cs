using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.Restaurant
{
    /// <summary>Créneau horaire inventorié pour un établissement restaurant.</summary>
    public class RestaurantCreneau
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantCreneau { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdRestaurant { get; set; }

        /// <summary>Date de service (jour calendaire métier).</summary>
        [Required]
        [Column(TypeName = "date")]
        public DateOnly DateService { get; set; }

        [Required]
        public DateTime StartAtUtc { get; set; }

        [Required]
        public DateTime EndAtUtc { get; set; }

        [Required]
        public RestaurantInventoryMode InventoryMode { get; set; }

        [Required]
        public RestaurantStatus Status { get; set; } = RestaurantStatus.Draft;

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        /// <summary>Montant d'acompte fixe optionnel (sinon % défaut établissement).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontantAcompte { get; set; }

        /// <summary>Template de planification d'origine (génération batch), optionnel.</summary>
        public int? IdRestaurantPlanification { get; set; }

        /// <summary>Plage du template ayant produit ce créneau, optionnel.</summary>
        public int? IdRestaurantPlanificationPlage { get; set; }

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
        public RestaurantPlanification? Planification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantPlanificationPlage? PlanificationPlage { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantCreneauGlobalQuota? GlobalQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantCreneauZoneQuota> ZoneQuotas { get; set; } = new List<RestaurantCreneauZoneQuota>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantReservation> Reservations { get; set; } = new List<RestaurantReservation>();
    }
}
