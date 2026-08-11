namespace CongoTravel.Helpers.SiteTouristique
{
    /// <summary>Références FlexPay pour le module site touristique (autonome du transport).</summary>
    public static class SiteTouristiqueFlexPayReferenceHelper
    {
        /// <summary>Référence marchand envoyée à FlexPay (max 20 car.).</summary>
        public static string BuildMerchantReference(int idSiteTouristiqueReservation)
        {
            var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
            var raw = $"ST{idSiteTouristiqueReservation:D8}{suffix}";
            return raw.Length <= 20 ? raw : raw[..20];
        }

        /// <summary>OrderNumber provisoire avant réponse FlexPay (max 100 car.).</summary>
        public static string BuildPendingOrderNumber(int idSiteTouristiqueReservation)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var raw = $"PENDING-ST-{idSiteTouristiqueReservation}-{suffix}";
            return raw.Length <= 100 ? raw : raw[..100];
        }
    }
}
