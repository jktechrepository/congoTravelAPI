using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement
{
    public class EvenementDashboardService : IEvenementDashboardService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<EvenementDashboardService> _logger;

        public EvenementDashboardService(
            CongoTravelDbContext context,
            ILogger<EvenementDashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EvenementDashboardResponseDto> GetSocieteDashboardAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var (todayUtc, _, _) = SocieteTransportMetricsHelper.GetUtcBoundaries(nowUtc);

            var societe = await _context.Societes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSociete == idSociete, cancellationToken);

            var sessionsQuery = _context.EvenementSessions.AsNoTracking()
                .Where(s => s.IdSociete == idSociete);

            var reservationsQuery = _context.EvenementReservations.AsNoTracking()
                .Where(r => r.IdSociete == idSociete);

            var ticketsQuery =
                from t in _context.EvenementTickets.AsNoTracking()
                join l in _context.EvenementReservationLines.AsNoTracking()
                    on t.IdEvenementReservationLine equals l.IdEvenementReservationLine
                join r in _context.EvenementReservations.AsNoTracking()
                    on l.IdEvenementReservation equals r.IdEvenementReservation
                where r.IdSociete == idSociete
                select t;

            var paymentsQuery =
                from p in _context.EvenementPayments.AsNoTracking()
                join r in _context.EvenementReservations.AsNoTracking()
                    on p.IdEvenementReservation equals r.IdEvenementReservation
                where r.IdSociete == idSociete
                select p;

            var summary = new EvenementDashboardSummaryDto
            {
                SessionsPubliees = await sessionsQuery
                    .CountAsync(s => s.Status == EvenementSessionStatus.Published, cancellationToken),
                SessionsActives = await sessionsQuery.CountAsync(
                    s => s.Status == EvenementSessionStatus.Published
                         && s.StartAtUtc <= nowUtc
                         && (s.EndAtUtc == null || s.EndAtUtc >= nowUtc),
                    cancellationToken),
                ReservationsConfirmeesMois = await reservationsQuery.CountAsync(
                    r => r.Status == EvenementReservationStatus.CONFIRMED
                         && (r.DateModification ?? r.DateCreation) >= monthStartUtc
                         && (r.DateModification ?? r.DateCreation) < monthEndUtc,
                    cancellationToken),
                ReservationsConfirmeesJour = await reservationsQuery.CountAsync(
                    r => r.Status == EvenementReservationStatus.CONFIRMED
                         && (r.DateModification ?? r.DateCreation) >= todayUtc
                         && (r.DateModification ?? r.DateCreation) < todayUtc.AddDays(1),
                    cancellationToken),
                TicketsEmisMois = await ticketsQuery.CountAsync(
                    t => t.IssuedAtUtc >= monthStartUtc && t.IssuedAtUtc < monthEndUtc,
                    cancellationToken),
                TicketsUtilisesMois = await ticketsQuery.CountAsync(
                    t => t.UsedAtUtc.HasValue
                         && t.UsedAtUtc >= monthStartUtc
                         && t.UsedAtUtc < monthEndUtc,
                    cancellationToken),
                HoldsEnCours = await reservationsQuery.CountAsync(
                    r => r.Status == EvenementReservationStatus.HOLD
                         && r.ExpiresAtUtc.HasValue
                         && r.ExpiresAtUtc > nowUtc,
                    cancellationToken)
            };

            var breakdown = new EvenementDashboardReservationBreakdownDto
            {
                Hold = await reservationsQuery.CountAsync(r => r.Status == EvenementReservationStatus.HOLD, cancellationToken),
                Confirmed = await reservationsQuery.CountAsync(r => r.Status == EvenementReservationStatus.CONFIRMED, cancellationToken),
                Cancelled = await reservationsQuery.CountAsync(r => r.Status == EvenementReservationStatus.CANCELLED, cancellationToken),
                Expired = await reservationsQuery.CountAsync(r => r.Status == EvenementReservationStatus.EXPIRED, cancellationToken)
            };

            var succeededPaymentsInPeriod = paymentsQuery.Where(
                p => p.Status == EvenementPaymentStatus.SUCCEEDED
                     && p.DateCreation >= monthStartUtc
                     && p.DateCreation < monthEndUtc);

            var revenuParProvider = await succeededPaymentsInPeriod
                .GroupBy(p => p.Provider)
                .Select(g => new EvenementDashboardRevenueByProviderDto
                {
                    Provider = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Count()
                })
                .OrderByDescending(x => x.Montant)
                .ToListAsync(cancellationToken);

            var revenuParDevise = await succeededPaymentsInPeriod
                .GroupBy(p => p.CodeDevise)
                .Select(g => new EvenementDashboardRevenueByDeviseDto
                {
                    CodeDevise = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Count()
                })
                .OrderByDescending(x => x.Montant)
                .ToListAsync(cancellationToken);

            var topSessionsRaw = await (
                from p in _context.EvenementPayments.AsNoTracking()
                join r in _context.EvenementReservations.AsNoTracking()
                    on p.IdEvenementReservation equals r.IdEvenementReservation
                join l in _context.EvenementReservationLines.AsNoTracking()
                    on r.IdEvenementReservation equals l.IdEvenementReservation
                join s in _context.EvenementSessions.AsNoTracking()
                    on r.IdEvenementSession equals s.IdEvenementSession
                where r.IdSociete == idSociete
                      && r.Status == EvenementReservationStatus.CONFIRMED
                      && p.Status == EvenementPaymentStatus.SUCCEEDED
                      && p.DateCreation >= monthStartUtc
                      && p.DateCreation < monthEndUtc
                group new { p, l, s } by new
                {
                    s.IdEvenementSession,
                    s.CodeSession,
                    s.Libelle,
                    p.CodeDevise
                }
                into g
                orderby g.Sum(x => x.p.Montant) descending
                select new
                {
                    g.Key.IdEvenementSession,
                    g.Key.CodeSession,
                    g.Key.Libelle,
                    g.Key.CodeDevise,
                    ChiffreAffaires = g.Sum(x => x.p.Montant),
                    TicketsVendus = g.Sum(x => x.l.Quantite)
                }).Take(5).ToListAsync(cancellationToken);

            var topSessions = topSessionsRaw
                .Select((x, index) => new EvenementDashboardTopSessionDto
                {
                    Rang = index + 1,
                    IdEvenementSession = x.IdEvenementSession,
                    CodeSession = x.CodeSession,
                    Libelle = x.Libelle,
                    ChiffreAffaires = x.ChiffreAffaires,
                    CodeDevise = x.CodeDevise,
                    TicketsVendus = x.TicketsVendus
                })
                .ToList();

            var reservationsRecentes = await (
                from r in _context.EvenementReservations.AsNoTracking()
                join s in _context.EvenementSessions.AsNoTracking()
                    on r.IdEvenementSession equals s.IdEvenementSession
                where r.IdSociete == idSociete && r.Status == EvenementReservationStatus.CONFIRMED
                orderby r.DateModification descending, r.DateCreation descending
                select new EvenementDashboardRecentReservationDto
                {
                    IdEvenementReservation = r.IdEvenementReservation,
                    ReferenceReservation = r.ReferenceReservation,
                    Status = r.Status.ToString(),
                    MontantSousTotal = r.MontantSousTotal,
                    CodeDevise = r.CodeDevise,
                    SessionLibelle = s.Libelle,
                    DateCreation = r.DateCreation
                }).Take(10).ToListAsync(cancellationToken);

            var paiementsRecents = await (
                from p in _context.EvenementPayments.AsNoTracking()
                join r in _context.EvenementReservations.AsNoTracking()
                    on p.IdEvenementReservation equals r.IdEvenementReservation
                where r.IdSociete == idSociete
                orderby p.DateCreation descending
                select new EvenementDashboardRecentPaymentDto
                {
                    IdEvenementPayment = p.IdEvenementPayment,
                    ReferencePaiement = p.ReferencePaiement,
                    Provider = p.Provider,
                    Status = p.Status.ToString(),
                    Montant = p.Montant,
                    CodeDevise = p.CodeDevise,
                    ReferenceReservation = r.ReferenceReservation,
                    DateCreation = p.DateCreation
                }).Take(10).ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Dashboard événement généré — Société={IdSociete}, Période={Start:yyyy-MM}",
                idSociete,
                monthStartUtc);

            return new EvenementDashboardResponseDto
            {
                IdSociete = idSociete,
                NomSociete = societe?.Nom ?? string.Empty,
                Summary = summary,
                Reservations = breakdown,
                RevenuParProvider = revenuParProvider,
                RevenuParDevise = revenuParDevise,
                Top5SessionsCa = topSessions,
                ReservationsRecentes = reservationsRecentes,
                PaiementsRecents = paiementsRecents,
                PeriodeDebutUtc = monthStartUtc,
                PeriodeFinUtc = monthEndUtc
            };
        }

        public async Task<EvenementSuperAdminDashboardResponseDto> GetSuperAdminDashboardAsync(
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var societeIds = await _context.EvenementSessions.AsNoTracking()
                .Select(s => s.IdSociete)
                .Distinct()
                .ToListAsync(cancellationToken);

            var societes = await _context.Societes.AsNoTracking()
                .Where(s => societeIds.Contains(s.IdSociete))
                .OrderBy(s => s.Nom)
                .ToListAsync(cancellationToken);

            var summaries = new List<EvenementDashboardSocieteSummaryDto>();
            foreach (var societe in societes)
            {
                var dashboard = await GetSocieteDashboardAsync(
                    societe.IdSociete,
                    monthStartUtc,
                    monthEndUtc,
                    cancellationToken);

                summaries.Add(new EvenementDashboardSocieteSummaryDto
                {
                    IdSociete = societe.IdSociete,
                    NomSociete = societe.Nom,
                    SessionsPubliees = dashboard.Summary.SessionsPubliees,
                    ReservationsConfirmeesMois = dashboard.Summary.ReservationsConfirmeesMois,
                    TicketsEmisMois = dashboard.Summary.TicketsEmisMois,
                    RevenuParDevise = dashboard.RevenuParDevise
                });
            }

            var globalRevenu = summaries
                .SelectMany(s => s.RevenuParDevise)
                .GroupBy(r => r.CodeDevise)
                .Select(g => new EvenementDashboardRevenueByDeviseDto
                {
                    CodeDevise = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Sum(x => x.NombrePaiements)
                })
                .OrderByDescending(x => x.Montant)
                .ToList();

            return new EvenementSuperAdminDashboardResponseDto
            {
                Global = new EvenementDashboardGlobalSummaryDto
                {
                    TotalSocietesActives = summaries.Count,
                    SessionsPubliees = summaries.Sum(s => s.SessionsPubliees),
                    ReservationsConfirmeesMois = summaries.Sum(s => s.ReservationsConfirmeesMois),
                    TicketsEmisMois = summaries.Sum(s => s.TicketsEmisMois),
                    RevenuParDevise = globalRevenu
                },
                Societes = summaries,
                PeriodeDebutUtc = monthStartUtc,
                PeriodeFinUtc = monthEndUtc
            };
        }

        public async Task<EvenementDashboardWidgetDto> GetWidgetAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var dashboard = await GetSocieteDashboardAsync(
                idSociete, monthStartUtc, monthEndUtc, cancellationToken);
            return MapToWidget(dashboard);
        }

        public async Task<EvenementDashboardWidgetDto> GetWidgetForSocietesAsync(
            IReadOnlyList<int> idSocietes,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            if (idSocietes.Count == 0)
                return new EvenementDashboardWidgetDto();

            if (idSocietes.Count == 1)
                return await GetWidgetAsync(idSocietes[0], monthStartUtc, monthEndUtc, cancellationToken);

            var widgets = new List<EvenementDashboardWidgetDto>();
            foreach (var idSociete in idSocietes.Distinct())
            {
                widgets.Add(await GetWidgetAsync(idSociete, monthStartUtc, monthEndUtc, cancellationToken));
            }

            return MergeWidgets(widgets);
        }

        private static EvenementDashboardWidgetDto MapToWidget(EvenementDashboardResponseDto dashboard) =>
            new()
            {
                Summary = dashboard.Summary,
                RevenuParProvider = dashboard.RevenuParProvider,
                RevenuParDevise = dashboard.RevenuParDevise,
                TopSessionsCa = dashboard.Top5SessionsCa.Take(3).ToList()
            };

        private static EvenementDashboardWidgetDto MergeWidgets(IReadOnlyList<EvenementDashboardWidgetDto> widgets)
        {
            var summaries = widgets.Select(w => w.Summary).ToList();
            var mergedSummary = new EvenementDashboardSummaryDto
            {
                SessionsPubliees = summaries.Sum(s => s.SessionsPubliees),
                SessionsActives = summaries.Sum(s => s.SessionsActives),
                HoldsEnCours = summaries.Sum(s => s.HoldsEnCours),
                ReservationsConfirmeesMois = summaries.Sum(s => s.ReservationsConfirmeesMois),
                ReservationsConfirmeesJour = summaries.Sum(s => s.ReservationsConfirmeesJour),
                TicketsEmisMois = summaries.Sum(s => s.TicketsEmisMois),
                TicketsUtilisesMois = summaries.Sum(s => s.TicketsUtilisesMois)
            };

            var revenuParProvider = widgets
                .SelectMany(w => w.RevenuParProvider)
                .GroupBy(r => r.Provider)
                .Select(g => new EvenementDashboardRevenueByProviderDto
                {
                    Provider = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Sum(x => x.NombrePaiements)
                })
                .OrderByDescending(x => x.Montant)
                .ToList();

            var revenuParDevise = widgets
                .SelectMany(w => w.RevenuParDevise)
                .GroupBy(r => r.CodeDevise)
                .Select(g => new EvenementDashboardRevenueByDeviseDto
                {
                    CodeDevise = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Sum(x => x.NombrePaiements)
                })
                .OrderByDescending(x => x.Montant)
                .ToList();

            var topSessions = widgets
                .SelectMany(w => w.TopSessionsCa)
                .OrderByDescending(s => s.ChiffreAffaires)
                .Take(3)
                .ToList();

            return new EvenementDashboardWidgetDto
            {
                Summary = mergedSummary,
                RevenuParProvider = revenuParProvider,
                RevenuParDevise = revenuParDevise,
                TopSessionsCa = topSessions
            };
        }
    }
}
