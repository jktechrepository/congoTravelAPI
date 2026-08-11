using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueDashboardService : ISiteTouristiqueDashboardService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<SiteTouristiqueDashboardService> _logger;

        public SiteTouristiqueDashboardService(
            CongoTravelDbContext context,
            ILogger<SiteTouristiqueDashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SiteTouristiqueDashboardResponseDto> GetSocieteDashboardAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var (todayUtc, _, _) = SocieteTransportMetricsHelper.GetUtcBoundaries(nowUtc);

            var societe = await _context.Societes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSociete == idSociete, cancellationToken);

            var sessionsQuery = _context.SiteTouristiqueJournees.AsNoTracking()
                .Where(s => s.IdSociete == idSociete);

            var reservationsQuery = _context.SiteTouristiqueReservations.AsNoTracking()
                .Where(r => r.IdSociete == idSociete);

            var ticketsQuery =
                from t in _context.SiteTouristiqueTickets.AsNoTracking()
                join l in _context.SiteTouristiqueReservationLines.AsNoTracking()
                    on t.IdSiteTouristiqueReservationLine equals l.IdSiteTouristiqueReservationLine
                join r in _context.SiteTouristiqueReservations.AsNoTracking()
                    on l.IdSiteTouristiqueReservation equals r.IdSiteTouristiqueReservation
                where r.IdSociete == idSociete
                select t;

            var paymentsQuery =
                from p in _context.SiteTouristiquePayments.AsNoTracking()
                join r in _context.SiteTouristiqueReservations.AsNoTracking()
                    on p.IdSiteTouristiqueReservation equals r.IdSiteTouristiqueReservation
                where r.IdSociete == idSociete
                select p;

            var summary = new SiteTouristiqueDashboardSummaryDto
            {
                JourneesPubliees = await sessionsQuery
                    .CountAsync(s => s.Status == SiteTouristiqueStatus.Published, cancellationToken),
                JourneesActives = await sessionsQuery.CountAsync(
                    s => s.Status == SiteTouristiqueStatus.Published
                         && s.DateVisite == DateOnly.FromDateTime(nowUtc),
                    cancellationToken),
                ReservationsConfirmeesMois = await reservationsQuery.CountAsync(
                    r => r.Status == SiteTouristiqueReservationStatus.CONFIRMED
                         && (r.DateModification ?? r.DateCreation) >= monthStartUtc
                         && (r.DateModification ?? r.DateCreation) < monthEndUtc,
                    cancellationToken),
                ReservationsConfirmeesJour = await reservationsQuery.CountAsync(
                    r => r.Status == SiteTouristiqueReservationStatus.CONFIRMED
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
                    r => r.Status == SiteTouristiqueReservationStatus.HOLD
                         && r.ExpiresAtUtc.HasValue
                         && r.ExpiresAtUtc > nowUtc,
                    cancellationToken)
            };

            var breakdown = new SiteTouristiqueDashboardReservationBreakdownDto
            {
                Hold = await reservationsQuery.CountAsync(r => r.Status == SiteTouristiqueReservationStatus.HOLD, cancellationToken),
                Confirmed = await reservationsQuery.CountAsync(r => r.Status == SiteTouristiqueReservationStatus.CONFIRMED, cancellationToken),
                Cancelled = await reservationsQuery.CountAsync(r => r.Status == SiteTouristiqueReservationStatus.CANCELLED, cancellationToken),
                Expired = await reservationsQuery.CountAsync(r => r.Status == SiteTouristiqueReservationStatus.EXPIRED, cancellationToken)
            };

            var succeededPaymentsInPeriod = paymentsQuery.Where(
                p => p.Status == SiteTouristiquePaymentStatus.SUCCEEDED
                     && p.DateCreation >= monthStartUtc
                     && p.DateCreation < monthEndUtc);

            var revenuParProvider = await succeededPaymentsInPeriod
                .GroupBy(p => p.Provider)
                .Select(g => new SiteTouristiqueDashboardRevenueByProviderDto
                {
                    Provider = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Count()
                })
                .OrderByDescending(x => x.Montant)
                .ToListAsync(cancellationToken);

            var revenuParDevise = await succeededPaymentsInPeriod
                .GroupBy(p => p.CodeDevise)
                .Select(g => new SiteTouristiqueDashboardRevenueByDeviseDto
                {
                    CodeDevise = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Count()
                })
                .OrderByDescending(x => x.Montant)
                .ToListAsync(cancellationToken);

            var topSessionsRaw = await (
                from p in _context.SiteTouristiquePayments.AsNoTracking()
                join r in _context.SiteTouristiqueReservations.AsNoTracking()
                    on p.IdSiteTouristiqueReservation equals r.IdSiteTouristiqueReservation
                join l in _context.SiteTouristiqueReservationLines.AsNoTracking()
                    on r.IdSiteTouristiqueReservation equals l.IdSiteTouristiqueReservation
                join s in _context.SiteTouristiqueJournees.AsNoTracking()
                    on r.IdSiteTouristiqueJournee equals s.IdSiteTouristiqueJournee
                join lieu in _context.SiteTouristiques.AsNoTracking()
                    on s.IdSiteTouristique equals lieu.IdSiteTouristique
                where r.IdSociete == idSociete
                      && r.Status == SiteTouristiqueReservationStatus.CONFIRMED
                      && p.Status == SiteTouristiquePaymentStatus.SUCCEEDED
                      && p.DateCreation >= monthStartUtc
                      && p.DateCreation < monthEndUtc
                group new { p, l, s, lieu } by new
                {
                    s.IdSiteTouristiqueJournee,
                    lieu.CodeLieu,
                    lieu.Nom,
                    p.CodeDevise
                }
                into g
                orderby g.Sum(x => x.p.Montant) descending
                select new
                {
                    g.Key.IdSiteTouristiqueJournee,
                    g.Key.CodeLieu,
                    NomLieu = g.Key.Nom,
                    g.Key.CodeDevise,
                    ChiffreAffaires = g.Sum(x => x.p.Montant),
                    TicketsVendus = g.Sum(x => x.l.Quantite)
                }).Take(5).ToListAsync(cancellationToken);

            var topJournees = topSessionsRaw
                .Select((x, index) => new SiteTouristiqueDashboardTopJourneeDto
                {
                    Rang = index + 1,
                    IdSiteTouristiqueJournee = x.IdSiteTouristiqueJournee,
                    CodeLieu = x.CodeLieu,
                    Libelle = x.NomLieu,
                    ChiffreAffaires = x.ChiffreAffaires,
                    CodeDevise = x.CodeDevise,
                    TicketsVendus = x.TicketsVendus
                })
                .ToList();

            var reservationsRecentes = await (
                from r in _context.SiteTouristiqueReservations.AsNoTracking()
                join s in _context.SiteTouristiqueJournees.AsNoTracking()
                    on r.IdSiteTouristiqueJournee equals s.IdSiteTouristiqueJournee
                join lieu in _context.SiteTouristiques.AsNoTracking()
                    on s.IdSiteTouristique equals lieu.IdSiteTouristique
                where r.IdSociete == idSociete && r.Status == SiteTouristiqueReservationStatus.CONFIRMED
                orderby r.DateModification descending, r.DateCreation descending
                select new SiteTouristiqueDashboardRecentReservationDto
                {
                    IdSiteTouristiqueReservation = r.IdSiteTouristiqueReservation,
                    ReferenceReservation = r.ReferenceReservation,
                    Status = r.Status.ToString(),
                    MontantSousTotal = r.MontantSousTotal,
                    CodeDevise = r.CodeDevise,
                    NomLieu = lieu.Nom,
                    DateCreation = r.DateCreation
                }).Take(10).ToListAsync(cancellationToken);

            var paiementsRecents = await (
                from p in _context.SiteTouristiquePayments.AsNoTracking()
                join r in _context.SiteTouristiqueReservations.AsNoTracking()
                    on p.IdSiteTouristiqueReservation equals r.IdSiteTouristiqueReservation
                where r.IdSociete == idSociete
                orderby p.DateCreation descending
                select new SiteTouristiqueDashboardRecentPaymentDto
                {
                    IdSiteTouristiquePayment = p.IdSiteTouristiquePayment,
                    ReferencePaiement = p.ReferencePaiement,
                    Provider = p.Provider,
                    Status = p.Status.ToString(),
                    Montant = p.Montant,
                    CodeDevise = p.CodeDevise,
                    ReferenceReservation = r.ReferenceReservation,
                    DateCreation = p.DateCreation
                }).Take(10).ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Dashboard site touristique généré — Société={IdSociete}, Période={Start:yyyy-MM}",
                idSociete,
                monthStartUtc);

            return new SiteTouristiqueDashboardResponseDto
            {
                IdSociete = idSociete,
                NomSociete = societe?.Nom ?? string.Empty,
                Summary = summary,
                Reservations = breakdown,
                RevenuParProvider = revenuParProvider,
                RevenuParDevise = revenuParDevise,
                Top5JourneesCa = topJournees,
                ReservationsRecentes = reservationsRecentes,
                PaiementsRecents = paiementsRecents,
                PeriodeDebutUtc = monthStartUtc,
                PeriodeFinUtc = monthEndUtc
            };
        }

        public async Task<SiteTouristiqueSuperAdminDashboardResponseDto> GetSuperAdminDashboardAsync(
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var societeIds = await _context.SiteTouristiqueJournees.AsNoTracking()
                .Select(s => s.IdSociete)
                .Distinct()
                .ToListAsync(cancellationToken);

            var societes = await _context.Societes.AsNoTracking()
                .Where(s => societeIds.Contains(s.IdSociete))
                .OrderBy(s => s.Nom)
                .ToListAsync(cancellationToken);

            var summaries = new List<SiteTouristiqueDashboardSocieteSummaryDto>();
            foreach (var societe in societes)
            {
                var dashboard = await GetSocieteDashboardAsync(
                    societe.IdSociete,
                    monthStartUtc,
                    monthEndUtc,
                    cancellationToken);

                summaries.Add(new SiteTouristiqueDashboardSocieteSummaryDto
                {
                    IdSociete = societe.IdSociete,
                    NomSociete = societe.Nom,
                    JourneesPubliees = dashboard.Summary.JourneesPubliees,
                    ReservationsConfirmeesMois = dashboard.Summary.ReservationsConfirmeesMois,
                    TicketsEmisMois = dashboard.Summary.TicketsEmisMois,
                    RevenuParDevise = dashboard.RevenuParDevise
                });
            }

            var globalRevenu = summaries
                .SelectMany(s => s.RevenuParDevise)
                .GroupBy(r => r.CodeDevise)
                .Select(g => new SiteTouristiqueDashboardRevenueByDeviseDto
                {
                    CodeDevise = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Sum(x => x.NombrePaiements)
                })
                .OrderByDescending(x => x.Montant)
                .ToList();

            return new SiteTouristiqueSuperAdminDashboardResponseDto
            {
                Global = new SiteTouristiqueDashboardGlobalSummaryDto
                {
                    TotalSocietesActives = summaries.Count,
                    JourneesPubliees = summaries.Sum(s => s.JourneesPubliees),
                    ReservationsConfirmeesMois = summaries.Sum(s => s.ReservationsConfirmeesMois),
                    TicketsEmisMois = summaries.Sum(s => s.TicketsEmisMois),
                    RevenuParDevise = globalRevenu
                },
                Societes = summaries,
                PeriodeDebutUtc = monthStartUtc,
                PeriodeFinUtc = monthEndUtc
            };
        }

        public async Task<SiteTouristiqueDashboardWidgetDto> GetWidgetAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var dashboard = await GetSocieteDashboardAsync(
                idSociete, monthStartUtc, monthEndUtc, cancellationToken);
            return MapToWidget(dashboard);
        }

        public async Task<SiteTouristiqueDashboardWidgetDto> GetWidgetForSocietesAsync(
            IReadOnlyList<int> idSocietes,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            if (idSocietes.Count == 0)
                return new SiteTouristiqueDashboardWidgetDto();

            if (idSocietes.Count == 1)
                return await GetWidgetAsync(idSocietes[0], monthStartUtc, monthEndUtc, cancellationToken);

            var widgets = new List<SiteTouristiqueDashboardWidgetDto>();
            foreach (var idSociete in idSocietes.Distinct())
            {
                widgets.Add(await GetWidgetAsync(idSociete, monthStartUtc, monthEndUtc, cancellationToken));
            }

            return MergeWidgets(widgets);
        }

        private static SiteTouristiqueDashboardWidgetDto MapToWidget(SiteTouristiqueDashboardResponseDto dashboard) =>
            new()
            {
                Summary = dashboard.Summary,
                RevenuParProvider = dashboard.RevenuParProvider,
                RevenuParDevise = dashboard.RevenuParDevise,
                TopJourneesCa = dashboard.Top5JourneesCa.Take(3).ToList()
            };

        private static SiteTouristiqueDashboardWidgetDto MergeWidgets(IReadOnlyList<SiteTouristiqueDashboardWidgetDto> widgets)
        {
            var summaries = widgets.Select(w => w.Summary).ToList();
            var mergedSummary = new SiteTouristiqueDashboardSummaryDto
            {
                JourneesPubliees = summaries.Sum(s => s.JourneesPubliees),
                JourneesActives = summaries.Sum(s => s.JourneesActives),
                HoldsEnCours = summaries.Sum(s => s.HoldsEnCours),
                ReservationsConfirmeesMois = summaries.Sum(s => s.ReservationsConfirmeesMois),
                ReservationsConfirmeesJour = summaries.Sum(s => s.ReservationsConfirmeesJour),
                TicketsEmisMois = summaries.Sum(s => s.TicketsEmisMois),
                TicketsUtilisesMois = summaries.Sum(s => s.TicketsUtilisesMois)
            };

            var revenuParProvider = widgets
                .SelectMany(w => w.RevenuParProvider)
                .GroupBy(r => r.Provider)
                .Select(g => new SiteTouristiqueDashboardRevenueByProviderDto
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
                .Select(g => new SiteTouristiqueDashboardRevenueByDeviseDto
                {
                    CodeDevise = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Sum(x => x.NombrePaiements)
                })
                .OrderByDescending(x => x.Montant)
                .ToList();

            var topJournees = widgets
                .SelectMany(w => w.TopJourneesCa)
                .OrderByDescending(s => s.ChiffreAffaires)
                .Take(3)
                .ToList();

            return new SiteTouristiqueDashboardWidgetDto
            {
                Summary = mergedSummary,
                RevenuParProvider = revenuParProvider,
                RevenuParDevise = revenuParDevise,
                TopJourneesCa = topJournees
            };
        }
    }
}
