using System.Globalization;
using CongoTravel.Data;
using CongoTravel.Models;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Helpers
{
    public static class CaissierTransportMetricsHelper
    {
        public static string[] StatutsReservationConfirmes => SocieteTransportMetricsHelper.StatutsReservationConfirmes;

        public static (DateTime TodayUtc, DateTime MonthStartUtc, DateTime WeekStartUtc) GetUtcBoundaries(DateTime? referenceUtc = null) =>
            SocieteTransportMetricsHelper.GetUtcBoundaries(referenceUtc);

        public static decimal ResolveMontantPaye(Paiement p) =>
            (p.MontantPayeDevisePrincipale ?? 0m) > 0m
                ? p.MontantPayeDevisePrincipale ?? 0m
                : p.MontantPaye ?? 0m;

        /// <summary>Date métier d'encaissement — alignée sur les dashboards société (<c>DatePaiement</c>, repli <c>DateCreation</c>).</summary>
        public static DateTime ResolveDateEncaissement(Paiement p) =>
            p.DatePaiement != default ? p.DatePaiement : p.DateCreation;

        /// <summary>Filtre plage UTC sur la date d'encaissement effective.</summary>
        public static bool IsEncaissementInUtcRange(Paiement p, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            var d = EnsureUtc(ResolveDateEncaissement(p));
            var start = EnsureUtc(rangeStartUtc);
            var end = EnsureUtc(rangeEndUtc);
            return d >= start && d < end;
        }

        /// <summary>Filtre jour UTC sur la date d'encaissement effective.</summary>
        public static bool IsEncaissementOnUtcDay(Paiement p, DateTime dayStartUtc, DateTime dayEndUtc) =>
            IsEncaissementInUtcRange(p, dayStartUtc, dayEndUtc);

        public static string BuildPeriodeLibelle(DateTime monthStartUtc) =>
            monthStartUtc.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("fr-FR"));

        public readonly record struct CaissierRecettesParMethode(
            decimal Espece,
            decimal MobileMoney,
            decimal Virement,
            decimal Carte,
            decimal Autre);

        public static CaissierRecettesParMethode BuildRecettesParMethode(IEnumerable<Paiement> paiements)
        {
            decimal SumBucket(MethodePaiementHelper.RecetteBucket bucket) =>
                paiements
                    .Where(p => MethodePaiementHelper.GetRecetteBucket(p.MethodePaiement) == bucket)
                    .Sum(ResolveMontantPaye);

            return new CaissierRecettesParMethode(
                SumBucket(MethodePaiementHelper.RecetteBucket.Espece),
                SumBucket(MethodePaiementHelper.RecetteBucket.MobileMoney),
                SumBucket(MethodePaiementHelper.RecetteBucket.Virement),
                SumBucket(MethodePaiementHelper.RecetteBucket.Carte),
                SumBucket(MethodePaiementHelper.RecetteBucket.Autre));
        }

        private static DateTime EnsureUtc(DateTime value) =>
            value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

        /// <summary>
        /// Taux basé sur les places vendues par le caissier / capacité des voyages concernés (une fois par voyage).
        /// </summary>
        public static decimal ComputeTauxRemplissageCaissierJour(IReadOnlyList<Reservation> reservationsJour)
        {
            if (reservationsJour.Count == 0)
                return 0m;

            var placesVendues = reservationsJour.Sum(r => r.NombreDePlace > 0 ? r.NombreDePlace : 1);

            var capaciteTotale = reservationsJour
                .Where(r => r.Voyage?.Vehicule != null)
                .GroupBy(r => r.IdVoyage)
                .Sum(g => g.First().Voyage!.Vehicule!.NombreSiege);

            return capaciteTotale > 0
                ? Math.Round(placesVendues / (decimal)capaciteTotale * 100m, 2)
                : 0m;
        }

        public static decimal ResolveMontantVoyage(Voyage? voyage) =>
            voyage == null
                ? 0m
                : voyage.PrixDevisePrincipale > 0m
                    ? voyage.PrixDevisePrincipale
                    : voyage.Prix;

        public static async Task<string> GetCodeDevisePrincipaleAsync(
            CongoTravelDbContext context,
            int societeId,
            CancellationToken cancellationToken = default)
        {
            var code = await context.Societes.AsNoTracking()
                .Where(s => s.IdSociete == societeId)
                .Select(s => s.CodeDevisePrincipale)
                .FirstOrDefaultAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(code) ? "CDF" : code;
        }

        /// <summary>Somme des recettes des deux destinations les plus encaissées du jour.</summary>
        public static decimal ComputeRecetteDestinationPrincipale(IEnumerable<Paiement> paiementsJour) =>
            paiementsJour
                .Where(p => p.Reservation?.Voyage?.Destination != null)
                .GroupBy(p => p.Reservation!.Voyage!.Destination!.VilleArrivee ?? string.Empty)
                .Select(g => g.Sum(ResolveMontantPaye))
                .OrderByDescending(m => m)
                .Take(2)
                .Sum();
    }
}
