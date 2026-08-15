using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.SiteTouristique
{
    /// <summary>Lieu / attraction touristique (produit) — distinct de <see cref="Site"/> (guichet marchand).</summary>
    public class SiteTouristiqueLieu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSiteTouristique { get; set; }

        [Required]
        public int IdSociete { get; set; }

        /// <summary>Site opérationnel (guichet / bénéficiaire PayOut futur). Nullable legacy ; requis à la création.</summary>
        public int? IdSite { get; set; }

        [Required]
        [MaxLength(64)]
        public string CodeLieu { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(120)]
        public string? Province { get; set; }

        [MaxLength(120)]
        public string? Ville { get; set; }

        [MaxLength(500)]
        public string? Adresse { get; set; }

        [MaxLength(30)]
        public string? Telephone { get; set; }

        /// <summary>Heure d'ouverture au public (horaire récurrent du lieu).</summary>
        [Column(TypeName = "time")]
        public TimeOnly? HeureOuverture { get; set; }

        /// <summary>Heure de fermeture au public (horaire récurrent du lieu).</summary>
        [Column(TypeName = "time")]
        public TimeOnly? HeureFermeture { get; set; }

        /// <summary>Jours d'ouverture (texte libre, ex. Lun-Dim).</summary>
        [MaxLength(100)]
        public string? JourOuverture { get; set; }

        [Required]
        public SiteTouristiqueStatus Status { get; set; } = SiteTouristiqueStatus.Draft;

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Site? Site { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiqueJournee> Journees { get; set; } = new List<SiteTouristiqueJournee>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiquePlanification> Planifications { get; set; } = new List<SiteTouristiquePlanification>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiqueLieuPhoto> Photos { get; set; } = new List<SiteTouristiqueLieuPhoto>();
    }
}
