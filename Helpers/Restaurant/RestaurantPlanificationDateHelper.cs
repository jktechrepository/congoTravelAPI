using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.Enums;

namespace CongoTravel.Helpers.Restaurant
{
    /// <summary>Réutilise la résolution de période / expansion des jours ST pour les créneaux restaurant.</summary>
    public static class RestaurantPlanificationDateHelper
    {
        public static (DateTime DebutUtc, DateTime FinUtc) ResolvePeriode(
            PlanificationGenerationMode mode,
            DateTime? dateDebut,
            DateTime? dateFin) =>
            SiteTouristiquePlanificationDateHelper.ResolvePeriode(mode, dateDebut, dateFin);

        public static (DateTime DebutUtc, DateTime FinUtc) ResolvePeriode(
            PlanificationGenerationMode mode,
            DateTime? dateDebut,
            DateTime? dateFin,
            DateTime nowUtc) =>
            SiteTouristiquePlanificationDateHelper.ResolvePeriode(mode, dateDebut, dateFin, nowUtc);

        public static List<DateOnly> ExpandDates(
            DateTime debut,
            DateTime fin,
            IReadOnlyCollection<int> joursSemaine) =>
            SiteTouristiquePlanificationDateHelper.ExpandDates(debut, fin, joursSemaine);
    }
}