namespace CongoTravel.Helpers
{
    /// <summary>
    /// Parse le query <c>status</c> des listes satellites (événement / restaurant / site touristique).
    /// Défaut métier : <c>CONFIRMED</c> ; <c>ALL</c> désactive le filtre (audit).
    /// </summary>
    public static class SatelliteReservationListStatusParser
    {
        public static bool TryParse<TStatus>(
            string? status,
            TStatus confirmedValue,
            out TStatus? parsedStatus,
            out string? errorMessage)
            where TStatus : struct, Enum
        {
            parsedStatus = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(status))
            {
                parsedStatus = confirmedValue;
                return true;
            }

            var trimmed = status.Trim();
            if (string.Equals(trimmed, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                parsedStatus = null;
                return true;
            }

            if (Enum.TryParse(trimmed, ignoreCase: true, out TStatus value)
                && Enum.IsDefined(typeof(TStatus), value))
            {
                parsedStatus = value;
                return true;
            }

            errorMessage =
                $"Statut invalide '{status}'. Valeurs acceptées : HOLD, CONFIRMED, CANCELLED, EXPIRED, ALL.";
            return false;
        }
    }
}
