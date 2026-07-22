using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Étape ordonnée d’un voyage (référence au référentiel Destination).
    /// </summary>
    public class VoyageDestination
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdVoyageDestination { get; set; }

        /// <summary>
        /// Référence la clé Voyage.Id.
        /// </summary>
        [Required]
        public int IdVoyage { get; set; }

        [Required]
        public int IdDestination { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Ordre { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Voyage? Voyage { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Destination? Destination { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }
    }
}
