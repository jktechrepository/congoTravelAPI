using CongoTravel.Models.Enums;

namespace CongoTravel.Helpers
{
    public static class PlanificationVoyageDateHelper
    {
        public static (DateTime DebutUtc, DateTime FinUtc) ResolvePeriode(
            PlanificationGenerationMode mode,
            DateTime? dateDebut,
            DateTime? dateFin) =>
            ResolvePeriode(mode, dateDebut, dateFin, DateTime.UtcNow.Date);

        /// <summary>
        /// Surcouche testable : <paramref name="nowUtc"/> doit être une date UTC (composante heure ignorée).
        /// </summary>
        public static (DateTime DebutUtc, DateTime FinUtc) ResolvePeriode(
            PlanificationGenerationMode mode,
            DateTime? dateDebut,
            DateTime? dateFin,
            DateTime nowUtc)
        {
            var now = DateTime.SpecifyKind(nowUtc.Date, DateTimeKind.Utc);

            return mode switch
            {
                PlanificationGenerationMode.SemaineCourante => ResolveSemaineCourante(now),
                PlanificationGenerationMode.MoisCourant => ResolveMoisCourant(now),
                PlanificationGenerationMode.MoisProchain => ResolveMoisComplet(now.AddMonths(1)),
                PlanificationGenerationMode.PeriodePersonnalisee => (
                    DateTime.SpecifyKind(dateDebut!.Value.Date, DateTimeKind.Utc),
                    DateTime.SpecifyKind(dateFin!.Value.Date, DateTimeKind.Utc)),
                _ => ResolveMoisCourant(now)
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

        /// <summary>
        /// Semaine ISO-like (lundi→dimanche) clampée : pas de jours avant <paramref name="now"/>.
        /// </summary>
        private static (DateTime, DateTime) ResolveSemaineCourante(DateTime now)
        {
            var diff = ((int)now.DayOfWeek + 6) % 7;
            var monday = now.AddDays(-diff);
            var sunday = monday.AddDays(6);
            var debut = now > monday ? now : monday;
            return (
                DateTime.SpecifyKind(debut, DateTimeKind.Utc),
                DateTime.SpecifyKind(sunday, DateTimeKind.Utc));
        }

        /// <summary>
        /// Mois civil UTC du <paramref name="now"/> : du jour courant au dernier jour du mois
        /// (exclut les jours déjà passés).
        /// </summary>
        private static (DateTime, DateTime) ResolveMoisCourant(DateTime now)
        {
            var (_, last) = ResolveMoisComplet(now);
            return (now, last);
        }

        /// <summary>Mois civil complet (1er → dernier jour) pour le mois de <paramref name="referenceInMonth"/>.</summary>
        private static (DateTime, DateTime) ResolveMoisComplet(DateTime referenceInMonth)
        {
            var first = new DateTime(referenceInMonth.Year, referenceInMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var last = first.AddMonths(1).AddDays(-1);
            return (first, last);
        }
    }
}
