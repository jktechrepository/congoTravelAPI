using CongoTravel.Helpers;
using CongoTravel.Models.Enums;

namespace CongoTravel.Helpers.Hotel
{
    /// <summary>Adaptation DateOnly de <see cref="PlanificationVoyageDateHelper"/> pour les allotments hôtel.</summary>
    public static class HotelPlanificationDateHelper
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
