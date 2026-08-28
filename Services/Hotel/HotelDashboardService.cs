using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelDashboardService : IHotelDashboardService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<HotelDashboardService> _logger;

        public HotelDashboardService(
            CongoTravelDbContext context,
            ILogger<HotelDashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HotelDashboardResponseDto> GetSocieteDashboardAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var (todayUtc, _, _) = SocieteTransportMetricsHelper.GetUtcBoundaries(nowUtc);
            var monthStartDate = monthStartUtc.Date;
            var monthEndDate = monthEndUtc.Date;
            var hotels = _context.Hotels.AsNoTracking().Where(h => h.IdSociete == idSociete);
            var roomTypes = _context.HotelRoomTypes.AsNoTracking().Where(t => t.IdSociete == idSociete);
            var allotments = _context.HotelNightAllotments.AsNoTracking().Where(a => a.IdSociete == idSociete);
            var reservations = _context.HotelReservations.AsNoTracking().Where(r => r.IdSociete == idSociete);
            var payments =
                from p in _context.HotelPayments.AsNoTracking()
                join r in _context.HotelReservations.AsNoTracking()
                    on p.IdHotelReservation equals (int?)r.IdHotelReservation
                where r.IdSociete == idSociete
                select p;
            var succeededPayments = payments.Where(p =>
                p.Status == HotelPaymentStatus.SUCCEEDED
                && p.DateCreation >= monthStartUtc
                && p.DateCreation < monthEndUtc);

            var summary = new HotelDashboardSummaryDto
            {
                HotelsPublies = await hotels.CountAsync(
                    h => h.Status == HotelStatus.Published, cancellationToken),
                RoomTypesPublies = await roomTypes.CountAsync(
                    t => t.Status == HotelStatus.Published, cancellationToken),
                AllotmentsActifs = await allotments.CountAsync(a =>
                    a.Status == HotelStatus.Published
                    && a.NightDate >= monthStartDate
                    && a.NightDate < monthEndDate, cancellationToken),
                ReservationsConfirmeesMois = await reservations.CountAsync(r =>
                    r.Status == HotelReservationStatus.CONFIRMED
                    && (r.DateModification ?? r.DateCreation) >= monthStartUtc
                    && (r.DateModification ?? r.DateCreation) < monthEndUtc, cancellationToken),
                ReservationsConfirmeesJour = await reservations.CountAsync(r =>
                    r.Status == HotelReservationStatus.CONFIRMED
                    && (r.DateModification ?? r.DateCreation) >= todayUtc
                    && (r.DateModification ?? r.DateCreation) < todayUtc.AddDays(1), cancellationToken),
                MontantAcomptesSuccesMois = await succeededPayments
                    .SumAsync(p => (decimal?)p.Montant, cancellationToken) ?? 0m,
                HoldsEnCours = await reservations.CountAsync(r =>
                    r.Status == HotelReservationStatus.HOLD
                    && r.ExpiresAtUtc.HasValue
                    && r.ExpiresAtUtc > nowUtc, cancellationToken)
            };

            var breakdown = new HotelDashboardReservationBreakdownDto
            {
                Hold = await reservations.CountAsync(r => r.Status == HotelReservationStatus.HOLD, cancellationToken),
                Confirmed = await reservations.CountAsync(r => r.Status == HotelReservationStatus.CONFIRMED, cancellationToken),
                Cancelled = await reservations.CountAsync(r => r.Status == HotelReservationStatus.CANCELLED, cancellationToken),
                Expired = await reservations.CountAsync(r => r.Status == HotelReservationStatus.EXPIRED, cancellationToken)
            };

            var revenuParProvider = await succeededPayments
                .GroupBy(p => p.Provider)
                .Select(g => new HotelDashboardRevenueByProviderDto
                {
                    Provider = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Count()
                })
                .OrderByDescending(x => x.Montant)
                .ToListAsync(cancellationToken);

            var revenuParDevise = await succeededPayments
                .GroupBy(p => p.CodeDevise)
                .Select(g => new HotelDashboardRevenueByDeviseDto
                {
                    CodeDevise = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Count()
                })
                .OrderByDescending(x => x.Montant)
                .ToListAsync(cancellationToken);

            var topRaw = await (
                from p in _context.HotelPayments.AsNoTracking()
                join r in _context.HotelReservations.AsNoTracking()
                    on p.IdHotelReservation equals (int?)r.IdHotelReservation
                join h in _context.Hotels.AsNoTracking() on r.IdHotel equals h.IdHotel
                where r.IdSociete == idSociete
                    && r.Status == HotelReservationStatus.CONFIRMED
                    && p.Status == HotelPaymentStatus.SUCCEEDED
                    && p.DateCreation >= monthStartUtc
                    && p.DateCreation < monthEndUtc
                group new { p, r, h } by new { h.IdHotel, h.Nom, p.CodeDevise } into g
                orderby g.Sum(x => x.p.Montant) descending
                select new
                {
                    g.Key.IdHotel,
                    NomHotel = g.Key.Nom,
                    g.Key.CodeDevise,
                    ChiffreAffaires = g.Sum(x => x.p.Montant),
                    ReservationsConfirmees = g.Count(),
                    NuitsConfirmees = g.Sum(x => x.r.NombreNuits)
                }).Take(5).ToListAsync(cancellationToken);

            var topHotels = topRaw.Select((x, index) => new HotelDashboardTopHotelDto
            {
                Rang = index + 1,
                IdHotel = x.IdHotel,
                NomHotel = x.NomHotel,
                CodeDevise = x.CodeDevise,
                ChiffreAffaires = x.ChiffreAffaires,
                ReservationsConfirmees = x.ReservationsConfirmees,
                NuitsConfirmees = x.NuitsConfirmees
            }).ToList();

            var recentReservations = await (
                from r in _context.HotelReservations.AsNoTracking()
                join h in _context.Hotels.AsNoTracking() on r.IdHotel equals h.IdHotel
                where r.IdSociete == idSociete && r.Status == HotelReservationStatus.CONFIRMED
                orderby r.DateModification descending, r.DateCreation descending
                select new HotelDashboardRecentReservationDto
                {
                    IdHotelReservation = r.IdHotelReservation,
                    ReferenceReservation = r.ReferenceReservation,
                    Status = r.Status.ToString(),
                    MontantSousTotal = r.MontantSousTotal,
                    CodeDevise = r.CodeDevise,
                    NomHotel = h.Nom,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    DateCreation = r.DateCreation
                }).Take(10).ToListAsync(cancellationToken);

            var recentPayments = await (
                from p in _context.HotelPayments.AsNoTracking()
                join r in _context.HotelReservations.AsNoTracking()
                    on p.IdHotelReservation equals (int?)r.IdHotelReservation
                where r.IdSociete == idSociete
                orderby p.DateCreation descending
                select new HotelDashboardRecentPaymentDto
                {
                    IdHotelPayment = p.IdHotelPayment,
                    ReferencePaiement = p.ReferencePaiement,
                    Provider = p.Provider,
                    Status = p.Status.ToString(),
                    Montant = p.Montant,
                    CodeDevise = p.CodeDevise,
                    ReferenceReservation = r.ReferenceReservation,
                    DateCreation = p.DateCreation
                }).Take(10).ToListAsync(cancellationToken);

            var societe = await _context.Societes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSociete == idSociete, cancellationToken);
            _logger.LogInformation(
                "Dashboard hôtel généré — Société={IdSociete}, Période={Start:yyyy-MM}",
                idSociete, monthStartUtc);

            return new HotelDashboardResponseDto
            {
                IdSociete = idSociete,
                NomSociete = societe?.Nom ?? string.Empty,
                Summary = summary,
                Reservations = breakdown,
                RevenuParProvider = revenuParProvider,
                RevenuParDevise = revenuParDevise,
                Top5HotelsCa = topHotels,
                ReservationsRecentes = recentReservations,
                PaiementsRecents = recentPayments,
                PeriodeDebutUtc = monthStartUtc,
                PeriodeFinUtc = monthEndUtc
            };
        }

        public async Task<HotelSuperAdminDashboardResponseDto> GetSuperAdminDashboardAsync(
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var idsHotels = await _context.Hotels.AsNoTracking()
                .Select(h => h.IdSociete).Distinct().ToListAsync(cancellationToken);
            var idsReservations = await _context.HotelReservations.AsNoTracking()
                .Select(r => r.IdSociete).Distinct().ToListAsync(cancellationToken);
            var ids = idsHotels.Union(idsReservations).Distinct().ToList();
            var societes = await _context.Societes.AsNoTracking()
                .Where(s => ids.Contains(s.IdSociete))
                .OrderBy(s => s.Nom)
                .ToListAsync(cancellationToken);

            var summaries = new List<HotelDashboardSocieteSummaryDto>();
            foreach (var societe in societes)
            {
                var dashboard = await GetSocieteDashboardAsync(
                    societe.IdSociete, monthStartUtc, monthEndUtc, cancellationToken);
                summaries.Add(new HotelDashboardSocieteSummaryDto
                {
                    IdSociete = societe.IdSociete,
                    NomSociete = societe.Nom ?? string.Empty,
                    HotelsPublies = dashboard.Summary.HotelsPublies,
                    RoomTypesPublies = dashboard.Summary.RoomTypesPublies,
                    ReservationsConfirmeesMois = dashboard.Summary.ReservationsConfirmeesMois,
                    MontantAcomptes = dashboard.Summary.MontantAcomptesSuccesMois,
                    RevenuParDevise = dashboard.RevenuParDevise
                });
            }

            var globalRevenue = summaries.SelectMany(s => s.RevenuParDevise)
                .GroupBy(r => r.CodeDevise)
                .Select(g => new HotelDashboardRevenueByDeviseDto
                {
                    CodeDevise = g.Key,
                    Montant = g.Sum(x => x.Montant),
                    NombrePaiements = g.Sum(x => x.NombrePaiements)
                })
                .OrderByDescending(x => x.Montant)
                .ToList();

            return new HotelSuperAdminDashboardResponseDto
            {
                Global = new HotelDashboardGlobalSummaryDto
                {
                    TotalSocietesActives = summaries.Count,
                    HotelsPublies = summaries.Sum(s => s.HotelsPublies),
                    RoomTypesPublies = summaries.Sum(s => s.RoomTypesPublies),
                    ReservationsConfirmeesMois = summaries.Sum(s => s.ReservationsConfirmeesMois),
                    MontantAcomptes = summaries.Sum(s => s.MontantAcomptes),
                    RevenuParDevise = globalRevenue
                },
                Societes = summaries,
                PeriodeDebutUtc = monthStartUtc,
                PeriodeFinUtc = monthEndUtc
            };
        }

        public async Task<HotelDashboardWidgetDto> GetWidgetAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime monthEndUtc,
            CancellationToken cancellationToken = default)
        {
            var dashboard = await GetSocieteDashboardAsync(
                idSociete, monthStartUtc, monthEndUtc, cancellationToken);
            return new HotelDashboardWidgetDto
            {
                Summary = dashboard.Summary,
                RevenuParProvider = dashboard.RevenuParProvider,
                RevenuParDevise = dashboard.RevenuParDevise,
                TopHotelsCa = dashboard.Top5HotelsCa.Take(3).ToList()
            };
        }
    }
}
