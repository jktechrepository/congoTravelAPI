using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongoTravel.Models
{
    /// <summary>
    /// Suivi technique d'une transaction FlexPay (réservation transport).
    /// </summary>
    public class TransactionFlexPay
    {
        [Key]
        public Guid IdTransaction { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ProviderReference { get; set; }

        /// <summary>1 = Mobile Money, 2 = Carte bancaire.</summary>
        [Required]
        [MaxLength(10)]
        public string TypePaiement { get; set; } = "1";

        [MaxLength(50)]
        public string? Channel { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountCustomer { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "CDF";

        [MaxLength(20)]
        public string? Phone { get; set; }

        public int StatusFlexPay { get; set; } = 2;

        [MaxLength(10)]
        public string? CodeFlexPay { get; set; }

        [MaxLength(500)]
        public string? MessageFlexPay { get; set; }

        public int StatutPaiement { get; set; }

        [MaxLength(100)]
        public string? Merchant { get; set; }

        [MaxLength(500)]
        public string? CallbackUrl { get; set; }

        [MaxLength(500)]
        public string? PaymentUrl { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime? DateCreationFlexPay { get; set; }

        public DateTime? DateCallback { get; set; }

        public DateTime? DateDerniereVerification { get; set; }

        public int IdUtilisateur { get; set; }

        public Guid? IdCommandeReservationEnAttente { get; set; }

        public int? IdPaiement { get; set; }

        public int? IdReservation { get; set; }

        [MaxLength(1000)]
        public string? MessageErreur { get; set; }

        public int? CodeHttpFlexPay { get; set; }

        public string? ReponseBruteFlexPay { get; set; }

        public int NombreCallbacks { get; set; }

        public int NombreVerifications { get; set; }
    }
}
