using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    public class PlanificationVoyageTarif
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPlanificationVoyageTarif { get; set; }

        [Required]
        public int IdPlanificationVoyage { get; set; }

        [Required]
        public int IdCategorieSiege { get; set; }

        [Required]
        public int Prix { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        [ValidateNever]
        public PlanificationVoyage? PlanificationVoyage { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public CategorieSiege? CategorieSiege { get; set; }
    }
}
