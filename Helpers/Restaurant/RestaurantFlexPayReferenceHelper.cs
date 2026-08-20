namespace CongoTravel.Helpers.Restaurant
{
    /// <summary>Références FlexPay pour le module restaurant (autonome du transport).</summary>
    public static class RestaurantFlexPayReferenceHelper
    {
        /// <summary>Référence marchand envoyée à FlexPay (max 20 car.).</summary>
        public static string BuildMerchantReference(int idRestaurantReservation)
        {
            var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
            var raw = $"RS{idRestaurantReservation:D8}{suffix}";
            return raw.Length <= 20 ? raw : raw[..20];
        }

        /// <summary>OrderNumber provisoire avant réponse FlexPay (max 100 car.).</summary>
        public static string BuildPendingOrderNumber(int idRestaurantReservation)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var raw = $"PENDING-RS-{idRestaurantReservation}-{suffix}";
            return raw.Length <= 100 ? raw : raw[..100];
        }

        public static string BuildMerchantReferenceForCommande(Guid idRestaurantCommandeEnAttente)
        {
            var raw = $"RC{idRestaurantCommandeEnAttente:N}"[..20].ToUpperInvariant();
            return raw;
        }

        public static string BuildPendingOrderNumberForCommande(Guid idRestaurantCommandeEnAttente)
        {
            var raw = $"PENDING-RC-{idRestaurantCommandeEnAttente:N}";
            return raw.Length <= 100 ? raw : raw[..100];
        }
    }
}
