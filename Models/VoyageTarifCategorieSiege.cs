using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Prix du voyage pour une catégorie de siège donnée (par place).
    /// </summary>
    public class VoyageTarifCategorieSiege
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdVoyageTarifCategorieSiege { get; set; }

        [Required]
        public int IdVoyage { get; set; }

        [Required]
        public int IdCategorieSiege { get; set; }

        /// <summary>Montant par passager pour cette catégorie sur ce voyage.</summary>
        [Required]
        public int Prix { get; set; }

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
        public CategorieSiege? CategorieSiege { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }
    }
}
