using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.SiteTouristique
{
    /// <summary>Journée de visite inventoriée pour un lieu touristique.</summary>
    public class SiteTouristiqueJournee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSiteTouristiqueJournee { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdSiteTouristique { get; set; }

        /// <summary>Date de visite (jour calendaire, stockée en UTC midnight de la date métier).</summary>
        [Required]
        [Column(TypeName = "date")]
        public DateOnly DateVisite { get; set; }

        [Required]
        public SiteTouristiqueInventoryMode InventoryMode { get; set; }

        [Required]
        public SiteTouristiqueStatus Status { get; set; } = SiteTouristiqueStatus.Draft;

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        /// <summary>Ouverture optionnelle des ventes (UTC). Null = dès publication.</summary>
        public DateTime? SalesOpenAtUtc { get; set; }

        /// <summary>Fermeture optionnelle des ventes (UTC). Null = fin du jour DateVisite.</summary>
        public DateTime? SalesCloseAtUtc { get; set; }

        /// <summary>Template de planification ayant généré cette journée (nullable).</summary>
        public int? IdSiteTouristiquePlanification { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiqueLieu? Lieu { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiquePlanification? Planification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiqueGlobalQuota? GlobalQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiqueClassQuota> ClassQuotas { get; set; } = new List<SiteTouristiqueClassQuota>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiqueReservation> Reservations { get; set; } = new List<SiteTouristiqueReservation>();
    }
}
