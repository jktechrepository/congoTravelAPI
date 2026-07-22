using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    public class TypeVehicule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdTypeVehicule { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("Libelle")]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public bool Statut { get; set; } = true;

        public DateTime DateCreation { get; set; } = DateTime.Now;

        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public virtual ICollection<Vehicule> Vehicules { get; set; } = new List<Vehicule>();
    }
}
