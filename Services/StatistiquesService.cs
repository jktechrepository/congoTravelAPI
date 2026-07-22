using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Statistiques;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class StatistiquesService : IStatistiquesService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<StatistiquesService> _logger;

        public StatistiquesService(
            CongoTravelDbContext context,
            ILogger<StatistiquesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<StatistiquesTransportDto> GetStatistiquesAsync(
            int idSociete,
            DateTime? debut = null,
            DateTime? fin = null,
            CancellationToken cancellationToken = default)
        {
            var societe = await _context.Societes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSociete == idSociete && s.Statut == true, cancellationToken);

            if (societe == null)
            {
                _logger.LogWarning("Société {SocieteId} introuvable pour Statistiques", idSociete);
                throw new KeyNotFoundException($"Société {idSociete} introuvable");
            }

            var (debutUtc, finUtc, libellePeriode) = StatistiquesTransportMetricsHelper.ResolvePeriodeUtc(debut, fin);
            var (_, monthStartUtc, weekStartUtc) = SocieteTransportMetricsHelper.GetUtcBoundaries();
            var todayUtc = monthStartUtc;
            var confirmed = SocieteTransportMetricsHelper.StatutsReservationConfirmes;

            _logger.LogInformation(
                "Génération Statistiques transport société {SocieteId} période {Periode}",
                idSociete, libellePeriode);

            var (ca, montantDu, tauxPaiement, nbPaiements) =
                await StatistiquesTransportMetricsHelper.GetPeriodeFinanciereAsync(
                    _context, idSociete, debutUtc, finUtc, cancellationToken);

            var totalClients = await _context.Reservations.AsNoTracking()
                .Where(r => r.IdSociete == idSociete && r.Statut
                    && r.DateReservation >= debutUtc && r.DateReservation < finUtc)
                .Select(r => r.IdClient)
                .Distinct()
                .CountAsync(cancellationToken);

            var totalReservations = await _context.Reservations.AsNoTracking()
                .CountAsync(r => r.IdSociete == idSociete && r.Statut
                    && confirmed.Contains(r.StatutReservation)
                    && r.DateReservation >= debutUtc && r.DateReservation < finUtc,
                    cancellationToken);

            var totalVoyages = await _context.Voyages.AsNoTracking()
                .CountAsync(v => v.IdSociete == idSociete && v.DateDepart >= debutUtc
                    && v.DateDepart < finUtc, cancellationToken);

            var totalBillets = await _context.Billets.AsNoTracking()
                .CountAsync(b => b.IdSociete == idSociete && b.DateGeneration >= debutUtc
                    && b.DateGeneration < finUtc, cancellationToken);

            var montantNonPayeGlobal = await GetMontantReservationsNonPayeesAsync(idSociete, cancellationToken);

            var evolutionMensuelle = await StatistiquesTransportMetricsHelper.GetEvolutionMensuelleAsync(
                _context, idSociete, cancellationToken);

            var repartitionPaiements = await StatistiquesTransportMetricsHelper.GetRepartitionPaiementsAsync(
                _context, idSociete, debutUtc, finUtc, cancellationToken);

            var repartitionDestination = await StatistiquesTransportMetricsHelper.GetRepartitionParDestinationAsync(
                _context, idSociete, debutUtc, finUtc, cancellationToken);

            var repartitionTypeVehicule = await StatistiquesTransportMetricsHelper.GetRepartitionParTypeVehiculeAsync(
                _context, idSociete, debutUtc, finUtc, cancellationToken);

            var statistiquesVoyagesMois = await StatistiquesTransportMetricsHelper.GetStatistiquesVoyagesMoisAsync(
                _context, idSociete, cancellationToken);

            var clientActivite = await StatistiquesTransportMetricsHelper.GetClientActiviteAsync(
                _context, idSociete, cancellationToken);

            var transportStatistiques = await SocieteTransportMetricsHelper.GetSocieteTransportMetricsAsync(
                _context, idSociete, monthStartUtc, todayUtc, weekStartUtc, cancellationToken: cancellationToken);

            var topAgents = await StatistiquesTransportMetricsHelper.GetTopAgentsAsync(
                _context, idSociete, debutUtc, finUtc, cancellationToken: cancellationToken);

            var performanceMensuelle = await StatistiquesTransportMetricsHelper.GetPerformanceMensuelleAsync(
                _context, idSociete, cancellationToken);

            var nowUtc = DateTime.UtcNow;

            return new StatistiquesTransportDto
            {
                Generales = new StatistiquesGeneralesDto
                {
                    TotalClients = totalClients,
                    TotalReservations = totalReservations,
                    TotalVoyages = totalVoyages,
                    TotalBillets = totalBillets,
                    TotalPaiements = ca,
                    MontantReservationsNonPayees = montantNonPayeGlobal,
                    TauxPaiement = tauxPaiement,
                    TotalPaiementsCount = nbPaiements,
                    DateGeneration = nowUtc
                },
                Financieres = new StatistiquesFinancieresDto
                {
                    ChiffreAffaires = ca,
                    MontantPaye = ca,
                    MontantDu = montantDu,
                    EvolutionMensuelle = evolutionMensuelle,
                    RepartitionPaiements = repartitionPaiements,
                    DateGeneration = nowUtc
                },
                Operationnelles = new StatistiquesOperationnellesDto
                {
                    RepartitionParDestination = repartitionDestination,
                    RepartitionParTypeVehicule = repartitionTypeVehicule,
                    StatistiquesVoyagesMois = statistiquesVoyagesMois,
                    ClientActivite = clientActivite,
                    TransportStatistiques = transportStatistiques,
                    DateGeneration = nowUtc
                },
                Performance = new StatistiquesPerformanceDto
                {
                    TauxPaiementGlobal = tauxPaiement,
                    TopAgents = topAgents,
                    PerformanceMensuelle = performanceMensuelle,
                    DateGeneration = nowUtc
                },
                Periode = new PeriodeStatistiquesDto
                {
                    DateDebut = debutUtc,
                    DateFin = finUtc.AddTicks(-1),
                    LibellePeriode = libellePeriode
                },
                CodeDevisePrincipale = string.IsNullOrWhiteSpace(societe.CodeDevisePrincipale)
                    ? "CDF"
                    : societe.CodeDevisePrincipale,
                DateGeneration = nowUtc
            };
        }

        private async Task<decimal> GetMontantReservationsNonPayeesAsync(
            int idSociete,
            CancellationToken cancellationToken)
        {
            var reservationsNonPayees = await _context.Reservations.AsNoTracking()
                .Include(r => r.Voyage)
                .Where(r => r.IdSociete == idSociete && r.Statut
                    && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation))
                .Where(r => !_context.Paiements.Any(p =>
                    p.IdReservation == r.IdReservation && p.Statut && !p.IsDeleted))
                .ToListAsync(cancellationToken);

            return reservationsNonPayees.Sum(r =>
                CaissierTransportMetricsHelper.ResolveMontantVoyage(r.Voyage) * Math.Max(r.NombreDePlace, 1));
        }
    }
}
