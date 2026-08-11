namespace CongoTravel.Helpers.SiteTouristique
{
    /// <summary>Normalisation des clés d'idempotence réservation / paiement site touristique.</summary>
    public static class SiteTouristiqueIdempotencyHelper
    {
        public const int MaxLength = 120;

        public static string? NormalizeKey(string? idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return null;

            var trimmed = idempotencyKey.Trim();
            return trimmed.Length > MaxLength ? trimmed[..MaxLength] : trimmed;
        }
    }
}
