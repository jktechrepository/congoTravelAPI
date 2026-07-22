using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>
    /// Corps de <c>POST /api/events/reservations/{idReservation}/confirm-payment</c>.
    /// V1 CASH : <c>MethodePaiement = CASH</c>.
    /// FlexPay : utiliser <c>POST .../initiate-flexpay</c> (Phase 5).
    /// </summary>
    public class EvenementConfirmPaymentRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string MethodePaiement { get; set; } = "CASH";

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        /// <summary>Référence caisse / reçu (optionnel).</summary>
        [MaxLength(100)]
        public string? ReferenceTransaction { get; set; }
    }
}
