using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Restaurant
{
    /// <summary>Plage horaire locale d'un template de planification restaurant.</summary>
    public class RestaurantPlanificationPlage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantPlanificationPlage { get; set; }

        [Required]
        public int IdRestaurantPlanification { get; set; }

        public int Ordre { get; set; }

        [MaxLength(120)]
        public string? Libelle { get; set; }

        /// <summary>Heure de début locale (UTC+1 fixe).</summary>
        [Required]
        [Column(TypeName = "time")]
        public TimeOnly StartTime { get; set; }

        /// <summary>Heure de fin locale (UTC+1 fixe).</summary>
        [Required]
        [Column(TypeName = "time")]
        public TimeOnly EndTime { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantPlanification? Planification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantPlanifPlageGlobalQuota? GlobalQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantPlanifPlageZoneQuota> ZoneQuotas { get; set; } = new List<RestaurantPlanifPlageZoneQuota>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantCreneau> CreneauxGeneres { get; set; } = new List<RestaurantCreneau>();
    }
}
