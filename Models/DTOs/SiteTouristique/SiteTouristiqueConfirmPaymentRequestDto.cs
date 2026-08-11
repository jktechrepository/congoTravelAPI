using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>
    /// Confirmation paiement CASH (usage interne / façade <c>with-paiement</c>).
    /// </summary>
    public class SiteTouristiqueConfirmPaymentRequestDto
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
