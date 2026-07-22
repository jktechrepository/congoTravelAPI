namespace CongoTravel.Helpers.Evenement
{
    /// <summary>Normalisation des clés d'idempotence réservation / paiement événement.</summary>
    public static class EvenementIdempotencyHelper
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
