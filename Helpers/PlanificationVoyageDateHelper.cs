using CongoTravel.Models.Enums;

namespace CongoTravel.Helpers
{
    public static class PlanificationVoyageDateHelper
    {
        public static (DateTime DebutUtc, DateTime FinUtc) ResolvePeriode(
            PlanificationGenerationMode mode,
            DateTime? dateDebut,
            DateTime? dateFin)
        {
            var now = DateTime.UtcNow.Date;

            return mode switch
            {
                PlanificationGenerationMode.SemaineCourante => ResolveSemaineCourante(now),
                PlanificationGenerationMode.MoisCourant => ResolveMois(now),
                PlanificationGenerationMode.MoisProchain => ResolveMois(now.AddMonths(1)),
                PlanificationGenerationMode.PeriodePersonnalisee => (
                    DateTime.SpecifyKind(dateDebut!.Value.Date, DateTimeKind.Utc),
                    DateTime.SpecifyKind(dateFin!.Value.Date, DateTimeKind.Utc)),
                _ => ResolveMois(now)
            };
        }

        public static List<DateTime> ExpandDates(DateTime debut, DateTime fin, IReadOnlyCollection<int> joursSemaine)
        {
            var daysSet = joursSemaine.Distinct().ToHashSet();
            var result = new List<DateTime>();

            for (var d = debut.Date; d <= fin.Date; d = d.AddDays(1))
            {
                if (daysSet.Contains((int)d.DayOfWeek))
                    result.Add(DateTime.SpecifyKind(d, DateTimeKind.Utc));
            }

            return result;
        }

        private static (DateTime, DateTime) ResolveSemaineCourante(DateTime reference)
        {
            var diff = ((int)reference.DayOfWeek + 6) % 7;
            var monday = reference.AddDays(-diff);
            var sunday = monday.AddDays(6);
            return (
                DateTime.SpecifyKind(monday, DateTimeKind.Utc),
                DateTime.SpecifyKind(sunday, DateTimeKind.Utc));
        }

        private static (DateTime, DateTime) ResolveMois(DateTime referenceInMonth)
        {
            var first = new DateTime(referenceInMonth.Year, referenceInMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var last = first.AddMonths(1).AddDays(-1);
            return (first, last);
        }
    }
}
