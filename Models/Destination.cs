using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Modèle représentant une destination de voyage
    /// </summary>
    public class Destination
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDestination { get; set; }

        /// <summary>
        /// Ville de départ
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string VilleDepart { get; set; } = string.Empty;

        /// <summary>
        /// Ville d'arrivée
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string VilleArrivee { get; set; } = string.Empty;

        /// <summary>
        /// Montant du trajet
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }

        /// <summary>
        /// Heure de départ au format HH:mm (optionnel)
        /// </summary>
        [Column("HeureDepart", TypeName = "time")]
        public TimeOnly? HeureDepart { get; set; }

        /// <summary>
        /// Jour de départ (optionnel)
        /// </summary>
        [MaxLength(50)]
        [Column("jourDepart")]
        public string? JourDepart { get; set; }

        /// <summary>
        /// Statut de la destination (actif/inactif)
        /// </summary>
        public bool Statut { get; set; } = true;

        /// <summary>
        /// Date de création de la destination
        /// </summary>
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        /// <summary>
        /// Identifiant de la société propriétaire de cette destination
        /// </summary>
        [ValidateNever]
        [ForeignKey("Societe")]
        [Column("IdSociete")]
        public int IdSociete { get; set; }

        // Navigation properties
        /// <summary>
        /// Société propriétaire de cette destination
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }
    }
}
