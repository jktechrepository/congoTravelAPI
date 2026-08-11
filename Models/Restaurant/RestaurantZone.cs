using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Restaurant
{
    /// <summary>Zone de salle (Terrasse, Salle…) — catalogue Mode B ClassQuota.</summary>
    public class RestaurantZone
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantZone { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdRestaurant { get; set; }

        [MaxLength(64)]
        public string? Code { get; set; }

        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool Actif { get; set; } = true;

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
        public ICollection<RestaurantCreneauZoneQuota> ZoneQuotas { get; set; } = new List<RestaurantCreneauZoneQuota>();
    }
}
