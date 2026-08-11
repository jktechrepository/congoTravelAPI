using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Restaurant
{
    /// <summary>Quota global d'un créneau (mode GlobalQuota). PK partagée avec le créneau.</summary>
    public class RestaurantCreneauGlobalQuota
    {
        [Key]
        [ForeignKey(nameof(Creneau))]
        public int IdRestaurantCreneau { get; set; }

        [Required]
        public int CapaciteTotale { get; set; }

        public int QuantiteHold { get; set; }

        public int QuantiteVendue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantCreneau? Creneau { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantReservationLine> ReservationLines { get; set; } = new List<RestaurantReservationLine>();
    }
}
