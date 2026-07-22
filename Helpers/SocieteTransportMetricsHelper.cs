using CongoTravel.Data;
using CongoTravel.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Helpers
{
    public static class SocieteTransportMetricsHelper
    {
        public static readonly string[] StatutsReservationConfirmes = { "CONFIRMEE", "CONFIRME" };

        public static (DateTime TodayUtc, DateTime MonthStartUtc, DateTime WeekStartUtc) GetUtcBoundaries(DateTime? referenceUtc = null)
        {
            var nowUtc = referenceUtc ?? DateTime.UtcNow;
            var todayUtc = nowUtc.Date;
            var monthStartUtc = new DateTime(todayUtc.Year, todayUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var weekStartUtc = todayUtc.AddDays(-(int)todayUtc.DayOfWeek);
            return (todayUtc, monthStartUtc, weekStartUtc);
        }

        public static async Task<DashboardTransportStatistiquesDto> GetSocieteTransportMetricsAsync(
            CongoTravelDbContext context,
            int idSociete,
            DateTime monthStartUtc,
            DateTime todayUtc,
            DateTime weekStartUtc,
            int? idSite = null,
            CancellationToken cancellationToken = default)
        {
            var voyagesQuery = context.Voyages.AsNoTracking()
                .Where(v => v.IdSociete == idSociete);
            if (idSite.HasValue)
                voyagesQuery = voyagesQuery.Where(v => v.IdSite == idSite.Value);

            var reservationsQuery = context.Reservations.AsNoTracking()
                .Where(r => r.IdSociete == idSociete);
            if (idSite.HasValue)
                reservationsQuery = reservationsQuery.Where(r => r.IdSite == idSite.Value);

            var billetsQuery = context.Billets.AsNoTracking()
                .Where(b => b.IdSociete == idSociete);
            if (idSite.HasValue)
                billetsQuery = billetsQuery.Where(b => b.IdSite == idSite.Value);

            return new DashboardTransportStatistiquesDto
            {
                VoyagesActifs = await voyagesQuery
                    .CountAsync(v => v.Statut == true, cancellationToken),
                VoyagesAujourdhui = await voyagesQuery
                    .CountAsync(v => v.Statut == true && v.DateDepart.Date == todayUtc, cancellationToken),
                VoyagesSemaine = await voyagesQuery
                    .CountAsync(v => v.Statut == true
                        && v.DateDepart.Date >= weekStartUtc && v.DateDepart.Date <= todayUtc, cancellationToken),
                VoyagesMois = await voyagesQuery
                    .CountAsync(v => v.DateDepart >= monthStartUtc, cancellationToken),
                ReservationsConfirmeesMois = await reservationsQuery
                    .CountAsync(r => r.Statut
                        && StatutsReservationConfirmes.Contains(r.StatutReservation)
                        && r.DateReservation >= monthStartUtc, cancellationToken),
                ReservationsConfirmeesJour = await reservationsQuery
                    .CountAsync(r => r.Statut
                        && StatutsReservationConfirmes.Contains(r.StatutReservation)
                        && r.DateReservation.Date == todayUtc, cancellationToken),
                BilletsEmisMois = await billetsQuery
                    .CountAsync(b => b.DateGeneration >= monthStartUtc, cancellationToken)
            };
        }

        public static async Task<(int VoyagesMois, int ReservationsMois, int BilletsMois)> GetSocieteMonthlyCountsAsync(
            CongoTravelDbContext context,
            int idSociete,
            DateTime monthStartUtc,
            int? idSite = null,
            CancellationToken cancellationToken = default)
        {
            var voyagesQuery = context.Voyages.AsNoTracking()
                .Where(v => v.IdSociete == idSociete && v.DateDepart >= monthStartUtc);
            if (idSite.HasValue)
                voyagesQuery = voyagesQuery.Where(v => v.IdSite == idSite.Value);

            var reservationsQuery = context.Reservations.AsNoTracking()
                .Where(r => r.IdSociete == idSociete && r.Statut
                    && StatutsReservationConfirmes.Contains(r.StatutReservation)
                    && r.DateReservation >= monthStartUtc);
            if (idSite.HasValue)
                reservationsQuery = reservationsQuery.Where(r => r.IdSite == idSite.Value);

            var billetsQuery = context.Billets.AsNoTracking()
                .Where(b => b.IdSociete == idSociete && b.DateGeneration >= monthStartUtc);
            if (idSite.HasValue)
                billetsQuery = billetsQuery.Where(b => b.IdSite == idSite.Value);

            var voyagesMois = await voyagesQuery.CountAsync(cancellationToken);
            var reservationsMois = await reservationsQuery.CountAsync(cancellationToken);
            var billetsMois = await billetsQuery.CountAsync(cancellationToken);

            return (voyagesMois, reservationsMois, billetsMois);
        }

        public static decimal ComputeVariationPercent(decimal current, decimal previous)
        {
            if (previous <= 0m)
                return current > 0m ? 100m : 0m;

            return Math.Round(((current - previous) / previous) * 100m, 2);
        }
    }
}
