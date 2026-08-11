namespace CongoTravel.Helpers.Restaurant
{
    public static class RestaurantIdempotencyHelper
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
