namespace CongoTravel.Helpers.Restaurant
{
    public static class RestaurantDateTimeUtcHelper
    {
        public static DateTime NormalizeToUtc(DateTime value) =>
            value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

        public static DateTime? NormalizeToUtc(DateTime? value) =>
            value.HasValue ? NormalizeToUtc(value.Value) : null;
    }
}
