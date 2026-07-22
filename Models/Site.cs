using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Site opérationnel rattaché à une société.
    /// </summary>
    public class Site
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("IdSite")]
        public int IdSite { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(40)]
        [Column("CodeSite")]
        public string CodeSite { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [Column("NomSite")]
        public string NomSite { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? Ville { get; set; }

        [MaxLength(500)]
        public string? Adresse { get; set; }

        [MaxLength(30)]
        public string? Telephone { get; set; }

        /// <summary>Numéro Mobile Money du site (encaissement / affichage guichet).</summary>
        [MaxLength(30)]
        public string? NumeroMobileMoney { get; set; }

        [Required]
        [MaxLength(200)]
        public string NomResponsableSite { get; set; } = string.Empty;

        [MaxLength(200)]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [MaxLength(10)]
        public string Genre { get; set; } = "Masculin";

        [Required]
        public bool Statut { get; set; } = true;

        /// <summary>Un seul site principal actif par société (marchand FlexPay de repli).</summary>
        public bool IsSitePrincipal { get; set; } = false;

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

    }
}
