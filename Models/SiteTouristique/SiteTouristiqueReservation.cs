using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.SiteTouristique
{
    public class SiteTouristiqueReservation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSiteTouristiqueReservation { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdSiteTouristiqueJournee { get; set; }

        /// <summary>Site opérationnel (défaut lieu, override possible à l'achat).</summary>
        public int? IdSite { get; set; }

        [Required]
        [MaxLength(64)]
        public string ReferenceReservation { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? CustomerRef { get; set; }

        /// <summary>Acheteur authentifié (JWT) pour notifications SignalR FlexPay ; null si guichet / legacy.</summary>
        public int? IdUtilisateur { get; set; }

        /// <summary>Client lié à l'acheteur (Utilisateur.IdClient) ; null si guichet / utilisateur sans profil client.</summary>
        public int? IdClient { get; set; }

        [Required]
        public SiteTouristiqueReservationStatus Status { get; set; } = SiteTouristiqueReservationStatus.HOLD;

        public DateTime? ExpiresAtUtc { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantSousTotal { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Client? Client { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Site? Site { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiqueJournee? Journee { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiqueReservationLine> Lines { get; set; } = new List<SiteTouristiqueReservationLine>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiquePayment> Payments { get; set; } = new List<SiteTouristiquePayment>();
    }
}
