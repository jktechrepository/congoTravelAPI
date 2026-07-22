using CongoTravel.Models.DTOs;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour calculer les statistiques du dashboard Admin société (transport).
    /// </summary>
    public class DashboardService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IEvenementDashboardService _evenementDashboardService;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            CongoTravelDbContext context,
            IEvenementDashboardService evenementDashboardService,
            IPermissionService permissionService,
            ICurrentUserService currentUserService,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _evenementDashboardService = evenementDashboardService;
            _permissionService = permissionService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<DashboardDto> GetDashboardDataAsync(int societeId)
        {
            try
            {
                var now = DateTime.UtcNow;
                var (todayUtc, monthStart, weekStartUtc) = SocieteTransportMetricsHelper.GetUtcBoundaries(now);
                var previousMonthStart = monthStart.AddMonths(-1);
                var nextMonthStart = monthStart.AddMonths(1);

                var societe = await _context.Societes.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.IdSociete == societeId);

                var totalAgents = await _context.Agents
                    .AsNoTracking()
                    .CountAsync(a => a.IdSociete == societeId && (a.Statut ?? false));

                var totalClientsActifs = await _context.Reservations
                    .AsNoTracking()
                    .Where(r => r.IdSociete == societeId && r.Statut)
                    .Select(r => r.IdClient)
                    .Distinct()
                    .Join(
                        _context.Clients.AsNoTracking().Where(c => c.Statut && c.IsActif && (!c.IsDeleted.HasValue || !c.IsDeleted.Value)),
                        idClient => idClient,
                        client => client.IdClient,
                        (idClient, _) => idClient)
                    .CountAsync();

                var paiementsMois = await _context.Paiements
                    .AsNoTracking()
                    .Where(p =>
                        p.IdSociete == societeId &&
                        !p.IsDeleted &&
                        p.Statut &&
                        p.DatePaiement >= monthStart &&
                        p.DatePaiement < nextMonthStart)
                    .SumAsync(p => (decimal?)(p.MontantPayeDevisePrincipale ?? p.MontantPaye) ?? 0m);

                var paiementsMoisPrecedent = await _context.Paiements
                    .AsNoTracking()
                    .Where(p =>
                        p.IdSociete == societeId &&
                        !p.IsDeleted &&
                        p.Statut &&
                        p.DatePaiement >= previousMonthStart &&
                        p.DatePaiement < monthStart)
                    .SumAsync(p => (decimal?)(p.MontantPayeDevisePrincipale ?? p.MontantPaye) ?? 0m);

                var transportStatistiques = await SocieteTransportMetricsHelper.GetSocieteTransportMetricsAsync(
                    _context, societeId, monthStart, todayUtc, weekStartUtc);
                var nombrePaiementsMois = await _context.Paiements
                    .AsNoTracking()
                    .CountAsync(p =>
                        p.IdSociete == societeId &&
                        !p.IsDeleted &&
                        p.Statut &&
                        p.DatePaiement >= monthStart &&
                        p.DatePaiement < nextMonthStart);

                var nombrePaiementsMoisPrecedent = await _context.Paiements
                    .AsNoTracking()
                    .CountAsync(p =>
                        p.IdSociete == societeId &&
                        !p.IsDeleted &&
                        p.Statut &&
                        p.DatePaiement >= previousMonthStart &&
                        p.DatePaiement < monthStart);

                var ticketMoyen = nombrePaiementsMois > 0 ? Math.Round(paiementsMois / nombrePaiementsMois, 2) : 0m;
                var ticketMoyenPrecedent = nombrePaiementsMoisPrecedent > 0
                    ? Math.Round(paiementsMoisPrecedent / nombrePaiementsMoisPrecedent, 2)
                    : 0m;

                var top5AgentsCollecteurs = await BuildTopAgentsCollecteursAsync(societeId, monthStart, nextMonthStart);

                var (collecteParOrigineGroupe, collecteOrigineGroupeSynthese) =
                    await CollecteOrigineGroupeMetricsHelper.GetCollecteParOrigineGroupeAsync(
                        _context, societeId, monthStart, nextMonthStart, previousMonthStart);

                var evenementStatistiques = await EvenementDashboardEnrichmentHelper.TryLoadWidgetAsync(
                    _evenementDashboardService,
                    _permissionService,
                    _currentUserService,
                    societeId);

                return new DashboardDto
                {
                    CodeDevisePrincipale = societe?.CodeDevisePrincipale ?? "CDF",
                    TotalAgents = totalAgents,
                    TotalClientsActifs = totalClientsActifs,
                    TransportStatistiques = transportStatistiques,
                    Top5AgentsCollecteurs = top5AgentsCollecteurs,
                    CollecteParOrigineGroupe = collecteParOrigineGroupe,
                    CollecteOrigineGroupeSynthese = collecteOrigineGroupeSynthese,
                    EvenementStatistiques = evenementStatistiques,
                    CollecteMois = new CollecteMoisDto
                    {
                        MoisLabel = monthStart.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("fr-FR")),
                        Montant = paiementsMois,
                        MontantMoisPrecedent = paiementsMoisPrecedent,
                        VariationPourcentage = SocieteTransportMetricsHelper.ComputeVariationPercent(paiementsMois, paiementsMoisPrecedent),
                        NombrePaiements = nombrePaiementsMois,
                        TicketMoyen = ticketMoyen,
                        VariationTicketMoyen = SocieteTransportMetricsHelper.ComputeVariationPercent(ticketMoyen, ticketMoyenPrecedent)
                    },
                    DateGeneration = now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data for society {SocieteId}", societeId);
                throw;
            }
        }

        private async Task<List<TopAgentCollecteurDto>> BuildTopAgentsCollecteursAsync(
            int societeId,
            DateTime monthStart,
            DateTime nextMonthStart)
        {
            var groupedByUser = await _context.Paiements
                .AsNoTracking()
                .Where(p =>
                    p.IdSociete == societeId &&
                    !p.IsDeleted &&
                    p.Statut &&
                    p.DatePaiement >= monthStart &&
                    p.DatePaiement < nextMonthStart)
                .GroupBy(p => p.IdUtilisateur)
                .Select(g => new
                {
                    IdUtilisateur = g.Key,
                    MontantCollecte = g.Sum(x => (decimal?)(x.MontantPayeDevisePrincipale ?? x.MontantPaye) ?? 0m),
                    NombrePaiements = g.Count()
                })
                .OrderByDescending(x => x.MontantCollecte)
                .ThenByDescending(x => x.NombrePaiements)
                .Take(20)
                .ToListAsync();

            if (groupedByUser.Count == 0)
                return new List<TopAgentCollecteurDto>();

            var userIds = groupedByUser.Select(x => x.IdUtilisateur).ToList();
            var users = await _context.Utilisateurs
                .AsNoTracking()
                .Include(u => u.Agent)
                .Where(u =>
                    userIds.Contains(u.IdUtilisateur) &&
                    u.IdSociete == societeId &&
                    u.IdAgent.HasValue &&
                    (u.Statut ?? false))
                .ToDictionaryAsync(u => u.IdUtilisateur);

            return groupedByUser
                .Where(x => users.ContainsKey(x.IdUtilisateur))
                .Select(x =>
                {
                    var user = users[x.IdUtilisateur];
                    return new TopAgentCollecteurDto
                    {
                        IdAgent = user.IdAgent ?? 0,
                        Matricule = user.Agent?.Matricule,
                        NomComplet = user.Agent?.NomComplet ?? user.NomComplet,
                        MontantCollecte = x.MontantCollecte,
                        NombrePaiements = x.NombrePaiements
                    };
                })
                .Where(x => x.IdAgent > 0)
                .OrderByDescending(x => x.MontantCollecte)
                .ThenByDescending(x => x.NombrePaiements)
                .Take(5)
                .ToList();
        }
    }
}
