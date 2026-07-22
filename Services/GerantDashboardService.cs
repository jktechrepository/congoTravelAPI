using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class GerantDashboardService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEvenementDashboardService _evenementDashboardService;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<GerantDashboardService> _logger;

        public GerantDashboardService(
            CongoTravelDbContext context,
            ICurrentUserService currentUserService,
            IEvenementDashboardService evenementDashboardService,
            IPermissionService permissionService,
            ILogger<GerantDashboardService> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _evenementDashboardService = evenementDashboardService;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<GerantDashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            var idSociete = _currentUserService.SocieteId;
            if (idSociete <= 0)
                throw new UnauthorizedAccessException("ID de société invalide");

            var idSite = _currentUserService.SiteId;
            if (idSite is > 0)
                return await GetDashboardDataForSiteAsync(idSociete, idSite.Value, cancellationToken);

            return await GetDashboardDataForSocieteAsync(idSociete, cancellationToken);
        }

        public Task<GerantDashboardDto> GetDashboardDataForSocieteAsync(
            int idSociete,
            CancellationToken cancellationToken = default) =>
            BuildDashboardAsync(idSociete, idSite: null, cancellationToken);

        public Task<GerantDashboardDto> GetDashboardDataForSiteAsync(
            int idSociete,
            int idSite,
            CancellationToken cancellationToken = default) =>
            BuildDashboardAsync(idSociete, idSite, cancellationToken);

        private async Task<GerantDashboardDto> BuildDashboardAsync(
            int idSociete,
            int? idSite,
            CancellationToken cancellationToken)
        {
            try
            {
                var nowUtc = DateTime.UtcNow;
                var (todayUtc, monthStartUtc, weekStartUtc) = SocieteTransportMetricsHelper.GetUtcBoundaries(nowUtc);
                var nextMonthStartUtc = monthStartUtc.AddMonths(1);
                var previousMonthStartUtc = monthStartUtc.AddMonths(-1);

                var societeStatistiques = await GetSocieteStatistiquesAsync(
                    idSociete, monthStartUtc, nextMonthStartUtc, previousMonthStartUtc, idSite, cancellationToken);
                var clientsStatistiques = await GetClientsStatistiquesAsync(
                    idSociete, monthStartUtc, nextMonthStartUtc, idSite, cancellationToken);
                var transportStatistiques = await SocieteTransportMetricsHelper.GetSocieteTransportMetricsAsync(
                    _context, idSociete, monthStartUtc, todayUtc, weekStartUtc, idSite, cancellationToken);
                var top5ClientsCA = await GerantTransportMetricsHelper.GetTop5ClientsCaAsync(
                    _context, idSociete, monthStartUtc, nextMonthStartUtc, idSite, cancellationToken);
                var top5ClientsNonPayes = await GerantTransportMetricsHelper.GetTop5ClientsNonPayesAsync(
                    _context, idSociete, idSite, cancellationToken);
                var tendances = await GerantTransportMetricsHelper.BuildTendancesGerantAsync(
                    _context, idSociete, idSite, cancellationToken);
                var paiementsStatistiques = await GerantTransportMetricsHelper.GetPaiementsStatistiquesAsync(
                    _context, idSociete, idSite, cancellationToken);
                var alertesSociete = await GerantTransportMetricsHelper.BuildAlertesTransportAsync(
                    _context, idSociete, societeStatistiques, idSite, cancellationToken);

                var (collecteParOrigineGroupe, collecteOrigineGroupeSynthese) =
                    await CollecteOrigineGroupeMetricsHelper.GetCollecteParOrigineGroupeAsync(
                        _context, idSociete, monthStartUtc, nextMonthStartUtc, previousMonthStartUtc, idSite, cancellationToken);

                var evenementStatistiques = await EvenementDashboardEnrichmentHelper.TryLoadWidgetAsync(
                    _evenementDashboardService,
                    _permissionService,
                    _currentUserService,
                    idSociete,
                    cancellationToken);

                return new GerantDashboardDto
                {
                    SocieteStatistiques = societeStatistiques,
                    ClientsStatistiques = clientsStatistiques,
                    TransportStatistiques = transportStatistiques,
                    Top5ClientsCA = top5ClientsCA,
                    Top5ClientsNonPayes = top5ClientsNonPayes,
                    AlertesSociete = alertesSociete,
                    Tendances = tendances,
                    PaiementsStatistiques = paiementsStatistiques,
                    CollecteParOrigineGroupe = collecteParOrigineGroupe,
                    CollecteOrigineGroupeSynthese = collecteOrigineGroupeSynthese,
                    EvenementStatistiques = evenementStatistiques,
                    DateGeneration = nowUtc
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors de la génération du dashboard Gérant pour la société {SocieteId} (site {IdSite})",
                    idSociete, idSite);
                throw;
            }
        }

        public async Task<SocieteStatistiquesDto> GetSocieteStatistiquesAsync(
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var (_, monthStartUtc, _) = SocieteTransportMetricsHelper.GetUtcBoundaries();
            return await GetSocieteStatistiquesAsync(
                idSociete,
                monthStartUtc,
                monthStartUtc.AddMonths(1),
                monthStartUtc.AddMonths(-1),
                idSite: null,
                cancellationToken);
        }

        private async Task<SocieteStatistiquesDto> GetSocieteStatistiquesAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime nextMonthStartUtc,
            DateTime previousMonthStartUtc,
            int? idSite,
            CancellationToken cancellationToken)
        {
            var societe = await _context.Societes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSociete == idSociete, cancellationToken);

            if (societe == null)
                return new SocieteStatistiquesDto();

            var stats = await FinancierTransportMetricsHelper.GetSocieteFinancierStatsAsync(
                _context, idSociete, monthStartUtc, nextMonthStartUtc, previousMonthStartUtc, idSite, cancellationToken);

            var clientIds = await GerantTransportMetricsHelper.GetSocieteClientIdsAsync(_context, idSociete, idSite, cancellationToken);
            var clientsActifs = await GerantTransportMetricsHelper.CountClientsActifsAsync(_context, idSociete, idSite, cancellationToken);

            return new SocieteStatistiquesDto
            {
                NomSociete = societe.Nom ?? string.Empty,
                VilleSociete = societe.AdresseResidence,
                CodeDevisePrincipale = societe.CodeDevisePrincipale,
                TotalClients = clientIds.Count,
                ClientsActifs = clientsActifs,
                ChiffreAffairesMois = stats.ChiffreAffairesMois,
                ChiffreAffairesMoisPrecedent = stats.ChiffreAffairesMoisPrecedent,
                VariationPourcentage = SocieteTransportMetricsHelper.ComputeVariationPercent(
                    stats.ChiffreAffairesMois, stats.ChiffreAffairesMoisPrecedent),
                MontantReservationsNonPayees = stats.MontantReservationsNonPayees,
                TauxPaiement = stats.TauxPaiement
            };
        }

        private async Task<ClientsStatistiquesDto> GetClientsStatistiquesAsync(
            int idSociete,
            DateTime monthStartUtc,
            DateTime nextMonthStartUtc,
            int? idSite,
            CancellationToken cancellationToken)
        {
            var clientIds = await GerantTransportMetricsHelper.GetSocieteClientIdsAsync(_context, idSociete, idSite, cancellationToken);
            var clientsActifs = await GerantTransportMetricsHelper.CountClientsActifsAsync(_context, idSociete, idSite, cancellationToken);

            var nouveauxClientsMois = await _context.Clients.AsNoTracking()
                .Where(c => clientIds.Contains(c.IdClient)
                    && c.DateCreation >= monthStartUtc && c.DateCreation < nextMonthStartUtc)
                .CountAsync(cancellationToken);

            var unpaidQuery = _context.Reservations.AsNoTracking()
                .Where(r => r.IdSociete == idSociete && r.Statut
                    && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation));

            if (idSite.HasValue)
                unpaidQuery = unpaidQuery.Where(r => r.IdSite == idSite.Value);

            var allUnpaidClientIds = await unpaidQuery
                .Where(r => !_context.Paiements.Any(p =>
                    p.IdReservation == r.IdReservation && p.Statut && !p.IsDeleted))
                .Select(r => r.IdClient)
                .Distinct()
                .CountAsync(cancellationToken);

            return new ClientsStatistiquesDto
            {
                TotalClients = clientIds.Count,
                ClientsActifs = clientsActifs,
                NouveauxClientsMois = nouveauxClientsMois,
                ClientsAvecReservationsNonPayees = allUnpaidClientIds
            };
        }
    }
}
