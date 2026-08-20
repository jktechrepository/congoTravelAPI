using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantDashboardService : IRestaurantDashboardService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<RestaurantDashboardService> _logger;

        public RestaurantDashboardService(
            CongoTravelDbContext context,
            ILogger<RestaurantDashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RestaurantDashboardResponseDto> GetSocieteDashboardAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var (todayUtc, _, _) = SocieteTransportMetricsHelper.GetUtcBoundaries(nowUtc);
            var monthStartDate = DateOnly.FromDateTime(monthStartUtc);
            var monthEndDate = DateOnly.FromDateTime(monthEndUtc);

            var societe = await _context.Societes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSociete == idSociete, cancellationToken);

            var etablissementsQuery = _context.Restaurants.AsNoTracking()
                .Where(r => r.IdSociete == idSociete);

            var creneauxQuery = _context.RestaurantCreneaux.AsNoTracking()
                .Where(c => c.IdSociete == idSociete);

            var reservationsQuery = _context.RestaurantReservations.AsNoTracking()
                .Where(r => r.IdSociete == idSociete);

            var paymentsQuery =
                from p in _context.RestaurantPayments.AsNoTracking()
                join r in _context.RestaurantReservations.AsNoTracking()
                    on p.IdRestaurantReservation equals (int?)r.IdRestaurantReservation
                where r.IdSociete == idSociete
                select p;

            var succeededPaymentsInPeriod = paymentsQuery.Where(
                p => p.Status == RestaurantPaymentStatus.SUCCEEDED
                     && p.DateCreation >= monthStartUtc
                     && p.DateCreation < monthEndUtc);

            var summary = new RestaurantDashboardSummaryDto
            {
                EtablissementsPublies = await etablissementsQuery
                    .CountAsync(r => r.Status == RestaurantStatus.Published, cancellationToken),
                CreneauxPublies = await creneauxQuery
                    .CountAsync(c => c.Status == RestaurantStatus.Published, cancellationToken),
                CreneauxActifs = await creneauxQuery.CountAsync(
                    c => c.Status == RestaurantStatus.Published
                         && (
                             (c.DateService >= monthStartDate && c.DateService < monthEndDate)
                             || (c.StartAtUtc < monthEndUtc && c.EndAtUtc > monthStartUtc)
                         ),
                    cancellationToken),
                ReservationsConfirmeesMois = await reservationsQuery.CountAsync(
                    r => r.Status == RestaurantReservationStatus.CONFIRMED
                         && (r.DateModification ?? r.DateCreation) >= monthStartUtc
                         && (r.DateModification ?? r.DateCreation) < monthEndUtc,
                    cancellationToken),
                ReservationsConfirmeesJour = await reservationsQuery.CountAsync(
                    r => r.Status == RestaurantReservationStatus.CONFIRMED
                         && (r.DateModification ?? r.DateCreation) >= todayUtc
                         && (r.DateModification ?? r.DateCreation) < todayUtc.AddDays(1),
                    cancellationToken),
                MontantAcomptesSuccesMois = await succeededPaymentsInPeriod
                    .SumAsync(p => (decimal?)p.Montant, cancellationToken) ?? 0m,
                HoldsEnCours = await reservationsQuery.CountAsync(
                    r => r.Status == RestaurantReservationStatus.HOLD
                         && r.ExpiresAtUtc.HasValue
                         && r.ExpiresAtUtc > nowUtc,
                    cancellationToken)
            };

            var breakdown = new RestaurantDashboardReservationBreakdownDto
            {
                Hold = await reservationsQuery.CountAsync(
                    r => r.Status == RestaurantReservationStatus.HOLD, cancellationToken),
                Confirmed = await reservationsQuery.CountAsync(
                    r => r.Status == RestaurantReservationStatus.CONFIRMED, cancellationToken),
                Cancelled = await reservationsQuery.CountAsync(
                    r => r.Status == RestaurantReservationStatus.CANCELLED, cancellationToken),
                Expired = await reservationsQuery.CountAsync(
                    r => r.Status == RestaurantReservationStatus.EXPIRED, cancellationToken)
            };

            var revenuParProvider = await succeededPaymentsInPeriod
                .GroupBy(p => p.Provider)
                .Select(g => new RestaurantDashboardRevenueByProviderDto
                {
                    Provider = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Count()
                })
                .OrderByDescending(x => x.Montant)
                .ToListAsync(cancellationToken);

            var revenuParDevise = await succeededPaymentsInPeriod
                .GroupBy(p => p.CodeDevise)
                .Select(g => new RestaurantDashboardRevenueByDeviseDto
                {
                    CodeDevise = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Count()
                })
                .OrderByDescending(x => x.Montant)
                .ToListAsync(cancellationToken);

            var topCreneauxRaw = await (
                from p in _context.RestaurantPayments.AsNoTracking()
                join r in _context.RestaurantReservations.AsNoTracking()
                    on p.IdRestaurantReservation equals (int?)r.IdRestaurantReservation
                join c in _context.RestaurantCreneaux.AsNoTracking()
                    on r.IdRestaurantCreneau equals c.IdRestaurantCreneau
                join resto in _context.Restaurants.AsNoTracking()
                    on r.IdRestaurant equals resto.IdRestaurant
                where r.IdSociete == idSociete
                      && r.Status == RestaurantReservationStatus.CONFIRMED
                      && p.Status == RestaurantPaymentStatus.SUCCEEDED
                      && p.DateCreation >= monthStartUtc
                      && p.DateCreation < monthEndUtc
                group new { p, r, c, resto } by new
                {
                    c.IdRestaurantCreneau,
                    resto.Nom,
                    c.DateService,
                    c.StartAtUtc,
                    p.CodeDevise
                }
                into g
                orderby g.Sum(x => x.p.Montant) descending
                select new
                {
                    g.Key.IdRestaurantCreneau,
                    NomRestaurant = g.Key.Nom,
                    g.Key.DateService,
                    g.Key.StartAtUtc,
                    g.Key.CodeDevise,
                    ChiffreAffaires = g.Sum(x => x.p.Montant),
                    CouvertsConfirmes = g.Sum(x => x.r.NombreCouverts)
                }).Take(5).ToListAsync(cancellationToken);

            var topCreneaux = topCreneauxRaw
                .Select((x, index) => new RestaurantDashboardTopCreneauDto
                {
                    Rang = index + 1,
                    IdRestaurantCreneau = x.IdRestaurantCreneau,
                    NomRestaurant = x.NomRestaurant,
                    DateService = x.DateService,
                    StartAtUtc = x.StartAtUtc,
                    ChiffreAffaires = x.ChiffreAffaires,
                    CodeDevise = x.CodeDevise,
                    CouvertsConfirmes = x.CouvertsConfirmes
                })
                .ToList();

            var reservationsRecentes = await (
                from r in _context.RestaurantReservations.AsNoTracking()
                join resto in _context.Restaurants.AsNoTracking()
                    on r.IdRestaurant equals resto.IdRestaurant
                where r.IdSociete == idSociete && r.Status == RestaurantReservationStatus.CONFIRMED
                orderby r.DateModification descending, r.DateCreation descending
                select new RestaurantDashboardRecentReservationDto
                {
                    IdRestaurantReservation = r.IdRestaurantReservation,
                    ReferenceReservation = r.ReferenceReservation,
                    Status = r.Status.ToString(),
                    MontantSousTotal = r.MontantSousTotal,
                    CodeDevise = r.CodeDevise,
                    NomRestaurant = resto.Nom,
                    NombreCouverts = r.NombreCouverts,
                    DateCreation = r.DateCreation
                }).Take(10).ToListAsync(cancellationToken);

            var paiementsRecents = await (
                from p in _context.RestaurantPayments.AsNoTracking()
                join r in _context.RestaurantReservations.AsNoTracking()
                    on p.IdRestaurantReservation equals (int?)r.IdRestaurantReservation
                where r.IdSociete == idSociete
                orderby p.DateCreation descending
                select new RestaurantDashboardRecentPaymentDto
                {
                    IdRestaurantPayment = p.IdRestaurantPayment,
                    ReferencePaiement = p.ReferencePaiement,
                    Provider = p.Provider,
                    Status = p.Status.ToString(),
                    Montant = p.Montant,
                    CodeDevise = p.CodeDevise,
                    ReferenceReservation = r.ReferenceReservation,
                    DateCreation = p.DateCreation
                }).Take(10).ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Dashboard restaurant généré — Société={IdSociete}, Période={Start:yyyy-MM}",
                idSociete,
                monthStartUtc);

            return new RestaurantDashboardResponseDto
            {
                IdSociete = idSociete,
                NomSociete = societe?.Nom ?? string.Empty,
                Summary = summary,
                Reservations = breakdown,
                RevenuParProvider = revenuParProvider,
                RevenuParDevise = revenuParDevise,
                Top5CreneauxCa = topCreneaux,
                ReservationsRecentes = reservationsRecentes,
                PaiementsRecents = paiementsRecents,
                PeriodeDebutUtc = monthStartUtc,
                PeriodeFinUtc = monthEndUtc
            };
        }

        public async Task<RestaurantSuperAdminDashboardResponseDto> GetSuperAdminDashboardAsync(
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var societeIdsFromCreneaux = await _context.RestaurantCreneaux.AsNoTracking()
                .Select(c => c.IdSociete)
                .Distinct()
                .ToListAsync(cancellationToken);

            var societeIdsFromEtablissements = await _context.Restaurants.AsNoTracking()
                .Select(r => r.IdSociete)
                .Distinct()
                .ToListAsync(cancellationToken);

            var societeIds = societeIdsFromCreneaux
                .Union(societeIdsFromEtablissements)
                .Distinct()
                .ToList();

            var societes = await _context.Societes.AsNoTracking()
                .Where(s => societeIds.Contains(s.IdSociete))
                .OrderBy(s => s.Nom)
                .ToListAsync(cancellationToken);

            var summaries = new List<RestaurantDashboardSocieteSummaryDto>();
            foreach (var societe in societes)
            {
                var dashboard = await GetSocieteDashboardAsync(
                    societe.IdSociete,
                    monthStartUtc,
                    monthEndUtc,
                    cancellationToken);

                summaries.Add(new RestaurantDashboardSocieteSummaryDto
                {
                    IdSociete = societe.IdSociete,
                    NomSociete = societe.Nom ?? string.Empty,
                    EtablissementsPublies = dashboard.Summary.EtablissementsPublies,
                    CreneauxPublies = dashboard.Summary.CreneauxPublies,
                    ReservationsConfirmeesMois = dashboard.Summary.ReservationsConfirmeesMois,
                    MontantAcomptes = dashboard.Summary.MontantAcomptesSuccesMois,
                    RevenuParDevise = dashboard.RevenuParDevise
                });
            }

            var globalRevenu = summaries
                .SelectMany(s => s.RevenuParDevise)
                .GroupBy(r => r.CodeDevise)
                .Select(g => new RestaurantDashboardRevenueByDeviseDto
                {
                    CodeDevise = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Sum(x => x.NombrePaiements)
                })
                .OrderByDescending(x => x.Montant)
                .ToList();

            return new RestaurantSuperAdminDashboardResponseDto
            {
                Global = new RestaurantDashboardGlobalSummaryDto
                {
                    TotalSocietesActives = summaries.Count,
                    EtablissementsPublies = summaries.Sum(s => s.EtablissementsPublies),
                    CreneauxPublies = summaries.Sum(s => s.CreneauxPublies),
                    ReservationsConfirmeesMois = summaries.Sum(s => s.ReservationsConfirmeesMois),
                    MontantAcomptes = summaries.Sum(s => s.MontantAcomptes),
                    RevenuParDevise = globalRevenu
                },
                Societes = summaries,
                PeriodeDebutUtc = monthStartUtc,
                PeriodeFinUtc = monthEndUtc
            };
        }

        public async Task<RestaurantDashboardWidgetDto> GetWidgetAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var dashboard = await GetSocieteDashboardAsync(
                idSociete, monthStartUtc, monthEndUtc, cancellationToken);
            return MapToWidget(dashboard);
        }

        public async Task<RestaurantDashboardWidgetDto> GetWidgetForSocietesAsync(
            IReadOnlyList<int> idSocietes,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            if (idSocietes.Count == 0)
                return new RestaurantDashboardWidgetDto();

            if (idSocietes.Count == 1)
                return await GetWidgetAsync(idSocietes[0], monthStartUtc, monthEndUtc, cancellationToken);

            var widgets = new List<RestaurantDashboardWidgetDto>();
            foreach (var idSociete in idSocietes.Distinct())
            {
                widgets.Add(await GetWidgetAsync(idSociete, monthStartUtc, monthEndUtc, cancellationToken));
            }

            return MergeWidgets(widgets);
        }

        private static RestaurantDashboardWidgetDto MapToWidget(RestaurantDashboardResponseDto dashboard) =>
            new()
            {
                Summary = dashboard.Summary,
                RevenuParProvider = dashboard.RevenuParProvider,
                RevenuParDevise = dashboard.RevenuParDevise,
                TopCreneauxCa = dashboard.Top5CreneauxCa.Take(3).ToList()
            };

        private static RestaurantDashboardWidgetDto MergeWidgets(IReadOnlyList<RestaurantDashboardWidgetDto> widgets)
        {
            var summaries = widgets.Select(w => w.Summary).ToList();
            var mergedSummary = new RestaurantDashboardSummaryDto
            {
                EtablissementsPublies = summaries.Sum(s => s.EtablissementsPublies),
                CreneauxPublies = summaries.Sum(s => s.CreneauxPublies),
                CreneauxActifs = summaries.Sum(s => s.CreneauxActifs),
                HoldsEnCours = summaries.Sum(s => s.HoldsEnCours),
                ReservationsConfirmeesMois = summaries.Sum(s => s.ReservationsConfirmeesMois),
                ReservationsConfirmeesJour = summaries.Sum(s => s.ReservationsConfirmeesJour),
                MontantAcomptesSuccesMois = summaries.Sum(s => s.MontantAcomptesSuccesMois)
            };

            var revenuParProvider = widgets
                .SelectMany(w => w.RevenuParProvider)
                .GroupBy(r => r.Provider)
                .Select(g => new RestaurantDashboardRevenueByProviderDto
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
                .Select(g => new RestaurantDashboardRevenueByDeviseDto
                {
                    CodeDevise = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Sum(x => x.NombrePaiements)
                })
                .OrderByDescending(x => x.Montant)
                .ToList();

            var topCreneaux = widgets
                .SelectMany(w => w.TopCreneauxCa)
                .OrderByDescending(s => s.ChiffreAffaires)
                .Take(3)
                .ToList();

            return new RestaurantDashboardWidgetDto
            {
                Summary = mergedSummary,
                RevenuParProvider = revenuParProvider,
                RevenuParDevise = revenuParDevise,
                TopCreneauxCa = topCreneaux
            };
        }
    }
}
