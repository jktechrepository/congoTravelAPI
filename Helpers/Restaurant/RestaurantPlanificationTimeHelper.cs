namespace CongoTravel.Helpers.Restaurant
{
    /// <summary>
    /// Conversion horaires locaux (UTC+1 fixe) → UTC et validation des plages non chevauchantes.
    /// </summary>
    public static class RestaurantPlanificationTimeHelper
    {
        /// <summary>Offset fixe Congo : heure locale = UTC+1 → utc = local − 1h.</summary>
        public static DateTime ToUtc(DateOnly date, TimeOnly time) =>
            DateTime.SpecifyKind(date.ToDateTime(time).AddHours(-1), DateTimeKind.Utc);

        /// <summary>
        /// Rejette les plages invalides (Start ≥ End) ou qui se chevauchent le même jour
        /// (demi-ouvert [Start, End)).
        /// </summary>
        public static void ValidateNoOverlappingPlages(
            IReadOnlyList<(TimeOnly StartTime, TimeOnly EndTime)> plages)
        {
            if (plages == null || plages.Count == 0)
                throw new ArgumentException("Au moins une plage horaire est requise.");

            for (var i = 0; i < plages.Count; i++)
            {
                var (start, end) = plages[i];
                if (end <= start)
                {
                    throw new ArgumentException(
                        $"Plage invalide : StartTime ({start}) doit être strictement antérieur à EndTime ({end}).");
                }
            }

            var ordered = plages
                .Select((p, index) => (p.StartTime, p.EndTime, Index: index))
                .OrderBy(p => p.StartTime)
                .ThenBy(p => p.EndTime)
                .ToList();

            for (var i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var curr = ordered[i];
                // Chevauchement demi-ouvert : StartA < EndB && StartB < EndA
                if (prev.StartTime < curr.EndTime && curr.StartTime < prev.EndTime)
                {
                    throw new ArgumentException(
                        $"Plages horaires chevauchantes : [{prev.StartTime}-{prev.EndTime}] et [{curr.StartTime}-{curr.EndTime}].");
                }
            }
        }
    }
}
