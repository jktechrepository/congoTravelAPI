using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>
    /// Initiation FlexPay sur une réservation site touristique <c>HOLD</c>
    /// (usage interne / façade <c>with-paiement-electronique</c>).
    /// </summary>
    public class SiteTouristiqueInitiateFlexPayRequestDto
    {
        /// <summary><c>MOBILE_MONEY</c> ou <c>CARTE_BANCAIRE</c> (codes canoniques <see cref="Helpers.MethodePaiementHelper"/>).</summary>
        [Required]
        [MaxLength(50)]
        public string MethodePaiement { get; set; } = string.Empty;

        /// <summary>Obligatoire si <c>MethodePaiement == MOBILE_MONEY</c>.</summary>
        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>Site marchand FlexPay (résolution <c>InfoPaiement</c>).</summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int IdSite { get; set; }

        /// <summary>
        /// Devise de paiement FlexPay (<c>D_p</c>) : <c>CDF</c> ou <c>USD</c>.
        /// Si omis / vide, la devise tarif de la réservation est utilisée (pas de conversion).
        /// </summary>
        [MaxLength(3)]
        public string? CodeDevisePaiement { get; set; }

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }
    }
}
