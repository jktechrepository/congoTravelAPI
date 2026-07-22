using CongoTravel.Models.DTOs;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Calcule l'intervalle inclusif sur <see cref="Models.Voyage.DateDepart"/> (jour civil)
    /// pour les listes GET avec paramètres <c>date</c> et <c>periode</c>.
    /// <c>Tout</c> retourne (null, null) : aucun filtre sur <see cref="Models.Voyage.DateDepart"/>.
    /// </summary>
    public static class VoyageListeDateFilter
    {
        public static (DateTime? DateDebut, DateTime? DateFin) Resolve(DateTime? date, VoyageListePeriode periode)
        {
            if (periode == VoyageListePeriode.Tout)
                return (null, null);

            var reference = (date ?? DateTime.Today).Date;

            return periode switch
            {
                VoyageListePeriode.Jour => (reference, reference),
                VoyageListePeriode.Hebdomadaire => GetIsoWeekRange(reference),
                VoyageListePeriode.Mensuel => GetCalendarMonthRange(reference),
                _ => throw new ArgumentOutOfRangeException(nameof(periode), periode, "Période non supportée.")
            };
        }

        /// <summary>Semaine du lundi au dimanche contenant la date de référence.</summary>
        private static (DateTime DateDebut, DateTime DateFin) GetIsoWeekRange(DateTime reference)
        {
            var daysSinceMonday = ((int)reference.DayOfWeek + 6) % 7;
            var debut = reference.AddDays(-daysSinceMonday);
            return (debut, debut.AddDays(6));
        }

        private static (DateTime DateDebut, DateTime DateFin) GetCalendarMonthRange(DateTime reference)
        {
            var debut = new DateTime(reference.Year, reference.Month, 1);
            var fin = debut.AddMonths(1).AddDays(-1);
            return (debut, fin);
        }
    }
}
