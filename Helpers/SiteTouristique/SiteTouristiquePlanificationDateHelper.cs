using CongoTravel.Helpers;
using CongoTravel.Models.Enums;

namespace CongoTravel.Helpers.SiteTouristique
{
    /// <summary>Adaptation DateOnly de <see cref="PlanificationVoyageDateHelper"/> pour les journées site touristique.</summary>
    public static class SiteTouristiquePlanificationDateHelper
    {
        public static (DateTime DebutUtc, DateTime FinUtc) ResolvePeriode(
            PlanificationGenerationMode mode,
            DateTime? dateDebut,
            DateTime? dateFin) =>
            PlanificationVoyageDateHelper.ResolvePeriode(mode, dateDebut, dateFin);

        public static (DateTime DebutUtc, DateTime FinUtc) ResolvePeriode(
            PlanificationGenerationMode mode,
            DateTime? dateDebut,
            DateTime? dateFin,
            DateTime nowUtc) =>
            PlanificationVoyageDateHelper.ResolvePeriode(mode, dateDebut, dateFin, nowUtc);

        public static List<DateOnly> ExpandDates(DateTime debut, DateTime fin, IReadOnlyCollection<int> joursSemaine) =>
            PlanificationVoyageDateHelper.ExpandDates(debut, fin, joursSemaine)
                .Select(DateOnly.FromDateTime)
                .ToList();
    }
}
