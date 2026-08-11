using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Helpers.SiteTouristique
{
    /// <summary>
    /// Fenêtre de vente journée : Published + now dans
    /// [<see cref="SiteTouristiqueJournee.SalesOpenAtUtc"/> ?? -∞,
    ///  <see cref="SiteTouristiqueJournee.SalesCloseAtUtc"/> ?? fin de <see cref="SiteTouristiqueJournee.DateVisite"/> UTC].
    /// </summary>
    public static class SiteTouristiqueJourneeSalesEligibilityHelper
    {
        /// <summary>Fin de vente UTC : SalesCloseAtUtc si présent, sinon fin du jour DateVisite (UTC).</summary>
        public static DateTime ResolveSalesEndUtc(SiteTouristiqueJournee journee)
        {
            if (journee.SalesCloseAtUtc.HasValue)
                return SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(journee.SalesCloseAtUtc.Value);

            return journee.DateVisite.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        }

        public static DateTime? ResolveSalesOpenUtc(SiteTouristiqueJournee journee)
        {
            if (!journee.SalesOpenAtUtc.HasValue)
                return null;

            return SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(journee.SalesOpenAtUtc.Value);
        }

        public static bool CanSell(SiteTouristiqueJournee journee, DateTime utcNow)
        {
            if (journee.Status != SiteTouristiqueStatus.Published)
                return false;

            var now = SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(utcNow);
            var open = ResolveSalesOpenUtc(journee);
            if (open.HasValue && now < open.Value)
                return false;

            return now < ResolveSalesEndUtc(journee);
        }

        public static void EnsureCanSell(SiteTouristiqueJournee journee, DateTime utcNow)
        {
            if (journee.Status != SiteTouristiqueStatus.Published)
            {
                throw new InvalidOperationException(
                    $"Impossible de vendre pour une journée au statut {journee.Status} (Published requis).");
            }

            var now = SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(utcNow);
            var open = ResolveSalesOpenUtc(journee);
            if (open.HasValue && now < open.Value)
            {
                throw new InvalidOperationException(
                    $"Vente pas encore ouverte (ouverture : {open.Value:O}).");
            }

            var salesEnd = ResolveSalesEndUtc(journee);
            if (now >= salesEnd)
            {
                throw new InvalidOperationException(
                    $"Vente fermée : la journée de visite est terminée (fin : {salesEnd:O}).");
            }
        }
    }
}
