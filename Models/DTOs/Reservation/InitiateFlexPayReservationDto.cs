using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Reservation
{
    /// <summary>
    /// Initiation paiement électronique FlexPay (sans création de réservation avant callback).
    /// </summary>
    public class InitiateFlexPayReservationDto
    {
        [Required]
        public ReservationDataDto Reservation { get; set; } = new();

        [Required]
        public FlexPayPaiementDataDto Paiement { get; set; } = new();
    }

    public class FlexPayPaiementDataDto
    {
        /// <summary>
        /// Montant attendu dans la devise tarif du voyage (<c>Reservation.IdVoyage.CodeDevisePrix</c>),
        /// même si <c>CodeDevisePaiement</c> diffère.
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal MontantAPaye { get; set; }

        [Required]
        [StringLength(50)]
        public string MethodePaiement { get; set; } = string.Empty;

        [Required]
        [StringLength(3)]
        public string CodeDevisePaiement { get; set; } = "CDF";

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdUtilisateur { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdSociete { get; set; }

        public int? IdSite { get; set; }
    }

    [Obsolete("Utiliser ReservationWithPaiementResponseDto (POST reservation_with_paiement_electronique).")]
    public class InitiateFlexPayReservationResponseDto
    {
        public Guid IdCommandeReservationEnAttente { get; set; }
        public int? IdPaiementEnAttente { get; set; }
        public string? OrderNumberFlexPay { get; set; }
        public string? ReferenceFlexPay { get; set; }
        public decimal MontantVoyage { get; set; }
        public string CodeDeviseVoyage { get; set; } = "CDF";
        public decimal MontantFlexPay { get; set; }
        public string CodeDevisePaiement { get; set; } = "CDF";
        public decimal TauxApplique { get; set; } = 1m;
        public DateTime HoldExpireAt { get; set; }
        public string? PaymentUrl { get; set; }
        public bool FlexPayAccepted { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
