using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.SiteTouristique
{
    public class SiteTouristiqueClasse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSiteTouristiqueClasse { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [MaxLength(50)]
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
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiqueClassQuota> ClassQuotas { get; set; } = new List<SiteTouristiqueClassQuota>();
    }
}
