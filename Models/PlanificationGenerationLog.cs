using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    public class PlanificationGenerationLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPlanificationGenerationLog { get; set; }

        [Required]
        public int IdPlanificationVoyage { get; set; }

        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }

        public int NombreCrees { get; set; }
        public int NombreIgnores { get; set; }
        public int NombreEchecs { get; set; }

        [Required]
        public string DetailsJson { get; set; } = "[]";

        public int? DeclencheParIdUtilisateur { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        [ValidateNever]
        public PlanificationVoyage? PlanificationVoyage { get; set; }
    }
}
