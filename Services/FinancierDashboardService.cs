using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class FinancierDashboardService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEvenementDashboardService _evenementDashboardService;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<FinancierDashboardService> _logger;

        public FinancierDashboardService(
            CongoTravelDbContext context,
            ICurrentUserService currentUserService,
            IEvenementDashboardService evenementDashboardService,
            IPermissionService permissionService,
            ILogger<FinancierDashboardService> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _evenementDashboardService = evenementDashboardService;
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<FinancierDashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var societeIds = await ResolveScopedSocieteIdsAsync(cancellationToken);
                if (societeIds.Count == 0)
                {
                    _logger.LogWarning("Dashboard Financier: aucune société dans le scope utilisateur {UserId}", _currentUserService.UserId);
                    return EmptyDashboard();
                }

                var nowUtc = DateTime.UtcNow;
                var (_, monthStartUtc, _) = SocieteTransportMetricsHelper.GetUtcBoundaries(nowUtc);
                var nextMonthStartUtc = monthStartUtc.AddMonths(1);
                var previousMonthStartUtc = monthStartUtc.AddMonths(-1);

                var global = await BuildGlobalStatistiquesAsync(
                    societeIds, monthStartUtc, nextMonthStartUtc, previousMonthStartUtc, cancellationToken);

                var societesFinancieres = await BuildSocietesFinancieresAsync(
                    societeIds, monthStartUtc, nextMonthStartUtc, previousMonthStartUtc, cancellationToken);

                var transactionsRecentes = await GetTransactionsRecentesForSocietesAsync(societeIds, 10, cancellationToken);
                var alertesFinancieres = BuildAlertes(societesFinancieres);
                var tendances = await FinancierTransportMetricsHelper.BuildTendances12MoisAsync(
                    _context, societeIds, cancellationToken);

                var (collecteParOrigineGroupe, collecteOrigineGroupeSynthese) =
                    await CollecteOrigineGroupeMetricsHelper.GetCollecteParOrigineGroupeAsync(
                        _context, societeIds, monthStartUtc, nextMonthStartUtc, previousMonthStartUtc, cancellationToken: cancellationToken);

                var evenementStatistiques = await EvenementDashboardEnrichmentHelper.TryLoadWidgetForSocietesAsync(
                    _evenementDashboardService,
                    _permissionService,
                    _currentUserService,
                    societeIds,
                    cancellationToken);

                return new FinancierDashboardDto
                {
                    GlobalStatistiques = global,
                    SocietesFinancieres = societesFinancieres,
                    TransactionsRecentes = transactionsRecentes,
                    AlertesFinancieres = alertesFinancieres,
                    Tendances = tendances,
                    CollecteParOrigineGroupe = collecteParOrigineGroupe,
                    CollecteOrigineGroupeSynthese = collecteOrigineGroupeSynthese,
                    EvenementStatistiques = evenementStatistiques,
                    DateGeneration = nowUtc
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des données du dashboard Financier");
                return EmptyDashboard();
            }
        }

        /// <summary>Utilisé par SuperAdminDashboard (toutes sociétés si societeIds null).</summary>
        public async Task<List<TransactionRecenteDto>> GetTransactionsRecentesForSocietesAsync(
            IReadOnlyList<int>? societeIds,
            int take = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var scopedIds = societeIds;
                if (scopedIds == null)
                {
                    scopedIds = await _context.Societes.AsNoTracking()
                        .Where(s => s.Statut == true)
                        .Select(s => s.IdSociete)
                        .ToListAsync(cancellationToken);
                }

                if (scopedIds.Count == 0)
                    return new List<TransactionRecenteDto>();

                var paiementsRecents = await _context.Paiements
                    .AsNoTracking()
                    .Include(p => p.Reservation)
                        .ThenInclude(r => r!.Voyage)
                            .ThenInclude(v => v!.Destination)
                    .Include(p => p.Reservation)
                        .ThenInclude(r => r!.Client)
                    .Include(p => p.Reservation)
                        .ThenInclude(r => r!.Societe)
                    .Where(p => !p.IsDeleted && p.Statut && scopedIds.Contains(p.IdSociete))
                    .OrderByDescending(p => p.DateCreation)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                return paiementsRecents.Select(MapTransaction).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des transactions récentes");
                return new List<TransactionRecenteDto>();
            }
        }

        private async Task<List<int>> ResolveScopedSocieteIdsAsync(CancellationToken cancellationToken)
        {
            return await FinancierTransportMetricsHelper.ResolveSocieteIdsAsync(
                _context,
                _currentUserService.IsSuperAdmin,
                _currentUserService.SocieteId,
                cancellationToken);
        }

        private async Task<GlobalFinancierStatistiquesDto> BuildGlobalStatistiquesAsync(
            IReadOnlyList<int> societeIds,
            DateTime monthStartUtc,
            DateTime nextMonthStartUtc,
            DateTime previousMonthStartUtc,
            CancellationToken cancellationToken)
        {
            decimal caMois = 0m;
            decimal caMoisPrecedent = 0m;
            decimal montantNonPaye = 0m;
            var nombreTransactions = 0;
            var nombreReservations = 0;
            var nombreVoyages = 0;
            decimal sommeTauxRemplissage = 0m;

            foreach (var idSociete in societeIds)
            {
                var stats = await FinancierTransportMetricsHelper.GetSocieteFinancierStatsAsync(
                    _context, idSociete, monthStartUtc, nextMonthStartUtc, previousMonthStartUtc, cancellationToken: cancellationToken);

                caMois += stats.ChiffreAffairesMois;
                caMoisPrecedent += stats.ChiffreAffairesMoisPrecedent;
                montantNonPaye += stats.MontantReservationsNonPayees;
                nombreTransactions += stats.NombreTransactions;
                nombreReservations += stats.NombreReservations;
                nombreVoyages += stats.NombreVoyages;
                sommeTauxRemplissage += stats.TauxRemplissageMoyen;
            }

            var denominateur = caMois + montantNonPaye;
            var tauxGlobal = denominateur > 0m ? Math.Round((caMois / denominateur) * 100m, 2) : 0m;
            var tauxRemplissageMoyen = societeIds.Count > 0
                ? Math.Round(sommeTauxRemplissage / societeIds.Count, 2)
                : 0m;

            return new GlobalFinancierStatistiquesDto
            {
                ChiffreAffairesMois = caMois,
                ChiffreAffairesMoisPrecedent = caMoisPrecedent,
                VariationPourcentage = SocieteTransportMetricsHelper.ComputeVariationPercent(caMois, caMoisPrecedent),
                MontantReservationsNonPayees = montantNonPaye,
                TauxPaiementGlobal = tauxGlobal,
                NombreTotalTransactions = nombreTransactions,
                MoyenneTransaction = nombreTransactions > 0 ? Math.Round(caMois / nombreTransactions, 2) : 0m,
                NombreTotalReservations = nombreReservations,
                NombreTotalVoyages = nombreVoyages,
                TauxRemplissageMoyen = tauxRemplissageMoyen
            };
        }

        private async Task<List<SocieteFinancierSummaryDto>> BuildSocietesFinancieresAsync(
            IReadOnlyList<int> societeIds,
            DateTime monthStartUtc,
            DateTime nextMonthStartUtc,
            DateTime previousMonthStartUtc,
            CancellationToken cancellationToken)
        {
            var societes = await _context.Societes.AsNoTracking()
                .Where(s => societeIds.Contains(s.IdSociete))
                .ToListAsync(cancellationToken);

            var result = new List<SocieteFinancierSummaryDto>();

            foreach (var societe in societes)
            {
                try
                {
                    var stats = await FinancierTransportMetricsHelper.GetSocieteFinancierStatsAsync(
                        _context, societe.IdSociete, monthStartUtc, nextMonthStartUtc, previousMonthStartUtc, cancellationToken: cancellationToken);

                    var (collecteParOrigineGroupe, collecteOrigineGroupeSynthese) =
                        await CollecteOrigineGroupeMetricsHelper.GetCollecteParOrigineGroupeAsync(
                            _context,
                            societe.IdSociete,
                            monthStartUtc,
                            nextMonthStartUtc,
                            previousMonthStartUtc,
                            cancellationToken: cancellationToken);

                    result.Add(new SocieteFinancierSummaryDto
                    {
                        IdSociete = societe.IdSociete,
                        NomSociete = societe.Nom ?? string.Empty,
                        VilleSociete = societe.AdresseResidence,
                        CodeDevisePrincipale = societe.CodeDevisePrincipale,
                        ChiffreAffairesMois = stats.ChiffreAffairesMois,
                        MontantReservationsNonPayees = stats.MontantReservationsNonPayees,
                        TauxPaiement = stats.TauxPaiement,
                        NombreTransactions = stats.NombreTransactions,
                        NombreReservations = stats.NombreReservations,
                        NombreVoyages = stats.NombreVoyages,
                        StatutFinancier = FinancierTransportMetricsHelper.GetStatutFinancier(
                            stats.TauxPaiement, stats.MontantReservationsNonPayees),
                        TauxRemplissageMoyen = stats.TauxRemplissageMoyen,
                        CollecteParOrigineGroupe = collecteParOrigineGroupe,
                        CollecteOrigineGroupeSynthese = collecteOrigineGroupeSynthese
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors du traitement de la société {SocieteId}", societe.IdSociete);
                }
            }

            return result.OrderByDescending(s => s.ChiffreAffairesMois).ToList();
        }

        private static List<AlerteFinanciereDto> BuildAlertes(IReadOnlyList<SocieteFinancierSummaryDto> societes)
        {
            var alertes = new List<AlerteFinanciereDto>();
            var id = 1;

            foreach (var societe in societes)
            {
                if (societe.TauxPaiement < 70m)
                {
                    alertes.Add(new AlerteFinanciereDto
                    {
                        IdAlerte = id++,
                        TypeAlerte = "Taux de paiement faible",
                        Description = $"Taux de paiement critique pour {societe.NomSociete}: {societe.TauxPaiement:F1}%",
                        NiveauCriticite = societe.TauxPaiement < 50m ? "Élevée" : "Moyenne",
                        DateAlerte = DateTime.UtcNow,
                        IdSociete = societe.IdSociete,
                        NomSociete = societe.NomSociete,
                        MontantConcerne = societe.MontantReservationsNonPayees,
                        EstLue = false,
                        TypeAlerteTransport = "Paiement Transport",
                        NombreReservationsConcernees = societe.NombreReservations,
                        TauxConcerne = societe.TauxPaiement,
                        ActionSuggeree = "Contacter les clients pour finaliser les paiements de réservations"
                    });
                }

                if (societe.MontantReservationsNonPayees > 1_000_000m)
                {
                    alertes.Add(new AlerteFinanciereDto
                    {
                        IdAlerte = id++,
                        TypeAlerte = "Réservations non payées élevées",
                        Description = $"Montant de réservations non payées important pour {societe.NomSociete}: {societe.MontantReservationsNonPayees:N0}",
                        NiveauCriticite = societe.MontantReservationsNonPayees > 5_000_000m ? "Élevée" : "Moyenne",
                        DateAlerte = DateTime.UtcNow,
                        IdSociete = societe.IdSociete,
                        NomSociete = societe.NomSociete,
                        MontantConcerne = societe.MontantReservationsNonPayees,
                        EstLue = false,
                        TypeAlerteTransport = "Réservations Transport",
                        NombreReservationsConcernees = societe.NombreReservations,
                        TauxConcerne = societe.TauxPaiement,
                        ActionSuggeree = "Relancer les clients pour finaliser les paiements de réservations"
                    });
                }
            }

            return alertes.OrderByDescending(a => a.DateAlerte).ToList();
        }

        private static TransactionRecenteDto MapTransaction(Paiement p)
        {
            var montant = (p.MontantPayeDevisePrincipale ?? 0m) > 0m
                ? p.MontantPayeDevisePrincipale ?? 0m
                : (p.MontantPaye ?? 0m);

            return new TransactionRecenteDto
            {
                IdTransaction = p.IdPaiement,
                Reference = $"PAY-{p.IdPaiement:D6}",
                NomClient = p.Reservation?.Client?.NomClient ?? "Client inconnu",
                NomSociete = p.Reservation?.Societe?.Nom ?? "Société inconnue",
                Montant = montant,
                DateTransaction = p.DateCreation,
                TypeTransaction = "Paiement Transport",
                Statut = p.Statut ? "Validé" : "En attente",
                ReferenceReservation = p.IdReservation.HasValue ? $"RES-{p.IdReservation.Value:D6}" : "",
                VoyageInfo = p.Reservation?.Voyage != null
                    ? $"{p.Reservation.Voyage.Destination?.VilleDepart} - {p.Reservation.Voyage.Destination?.VilleArrivee}"
                    : "Voyage non spécifié",
                Destination = p.Reservation?.Voyage?.Destination?.VilleArrivee ?? "",
                DateVoyage = p.Reservation?.Voyage?.DateDepart ?? DateTime.MinValue,
                MethodePaiement = p.MethodePaiement ?? "Non spécifié"
            };
        }

        private static FinancierDashboardDto EmptyDashboard() => new()
        {
            GlobalStatistiques = new GlobalFinancierStatistiquesDto(),
            SocietesFinancieres = new List<SocieteFinancierSummaryDto>(),
            TransactionsRecentes = new List<TransactionRecenteDto>(),
            AlertesFinancieres = new List<AlerteFinanciereDto>(),
            Tendances = new TendancesFinancieresDto(),
            DateGeneration = DateTime.UtcNow
        };
    }
}
