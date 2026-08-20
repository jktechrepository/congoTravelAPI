namespace CongoTravel.Helpers.Evenement
{
    /// <summary>Références FlexPay pour le module événementiel (autonome du transport).</summary>
    public static class EvenementFlexPayReferenceHelper
    {
        /// <summary>Référence marchand envoyée à FlexPay (max 20 car.).</summary>
        public static string BuildMerchantReference(int idEvenementReservation)
        {
            var suffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
            var raw = $"EVT{idEvenementReservation:D8}{suffix}";
            return raw.Length <= 20 ? raw : raw[..20];
        }

        /// <summary>Référence marchand pour commande Plan A (max 20 car.).</summary>
        public static string BuildMerchantReferenceForCommande(Guid idCommande)
        {
            var hex = idCommande.ToString("N")[..12].ToUpperInvariant();
            var raw = $"EVTC{hex}";
            return raw.Length <= 20 ? raw : raw[..20];
        }

        /// <summary>OrderNumber provisoire avant réponse FlexPay (max 100 car.).</summary>
        public static string BuildPendingOrderNumber(int idEvenementReservation)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var raw = $"PENDING-EVT-{idEvenementReservation}-{suffix}";
            return raw.Length <= 100 ? raw : raw[..100];
        }

        /// <summary>OrderNumber provisoire commande Plan A (max 100 car.).</summary>
        public static string BuildPendingOrderNumberForCommande(Guid idCommande)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var raw = $"PENDING-EVTC-{idCommande:N}-{suffix}";
            return raw.Length <= 100 ? raw : raw[..100];
        }
    }
}
