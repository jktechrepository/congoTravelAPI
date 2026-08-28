using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    public class HotelPlanifGenerationLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelPlanifGenerationLog { get; set; }

        [Required]
        public int IdHotelPlanification { get; set; }

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
        public HotelPlanification? Planification { get; set; }
    }
}
