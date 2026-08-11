namespace CongoTravel.Helpers.Evenement
{
    /// <summary>Normalisation des dates session événement en UTC pour comparaisons fiables.</summary>
    public static class EvenementDateTimeUtcHelper
    {
        /// <summary>
        /// Local → UTC via <see cref="DateTime.ToUniversalTime"/> ;
        /// Unspecified → traité comme UTC déjà (évite un double décalage serveur) ;
        /// Utc → inchangé.
        /// </summary>
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
