using CongoTravel.Data;
using CongoTravel.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Helpers
{
    public class SocieteFinancierStatsResult
    {
        public decimal ChiffreAffairesMois { get; set; }
        public decimal ChiffreAffairesMoisPrecedent { get; set; }
        public decimal MontantReservationsNonPayees { get; set; }
        public decimal TauxPaiement { get; set; }
        public int NombreTransactions { get; set; }
        public int NombreReservations { get; set; }
        public int NombreVoyages { get; set; }
        public decimal TauxRemplissageMoyen { get; set; }
        public int NombreReservationsNonPayees { get; set; }
    }

    public static class FinancierTransportMetricsHelper
    {
        public static async Task<List<int>> ResolveSocieteIdsAsync(
            CongoTravelDbContext context,
            bool isSuperAdmin,
            int societeIdToken,
            CancellationToken cancellationToken = default)
        {
            if (isSuperAdmin)
            {
                return await context.Societes.AsNoTracking()
                    .Where(s => s.Statut == true)
                    .Select(s => s.IdSociete)
                    .ToListAsync(cancellationToken);
            }

            if (societeIdToken <= 0)
                return new List<int>();

            return new List<int> { societeIdToken };
        }

        public static async Task<SocieteFinancierStatsResult> GetSocieteFinancierStatsAsync(
            CongoTravelDbContext context,
            int idSociete,
            DateTime monthStartUtc,
            DateTime nextMonthStartUtc,
            DateTime previousMonthStartUtc,
            int? idSite = null,
            CancellationToken cancellationToken = default)
        {
            var paiementsMoisQuery = context.Paiements.AsNoTracking()
                .Where(p => p.IdSociete == idSociete && !p.IsDeleted && p.Statut
                    && p.DatePaiement >= monthStartUtc && p.DatePaiement < nextMonthStartUtc);
            if (idSite.HasValue)
                paiementsMoisQuery = paiementsMoisQuery.Where(p => p.IdSite == idSite.Value);

            var paiementsMois = await paiementsMoisQuery
                .Select(p => new { p.MontantPayeDevisePrincipale, p.MontantPaye })
                .ToListAsync(cancellationToken);

            var caMois = paiementsMois.Sum(p =>
                (p.MontantPayeDevisePrincipale ?? 0m) > 0m
                    ? p.MontantPayeDevisePrincipale ?? 0m
                    : (p.MontantPaye ?? 0m));

            var paiementsMoisPrecedentQuery = context.Paiements.AsNoTracking()
                .Where(p => p.IdSociete == idSociete && !p.IsDeleted && p.Statut
                    && p.DatePaiement >= previousMonthStartUtc && p.DatePaiement < monthStartUtc);
            if (idSite.HasValue)
                paiementsMoisPrecedentQuery = paiementsMoisPrecedentQuery.Where(p => p.IdSite == idSite.Value);

            var paiementsMoisPrecedent = await paiementsMoisPrecedentQuery
                .Select(p => new { p.MontantPayeDevisePrincipale, p.MontantPaye })
                .ToListAsync(cancellationToken);

            var caMoisPrecedent = paiementsMoisPrecedent.Sum(p =>
                (p.MontantPayeDevisePrincipale ?? 0m) > 0m
                    ? p.MontantPayeDevisePrincipale ?? 0m
                    : (p.MontantPaye ?? 0m));

            var reservationsNonPayeesQuery = context.Reservations.AsNoTracking()
                .Include(r => r.Voyage)
                .Where(r => r.IdSociete == idSociete && r.Statut
                    && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation));
            if (idSite.HasValue)
                reservationsNonPayeesQuery = reservationsNonPayeesQuery.Where(r => r.IdSite == idSite.Value);

            var reservationsNonPayees = await reservationsNonPayeesQuery
                .Where(r => !context.Paiements.Any(p =>
                    p.IdReservation == r.IdReservation && p.Statut && !p.IsDeleted))
                .ToListAsync(cancellationToken);

            var montantNonPaye = reservationsNonPayees.Sum(r =>
                (r.Voyage?.Prix ?? 0m) * r.NombreDePlace);

            var denominateur = caMois + montantNonPaye;
            var tauxPaiement = denominateur > 0m ? Math.Round((caMois / denominateur) * 100m, 2) : 0m;

            var (_, reservationsMois, _) = await SocieteTransportMetricsHelper.GetSocieteMonthlyCountsAsync(
                context, idSociete, monthStartUtc, idSite, cancellationToken);

            var voyagesQuery = context.Voyages.AsNoTracking()
                .Where(v => v.IdSociete == idSociete && v.DateDepart >= monthStartUtc
                    && v.DateDepart < nextMonthStartUtc);
            if (idSite.HasValue)
                voyagesQuery = voyagesQuery.Where(v => v.IdSite == idSite.Value);

            var nombreVoyages = await voyagesQuery.CountAsync(cancellationToken);

            var tauxRemplissage = await ComputeTauxRemplissageMoisAsync(
                context, idSociete, monthStartUtc, nextMonthStartUtc, idSite, cancellationToken);

            return new SocieteFinancierStatsResult
            {
                ChiffreAffairesMois = caMois,
                ChiffreAffairesMoisPrecedent = caMoisPrecedent,
                MontantReservationsNonPayees = montantNonPaye,
                TauxPaiement = tauxPaiement,
                NombreTransactions = paiementsMois.Count,
                NombreReservations = reservationsMois,
                NombreVoyages = nombreVoyages,
                TauxRemplissageMoyen = tauxRemplissage,
                NombreReservationsNonPayees = reservationsNonPayees.Count
            };
        }

        public static async Task<decimal> ComputeTauxRemplissageMoisAsync(
            CongoTravelDbContext context,
            int idSociete,
            DateTime monthStartUtc,
            DateTime nextMonthStartUtc,
            int? idSite = null,
            CancellationToken cancellationToken = default)
        {
            var query = context.Reservations.AsNoTracking()
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Vehicule)
                .Where(r => r.IdSociete == idSociete && r.Statut
                    && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation)
                    && r.DateReservation >= monthStartUtc && r.DateReservation < nextMonthStartUtc);

            if (idSite.HasValue)
                query = query.Where(r => r.IdSite == idSite.Value);

            var reservationsMois = await query.ToListAsync(cancellationToken);

            if (reservationsMois.Count == 0)
                return 0m;

            var placesReservees = reservationsMois.Sum(r => r.NombreDePlace);
            var capaciteTotale = reservationsMois
                .Where(r => r.Voyage?.Vehicule != null)
                .GroupBy(r => r.IdVoyage)
                .Sum(g => g.First().Voyage!.Vehicule!.NombreSiege);

            return capaciteTotale > 0
                ? Math.Round((decimal)placesReservees / capaciteTotale * 100m, 2)
                : 0m;
        }

        public static async Task<TendancesFinancieresDto> BuildTendances12MoisAsync(
            CongoTravelDbContext context,
            IReadOnlyList<int> societeIds,
            CancellationToken cancellationToken = default)
        {
            var revenus = new List<TendanceMensuelleDto>();
            var encaissements = new List<TendanceMensuelleDto>();
            var tauxPaiement = new List<TendanceMensuelleDto>();
            var nbReservations = new List<TendanceMensuelleDto>();
            var nbVoyages = new List<TendanceMensuelleDto>();

            var nowUtc = DateTime.UtcNow;
            for (var i = 11; i >= 0; i--)
            {
                var dateRef = nowUtc.AddMonths(-i);
                var monthStart = new DateTime(dateRef.Year, dateRef.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var nextMonthStart = monthStart.AddMonths(1);
                var moisLabel = monthStart.ToString("MMM yyyy");

                decimal caTotal = 0m;
                var transactions = 0;
                var reservations = 0;
                var voyages = 0;
                var reservationsPayees = 0;
                var reservationsMoisTotal = 0;

                foreach (var idSociete in societeIds)
                {
                    var stats = await GetSocieteFinancierStatsAsync(
                        context, idSociete, monthStart, nextMonthStart, monthStart.AddMonths(-1), cancellationToken: cancellationToken);

                    caTotal += stats.ChiffreAffairesMois;
                    transactions += stats.NombreTransactions;
                    reservations += stats.NombreReservations;
                    voyages += stats.NombreVoyages;

                    var reservationsMois = await context.Reservations.AsNoTracking()
                        .Where(r => r.IdSociete == idSociete && r.Statut
                            && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation)
                            && r.DateReservation >= monthStart && r.DateReservation < nextMonthStart)
                        .Select(r => r.IdReservation)
                        .ToListAsync(cancellationToken);

                    reservationsMoisTotal += reservationsMois.Count;
                    if (reservationsMois.Count > 0)
                    {
                        reservationsPayees += await context.Paiements.AsNoTracking()
                            .CountAsync(p => p.IdReservation.HasValue
                                && reservationsMois.Contains(p.IdReservation.Value)
                                && p.Statut && !p.IsDeleted, cancellationToken);
                    }
                }

                var taux = reservationsMoisTotal > 0
                    ? Math.Round((decimal)reservationsPayees / reservationsMoisTotal * 100m, 2)
                    : 0m;

                revenus.Add(new TendanceMensuelleDto { Mois = moisLabel, Annee = monthStart.Year, Valeur = caTotal });
                encaissements.Add(new TendanceMensuelleDto { Mois = moisLabel, Annee = monthStart.Year, Valeur = caTotal });
                tauxPaiement.Add(new TendanceMensuelleDto { Mois = moisLabel, Annee = monthStart.Year, Valeur = taux });
                nbReservations.Add(new TendanceMensuelleDto { Mois = moisLabel, Annee = monthStart.Year, Valeur = reservations });
                nbVoyages.Add(new TendanceMensuelleDto { Mois = moisLabel, Annee = monthStart.Year, Valeur = voyages });
            }

            return new TendancesFinancieresDto
            {
                RevenusTransport = revenus,
                Encaissements = encaissements,
                TauxPaiement = tauxPaiement,
                NombreReservations = nbReservations,
                NombreVoyages = nbVoyages
            };
        }

        public static string GetStatutFinancier(decimal tauxPaiement, decimal montantNonPaye)
        {
            if (tauxPaiement >= 90m && montantNonPaye < 100_000m) return "Excellent";
            if (tauxPaiement >= 80m && montantNonPaye < 500_000m) return "Bon";
            if (tauxPaiement >= 70m && montantNonPaye < 1_000_000m) return "Moyen";
            return "Critique";
        }
    }
}
