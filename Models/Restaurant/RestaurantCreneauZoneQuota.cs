using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Restaurant
{
    /// <summary>Quota / prix par zone pour un créneau en mode ClassQuota.</summary>
    public class RestaurantCreneauZoneQuota
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantCreneauZoneQuota { get; set; }

        [Required]
        public int IdRestaurantCreneau { get; set; }

        [Required]
        public int IdRestaurantZone { get; set; }

        [Required]
        public int CapaciteTotale { get; set; }

        public int QuantiteHold { get; set; }

        public int QuantiteVendue { get; set; }

        /// <summary>Prix unitaire de référence (base de calcul d'acompte si pas de MontantAcompte créneau).</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantCreneau? Creneau { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantZone? Zone { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantReservationLine> ReservationLines { get; set; } = new List<RestaurantReservationLine>();
    }
}
