using AutoMapper;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class SuperAdminDashboardService
    {
        private readonly CongoTravelDbContext _context;
        private readonly FinancierDashboardService _financierDashboardService;
        private readonly IEvenementDashboardService _evenementDashboardService;
        private readonly IReservationRepository _reservationRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<SuperAdminDashboardService> _logger;

        public SuperAdminDashboardService(
            CongoTravelDbContext context,
            FinancierDashboardService financierDashboardService,
            IEvenementDashboardService evenementDashboardService,
            IReservationRepository reservationRepository,
            IMapper mapper,
            ILogger<SuperAdminDashboardService> logger)
        {
            _context = context;
            _financierDashboardService = financierDashboardService;
            _evenementDashboardService = evenementDashboardService;
            _reservationRepository = reservationRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<SuperAdminDashboardTransportDto> GetDashboardDataAsync(
            PagedRequest? reservationsRequest = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                reservationsRequest ??= new PagedRequest();
                var nowUtc = DateTime.UtcNow;
                var (todayUtc, monthStartUtc, weekStartUtc) = SocieteTransportMetricsHelper.GetUtcBoundaries(nowUtc);
                var nextMonthStartUtc = monthStartUtc.AddMonths(1);
                var previousMonthStartUtc = monthStartUtc.AddMonths(-1);

                var societes = await _context.Societes.AsNoTracking().ToListAsync(cancellationToken);

                var global = new SuperAdminGlobalStatistiquesTransportDto
                {
                    TotalSocietes = societes.Count,
                    SocietesActives = societes.Count(s => s.Statut == true)
                };

                global.TotalClient = await _context.Clients.AsNoTracking()
                    .CountAsync(c => !c.IsDeleted.HasValue || !c.IsDeleted.Value, cancellationToken);

                global.TotalClientActif = await _context.Reservations.AsNoTracking()
                    .Where(r => r.Statut)
                    .Select(r => r.IdClient)
                    .Distinct()
                    .CountAsync(cancellationToken);

                global.TotalReservation = await _context.Reservations.AsNoTracking()
                    .CountAsync(r => r.Statut, cancellationToken);

                global.TotalVoyagesActifs = await _context.Voyages.AsNoTracking()
                    .CountAsync(v => v.Statut == true, cancellationToken);

                global.VoyagesAujourdhui = await _context.Voyages.AsNoTracking()
                    .CountAsync(v => v.Statut == true && v.DateDepart.Date == todayUtc, cancellationToken);

                global.VoyagesSemaine = await _context.Voyages.AsNoTracking()
                    .CountAsync(v => v.Statut == true && v.DateDepart.Date >= weekStartUtc && v.DateDepart.Date <= todayUtc, cancellationToken);

                global.TotalReservationsConfirmeesMois = await _context.Reservations.AsNoTracking()
                    .CountAsync(r => r.Statut && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation)
                        && r.DateReservation >= monthStartUtc, cancellationToken);

                global.TotalReservationsConfirmeesJour = await _context.Reservations.AsNoTracking()
                    .CountAsync(r => r.Statut && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation)
                        && r.DateReservation.Date == todayUtc, cancellationToken);

                global.TotalBilletsEmisMois = await _context.Billets.AsNoTracking()
                    .CountAsync(b => b.DateGeneration >= monthStartUtc, cancellationToken);

                var paiementsMois = await _context.Paiements.AsNoTracking()
                    .Where(p => !p.IsDeleted && p.Statut && p.DateCreation >= monthStartUtc)
                    .Select(p => new { p.MontantPayeDevisePrincipale, p.MontantPaye })
                    .ToListAsync(cancellationToken);

                global.NombreTransactionsMois = paiementsMois.Count;
                global.ChiffreAffairesMois = paiementsMois.Sum(p =>
                    (p.MontantPayeDevisePrincipale ?? 0m) > 0m
                        ? p.MontantPayeDevisePrincipale ?? 0m
                        : (p.MontantPaye ?? 0m));

                var societeSummaries = new List<SocieteTransportSummaryDto>();
                foreach (var societe in societes.Where(s => s.Statut == true))
                {
                    societeSummaries.Add(await BuildSocieteSummaryAsync(
                        societe.IdSociete,
                        societe.Nom ?? string.Empty,
                        societe.AdresseResidence,
                        societe.Statut == true,
                        societe.CodeDevisePrincipale,
                        monthStartUtc,
                        cancellationToken));
                }

                var top5 = societeSummaries
                    .OrderByDescending(s => s.ChiffreAffairesMois)
                    .Take(5)
                    .Select((s, index) => new SuperAdminTopSocieteCaDto
                    {
                        Rang = index + 1,
                        IdSociete = s.IdSociete,
                        Nom = s.Nom,
                        ChiffreAffairesMois = s.ChiffreAffairesMois,
                        CodeDevisePrincipale = s.CodeDevisePrincipale
                    })
                    .ToList();

                var transactions = await _financierDashboardService.GetTransactionsRecentesForSocietesAsync(null, 10, cancellationToken);

                var pagedReservations = await _reservationRepository.GetPagedAsync(reservationsRequest);
                var reservationDtos = pagedReservations.Data
                    .Select(r => _mapper.Map<ReservationResponseDto>(r))
                    .ToList();
                var reservations = new PagedResult<ReservationResponseDto>(
                    reservationDtos,
                    pagedReservations.TotalCount,
                    pagedReservations.PageNumber,
                    pagedReservations.PageSize);

                var (collecteParOrigineGroupe, collecteOrigineGroupeSynthese) =
                    await CollecteOrigineGroupeMetricsHelper.GetCollecteParOrigineGroupeAsync(
                        _context,
                        societeIds: null,
                        monthStartUtc,
                        nextMonthStartUtc,
                        previousMonthStartUtc,
                        cancellationToken: cancellationToken);

                var evenementStatistiques = await EvenementDashboardEnrichmentHelper.TryLoadSuperAdminWidgetAsync(
                    _evenementDashboardService,
                    cancellationToken);

                return new SuperAdminDashboardTransportDto
                {
                    GlobalStatistiques = global,
                    Societes = societeSummaries.OrderByDescending(s => s.ChiffreAffairesMois).ToList(),
                    Top5SocietesCa = top5,
                    TransactionsRecentes = transactions,
                    Reservations = reservations,
                    CollecteParOrigineGroupe = collecteParOrigineGroupe,
                    CollecteOrigineGroupeSynthese = collecteOrigineGroupeSynthese,
                    EvenementStatistiques = evenementStatistiques,
                    DateGeneration = nowUtc
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du dashboard SuperAdmin transport");
                throw;
            }
        }

        private async Task<SocieteTransportSummaryDto> BuildSocieteSummaryAsync(
            int idSociete,
            string nom,
            string? adresse,
            bool statut,
            string codeDevisePrincipale,
            DateTime monthStartUtc,
            CancellationToken cancellationToken)
        {
            var (voyagesMois, reservationsMois, billetsMois) =
                await SocieteTransportMetricsHelper.GetSocieteMonthlyCountsAsync(
                    _context, idSociete, monthStartUtc, cancellationToken: cancellationToken);

            var paiementsSociete = await _context.Paiements.AsNoTracking()
                .Where(p => p.IdSociete == idSociete && !p.IsDeleted && p.Statut && p.DateCreation >= monthStartUtc)
                .Select(p => new { p.MontantPayeDevisePrincipale, p.MontantPaye, p.DateCreation })
                .ToListAsync(cancellationToken);

            var caMois = paiementsSociete.Sum(p =>
                (p.MontantPayeDevisePrincipale ?? 0m) > 0m
                    ? p.MontantPayeDevisePrincipale ?? 0m
                    : (p.MontantPaye ?? 0m));

            var dernierPaiement = paiementsSociete.Count > 0
                ? paiementsSociete.Max(p => p.DateCreation)
                : (DateTime?)null;

            var derniereReservation = await _context.Reservations.AsNoTracking()
                .Where(r => r.IdSociete == idSociete)
                .OrderByDescending(r => r.DateReservation)
                .Select(r => (DateTime?)r.DateReservation)
                .FirstOrDefaultAsync(cancellationToken);

            DateTime? derniereActivite = null;
            if (dernierPaiement.HasValue && derniereReservation.HasValue)
                derniereActivite = dernierPaiement > derniereReservation ? dernierPaiement : derniereReservation;
            else
                derniereActivite = dernierPaiement ?? derniereReservation;

            return new SocieteTransportSummaryDto
            {
                IdSociete = idSociete,
                Nom = nom,
                Ville = adresse,
                Statut = statut,
                CodeDevisePrincipale = codeDevisePrincipale,
                VoyagesMois = voyagesMois,
                ReservationsConfirmeesMois = reservationsMois,
                BilletsEmisMois = billetsMois,
                ChiffreAffairesMois = caMois,
                DerniereActivite = derniereActivite
            };
        }
    }
}
