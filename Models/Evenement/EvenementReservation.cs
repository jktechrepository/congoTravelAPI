using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Models.Evenement
{
    public class EvenementReservation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEvenementReservation { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdEvenementSession { get; set; }

        /// <summary>Site opérationnel (défaut session, override possible à l'achat).</summary>
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
        public EvenementReservationStatus Status { get; set; } = EvenementReservationStatus.HOLD;

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
        public EvenementSession? Session { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementReservationLine> Lines { get; set; } = new List<EvenementReservationLine>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementPayment> Payments { get; set; } = new List<EvenementPayment>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementSessionSeat> SeatsEnCours { get; set; } = new List<EvenementSessionSeat>();
    }
}
