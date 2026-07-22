using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Models.Evenement
{
    public class EvenementPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEvenementPayment { get; set; }

        [Required]
        public int IdEvenementReservation { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReferencePaiement { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string Provider { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? ProviderTxRef { get; set; }

        [Required]
        public EvenementPaymentStatus Status { get; set; } = EvenementPaymentStatus.PENDING;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }

        /// <summary>Devise réellement envoyée à FlexPay (<c>D_p</c> : CDF ou USD).</summary>
        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        /// <summary>Montant tarif métier recalculé (<c>D_t</c>), avant conversion FlexPay.</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantTarif { get; set; }

        /// <summary>Devise du pricing réservation (<c>D_t</c>).</summary>
        [Required]
        [MaxLength(3)]
        public string CodeDeviseTarif { get; set; } = "CDF";

        /// <summary>Taux appliqué pour <c>D_t</c> → <c>D_p</c> (1 si identiques).</summary>
        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal TauxVersDevisePaiement { get; set; } = 1m;

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public EvenementReservation? Reservation { get; set; }
    }
}
