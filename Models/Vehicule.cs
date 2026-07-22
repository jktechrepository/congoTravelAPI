using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    public class Vehicule
    {
        [Key]
        public int IdVehicule { get; set; }

        [Required]
        [MaxLength(100)]
        public string? Marques { get; set; }

        [Required]
        [MaxLength(100)]
        public string AliasVehicule { get; set; } = string.Empty;

        [Required]
        public int IdTypeVehicule { get; set; }

        [Required]
        public int NombreSiege { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(20)]
        public string NumeroDePlaque { get; set; } = string.Empty;

        public bool? Statut { get; set; } = true;

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public TypeVehicule? TypeVehicule { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<Siege>? Sieges { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<PhotoVehicule>? Photos { get; set; }
    }
}
