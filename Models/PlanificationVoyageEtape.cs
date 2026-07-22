using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    public class PlanificationVoyageEtape
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPlanificationVoyageEtape { get; set; }

        [Required]
        public int IdPlanificationVoyage { get; set; }

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
        [ValidateNever]
        public PlanificationVoyage? PlanificationVoyage { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Destination? Destination { get; set; }
    }
}
