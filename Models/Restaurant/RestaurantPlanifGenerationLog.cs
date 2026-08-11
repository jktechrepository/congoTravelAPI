using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Restaurant
{
    public class RestaurantPlanifGenerationLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantPlanifGenerationLog { get; set; }

        [Required]
        public int IdRestaurantPlanification { get; set; }

        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }

        public int NombreCrees { get; set; }
        public int NombreIgnores { get; set; }
        public int NombreEchecs { get; set; }
        public int NombrePublies { get; set; }

        [Required]
        public string DetailsJson { get; set; } = "[]";

        public int? DeclencheParIdUtilisateur { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        [ValidateNever]
        public RestaurantPlanification? Planification { get; set; }
    }
}
