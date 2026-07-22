using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Catégorie de siège / classe tarifaire (référentiel par société) — ex. ECO, PREMIERE, AFFAIRES.
    /// </summary>
    public class CategorieSiege
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCategorieSiege { get; set; }

        [Required]
        public int IdSociete { get; set; }

        /// <summary>Code court stable (QR, CodeSiege) — unique par société.</summary>
        [Required]
        [MaxLength(40)]
        public string CodeCategorieSiege { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        public bool Statut { get; set; } = true;

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<Siege>? Sieges { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<VoyageTarifCategorieSiege>? VoyageTarifsCategorieSiege { get; set; }
    }
}
