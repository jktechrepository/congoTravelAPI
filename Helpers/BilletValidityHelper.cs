using CongoTravel.Models;

namespace CongoTravel.Helpers
{
    public static class BilletValidityHelper
    {
        public static (DateTime Start, DateTime End) ComputeValidityWindow(DateTime dateDepart, int dureeValiditeBilletJours)
        {
            var start = dateDepart.Date;
            var days = Math.Max(0, dureeValiditeBilletJours);
            var end = start.AddDays(days).Date.AddDays(1).AddTicks(-1);
            return (start, end);
        }

        public static void ApplyToBillet(Billet billet, DateTime dateDepart, int dureeValiditeBilletJours)
        {
            var (start, end) = ComputeValidityWindow(dateDepart, dureeValiditeBilletJours);
            billet.DateValiditeDebut = start;
            billet.DateValiditeFin = end;
        }

        public static (DateTime? Start, DateTime? End) ResolveWindow(
            Billet billet,
            Voyage? voyage,
            int dureeValiditeBilletJours)
        {
            if (billet.DateValiditeDebut.HasValue || billet.DateValiditeFin.HasValue)
            {
                return (
                    NormalizeValidityInstant(billet.DateValiditeDebut, endOfDay: false),
                    NormalizeValidityInstant(billet.DateValiditeFin, endOfDay: true));
            }

            if (voyage == null)
                return (null, null);

            var (start, end) = ComputeValidityWindow(voyage.DateDepart, dureeValiditeBilletJours);
            return (start, end);
        }

        /// <summary>
        /// Les dates SQL/legacy à minuit (00:00) désignent un jour civil entier, pas une expiration à l'instant T=0h.
        /// </summary>
        internal static DateTime? NormalizeValidityInstant(DateTime? value, bool endOfDay)
        {
            if (!value.HasValue)
                return null;

            var v = value.Value;
            if (v.TimeOfDay != TimeSpan.Zero)
                return v;

            return endOfDay
                ? v.Date.AddDays(1).AddTicks(-1)
                : v.Date;
        }
    }
}
