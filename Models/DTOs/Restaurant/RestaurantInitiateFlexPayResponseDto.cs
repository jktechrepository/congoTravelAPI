namespace CongoTravel.Models.DTOs.Restaurant
{
    /// <summary>Réponse après initiation FlexPay sur une réservation restaurant <c>HOLD</c>.</summary>
    public class RestaurantInitiateFlexPayResponseDto
    {
        public int IdRestaurantReservation { get; set; }

        public RestaurantPaymentResponseDto Payment { get; set; } = new();

        /// <summary>Numéro de commande FlexPay (<c>orderNumber</c>) — à conserver pour verify/callback.</summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>Expiration du hold réservation (UTC).</summary>
        public DateTime ReservationExpiresAtUtc { get; set; }

        /// <summary>URL de paiement carte (null pour Mobile Money push).</summary>
        public string? PaymentUrl { get; set; }

        /// <summary>Montant réellement envoyé à FlexPay (serveur).</summary>
        public decimal MontantFlexPay { get; set; }

        /// <summary>Devise FlexPay (<c>CDF</c> / <c>USD</c>).</summary>
        public string CodeDevisePaiement { get; set; } = "CDF";

        /// <summary>Montant tarif métier avant conversion (acompte).</summary>
        public decimal MontantTarif { get; set; }

        /// <summary>Devise du pricing réservation.</summary>
        public string CodeDeviseTarif { get; set; } = "CDF";

        /// <summary>Taux appliqué <c>D_t</c> → <c>D_p</c>.</summary>
        public decimal TauxApplique { get; set; } = 1m;

        /// <summary><c>true</c> si FlexPay a accepté la demande (<c>code == 0</c>).</summary>
        public bool FlexPayAccepted { get; set; }

        public string Message { get; set; } = string.Empty;

        /// <summary><c>true</c> si un paiement PENDING existait déjà pour la même clé d'idempotence.</summary>
        public bool AlreadyInitiated { get; set; }
    }
}
