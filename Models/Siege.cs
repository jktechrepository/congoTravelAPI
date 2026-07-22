using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Siège physique rattaché à un véhicule. CodeSiege au format AliasVehicule/{NumeroOrdre}.
    /// </summary>
    public class Siege
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSiege { get; set; }

        [Required]
        public int IdVehicule { get; set; }

        /// <summary>
        /// Numéro de 1 à NombreSiege du véhicule.
        /// </summary>
        [Required]
        public int NumeroOrdre { get; set; }

        [Required]
        [MaxLength(120)]
        public string CodeSiege { get; set; } = string.Empty;

        /// <summary>
        /// Siège utilisable pour attribution (false = désactivé).
        /// </summary>
        public bool EstActif { get; set; } = true;

        [Required]
        public int IdSociete { get; set; }

        /// <summary>Classe tarifaire du siège (ex. ECO, PREMIERE).</summary>
        [Required]
        public int IdCategorieSiege { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Vehicule? Vehicule { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public CategorieSiege? CategorieSiege { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<VoyageSeatAllocation>? VoyageSeatAllocations { get; set; }
    }
}
