using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.Restaurant
{
    /// <summary>Établissement restaurant (produit catalogue) — distinct de <see cref="Site"/> (guichet marchand).</summary>
    public class Restaurant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurant { get; set; }

        [Required]
        public int IdSociete { get; set; }

        /// <summary>Site opérationnel (guichet / bénéficiaire PayOut futur). Nullable legacy ; requis à la création.</summary>
        public int? IdSite { get; set; }

        [Required]
        [MaxLength(64)]
        public string CodeRestaurant { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Adresse { get; set; }

        /// <summary>Pourcentage d'acompte par défaut (0–100).</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal AcomptePourcentDefaut { get; set; }

        [Required]
        public RestaurantStatus Status { get; set; } = RestaurantStatus.Draft;

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
        public ICollection<RestaurantCreneau> Creneaux { get; set; } = new List<RestaurantCreneau>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantZone> Zones { get; set; } = new List<RestaurantZone>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantPlanification> Planifications { get; set; } = new List<RestaurantPlanification>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantReservation> Reservations { get; set; } = new List<RestaurantReservation>();
    }
}
