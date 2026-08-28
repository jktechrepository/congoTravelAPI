namespace CongoTravel.Helpers.Hotel
{
    public static class HotelFlexPayConstants
    {
        public const string Provider = "FLEXPAY";
        public const string CallbackRoute = "/api/hotels/flexpay/callback";
    }

    public static class HotelFlexPayReferenceHelper
    {
        public static string BuildMerchantReferenceForCommande(Guid id)
        {
            var raw = $"HC{id:N}"[..20].ToUpperInvariant();
            return raw;
        }

        public static string BuildPendingOrderNumberForCommande(Guid id)
        {
            var raw = $"PENDING-HC-{id:N}";
            return raw.Length <= 100 ? raw : raw[..100];
        }
    }
}
