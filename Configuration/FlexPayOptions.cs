namespace CongoTravel.Configuration
{
    public class FlexPayOptions
    {
        public const string SectionName = "FlexPay";

        public bool Enabled { get; set; }

        public int SeatHoldMinutes { get; set; } = 15;

        public string? ApiToken { get; set; }

        public string? Merchant { get; set; }

        public string? CallbackBaseUrl { get; set; }

        public string? MobileMoneyUrl { get; set; } =
            "https://backend.flexpay.cd/api/rest/v1/paymentService";

        public string? PayOutUrl { get; set; } =
            "https://backend.flexpay.cd/api/rest/v1/merchantPayOutService";

        /// <summary>Fenêtre d'idempotence : refuser un second reversement EnAttente sur le même site.</summary>
        public int PayOutPendingMinutes { get; set; } = 15;

        public string? CardPaymentUrl { get; set; } =
            "https://cardpayment.flexpay.cd/v1.1/pay";

        public string? CheckTransactionUrl { get; set; } =
            "https://apicheck.flexpaie.com/api/rest/v1/check";

        public bool ForceProductionCallbackInDev { get; set; }

        /// <summary>Devises autorisées pour le canal Mobile Money.</summary>
        public List<string> MobileMoneySupportedCurrencies { get; set; } = new() { "CDF", "USD" };

        /// <summary>Devises autorisées pour le canal Carte Bancaire.</summary>
        public List<string> CardSupportedCurrencies { get; set; } = new() { "CDF", "USD" };

        /// <summary>
        /// Chemin relatif callback FlexPay événement (défaut <c>/api/events/flexpay/callback</c>).
        /// Concaténé à <see cref="CallbackBaseUrl"/> ou à l'hôte courant.
        /// </summary>
        public string EventCallbackRelativePath { get; set; } = "/api/events/flexpay/callback";

        /// <summary>
        /// Chemin relatif callback FlexPay site touristique (défaut <c>/api/sites-touristiques/flexpay/callback</c>).
        /// </summary>
        public string SiteTouristiqueCallbackRelativePath { get; set; } = "/api/sites-touristiques/flexpay/callback";

        /// <summary>
        /// Chemin relatif callback FlexPay restaurant (défaut <c>/api/restaurants/flexpay/callback</c>).
        /// </summary>
        public string RestaurantCallbackRelativePath { get; set; } = "/api/restaurants/flexpay/callback";

        public string HotelCallbackRelativePath { get; set; } = "/api/hotels/flexpay/callback";

        /// <summary>Kill-switch dédié événement ; si <c>false</c>, seul <see cref="Enabled"/> global s'applique.</summary>
        public bool? EventEnabled { get; set; }

        /// <summary>Kill-switch dédié site touristique ; si null, fallback sur <see cref="Enabled"/>.</summary>
        public bool? SiteTouristiqueEnabled { get; set; }

        /// <summary>Kill-switch dédié restaurant ; si null, fallback sur <see cref="Enabled"/>.</summary>
        public bool? RestaurantEnabled { get; set; }

        public bool? HotelEnabled { get; set; }

        /// <summary>Kill-switch global pour le reversement automatique post-paiement électronique.</summary>
        public bool AutoReversementEnabled { get; set; } = true;

        /// <summary>FlexPay événement actif (fallback sur <see cref="Enabled"/> si null).</summary>
        public bool IsEventEnabled => EventEnabled ?? Enabled;

        /// <summary>FlexPay site touristique actif (fallback sur <see cref="Enabled"/> si null).</summary>
        public bool IsSiteTouristiqueEnabled => SiteTouristiqueEnabled ?? Enabled;

        /// <summary>FlexPay restaurant actif (fallback sur <see cref="Enabled"/> si null).</summary>
        public bool IsRestaurantEnabled => RestaurantEnabled ?? Enabled;

        public bool IsHotelEnabled => HotelEnabled ?? Enabled;
    }
}
