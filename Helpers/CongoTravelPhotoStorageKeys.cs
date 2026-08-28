namespace CongoTravel.Helpers
{
    /// <summary>Construction des clés de stockage photo CongoTravel.</summary>
    public static class CongoTravelPhotoStorageKeys
    {
        public const string EntityVehicules = "vehicules";
        public const string EntityEvenementSessions = "evenement-sessions";
        public const string EntityRestaurants = "restaurants";
        public const string EntityHotels = "hotels";
        public const string EntitySitesTouristiques = "sites-touristiques";

        public static string BuildRelativeKey(
            string entityFolder,
            int parentId,
            int ordre,
            string extension)
        {
            var ext = string.IsNullOrWhiteSpace(extension)
                ? ".jpg"
                : (extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant());

            return $"{entityFolder}/{parentId}/{ordre}-{Guid.NewGuid():N}{ext}";
        }

        public static string CombinePrefix(string? prefix, string relativeKey)
        {
            var p = string.IsNullOrWhiteSpace(prefix) ? "congotravel/photos" : prefix.Trim().Trim('/');
            var r = relativeKey.Trim().TrimStart('/');
            return $"{p}/{r}";
        }
    }
}
