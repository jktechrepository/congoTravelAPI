using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Ligne passager figée d'une feuille de route (snapshot à la génération).
    /// </summary>
    public class FeuilleDeRoutePassager
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdFeuilleDeRoutePassager { get; set; }

        [Required]
        public int IdFeuilleDeRoute { get; set; }

        public int? IdEmbarquement { get; set; }

        public int? IdBillet { get; set; }

        public int? IdReservationPassenger { get; set; }

        public int? IdReservation { get; set; }

        [Required]
        [MaxLength(200)]
        public string NomComplet { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telephone { get; set; }

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? DocumentType { get; set; }

        [MaxLength(100)]
        public string? DocumentNumero { get; set; }

        [MaxLength(120)]
        public string? CodeSiege { get; set; }

        public DateTime? DateEmbarquementUtc { get; set; }

        public int? IdUtilisateurEnregistrement { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public FeuilleDeRoute? FeuilleDeRoute { get; set; }
    }
}
