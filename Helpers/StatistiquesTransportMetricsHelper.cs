using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Statistiques;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CongoTravel.Helpers
{
    public static class StatistiquesTransportMetricsHelper
    {
        public static (DateTime DebutUtc, DateTime FinUtc, string Libelle) ResolvePeriodeUtc(
            DateTime? debut,
            DateTime? fin)
        {
            var (_, monthStartUtc, _) = SocieteTransportMetricsHelper.GetUtcBoundaries();
            var defaultFin = monthStartUtc.AddMonths(1);

            var debutUtc = debut.HasValue
                ? DateTime.SpecifyKind(debut.Value.Date, DateTimeKind.Utc)
                : monthStartUtc;
            var finUtc = fin.HasValue
                ? DateTime.SpecifyKind(fin.Value.Date.AddDays(1), DateTimeKind.Utc)
                : defaultFin;

            if (finUtc <= debutUtc)
                finUtc = debutUtc.AddMonths(1);

            var libelle = debut.HasValue || fin.HasValue
                ? $"{debutUtc:yyyy-MM-dd} — {finUtc.AddDays(-1):yyyy-MM-dd}"
                : debutUtc.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("fr-FR"));

            return (debutUtc, finUtc, libelle);
        }

        public static decimal ResolveMontantPaye(Paiement p) =>
            CaissierTransportMetricsHelper.ResolveMontantPaye(p);

        public static async Task<List<EvolutionMensuelleDto>> GetEvolutionMensuelleAsync(
            CongoTravelDbContext context,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var result = new List<EvolutionMensuelleDto>();
            var nowUtc = DateTime.UtcNow;
            var confirmed = SocieteTransportMetricsHelper.StatutsReservationConfirmes;

            for (var i = 11; i >= 0; i--)
            {
                var dateRef = nowUtc.AddMonths(-i);
                var monthStart = new DateTime(dateRef.Year, dateRef.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var nextMonthStart = monthStart.AddMonths(1);

                var paiements = await context.Paiements.AsNoTracking()
                    .Where(p => p.IdSociete == idSociete && !p.IsDeleted && p.Statut
                        && p.DatePaiement >= monthStart && p.DatePaiement < nextMonthStart)
                    .ToListAsync(cancellationToken);

                var ca = paiements.Sum(ResolveMontantPaye);
                var nbReservations = await context.Reservations.AsNoTracking()
                    .CountAsync(r => r.IdSociete == idSociete && r.Statut
                        && confirmed.Contains(r.StatutReservation)
                        && r.DateReservation >= monthStart && r.DateReservation < nextMonthStart,
                        cancellationToken);

                result.Add(new EvolutionMensuelleDto
                {
                    Mois = monthStart.ToString("MMM yyyy", CultureInfo.GetCultureInfo("fr-FR")),
                    ChiffreAffaires = ca,
                    NombrePaiements = paiements.Count,
                    NombreReservations = nbReservations
                });
            }

            return result;
        }

        public static async Task<List<RepartitionPaiementDto>> GetRepartitionPaiementsAsync(
            CongoTravelDbContext context,
            int idSociete,
            DateTime debutUtc,
            DateTime finUtc,
            CancellationToken cancellationToken = default)
        {
            var paiements = await context.Paiements.AsNoTracking()
                .Where(p => p.IdSociete == idSociete && !p.IsDeleted && p.Statut
                    && p.DatePaiement >= debutUtc && p.DatePaiement < finUtc)
                .ToListAsync(cancellationToken);

            if (paiements.Count == 0)
                return new List<RepartitionPaiementDto>();

            var total = paiements.Sum(ResolveMontantPaye);
            var buckets = new[]
            {
                (Label: "Espèce", Bucket: MethodePaiementHelper.RecetteBucket.Espece),
                (Label: "Mobile Money", Bucket: MethodePaiementHelper.RecetteBucket.MobileMoney),
                (Label: "Virement", Bucket: MethodePaiementHelper.RecetteBucket.Virement),
                (Label: "Carte", Bucket: MethodePaiementHelper.RecetteBucket.Carte)
            };

            return buckets.Select(b =>
            {
                var subset = paiements
                    .Where(p => MethodePaiementHelper.GetRecetteBucket(p.MethodePaiement) == b.Bucket)
                    .ToList();
                var montant = subset.Sum(ResolveMontantPaye);
                return new RepartitionPaiementDto
                {
                    MethodePaiement = b.Label,
                    MontantTotal = montant,
                    NombrePaiements = subset.Count,
                    Pourcentage = total > 0m ? Math.Round(montant / total * 100m, 2) : 0m
                };
            }).Where(x => x.NombrePaiements > 0).OrderByDescending(x => x.MontantTotal).ToList();
        }

        public static async Task<List<RepartitionParDestinationDto>> GetRepartitionParDestinationAsync(
            CongoTravelDbContext context,
            int idSociete,
            DateTime debutUtc,
            DateTime finUtc,
            CancellationToken cancellationToken = default)
        {
            var confirmed = SocieteTransportMetricsHelper.StatutsReservationConfirmes;

            var reservations = await context.Reservations.AsNoTracking()
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Destination)
                .Where(r => r.IdSociete == idSociete && r.Statut
                    && confirmed.Contains(r.StatutReservation)
                    && r.DateReservation >= debutUtc && r.DateReservation < finUtc)
                .ToListAsync(cancellationToken);

            if (reservations.Count == 0)
                return new List<RepartitionParDestinationDto>();

            var total = reservations.Count;
            return reservations
                .GroupBy(r => r.Voyage?.Destination?.VilleArrivee ?? "Inconnue")
                .Select(g => new RepartitionParDestinationDto
                {
                    Destination = g.Key,
                    NombreReservations = g.Count(),
                    MontantTotal = g.Sum(r => CaissierTransportMetricsHelper.ResolveMontantVoyage(r.Voyage)),
                    Pourcentage = Math.Round((decimal)g.Count() / total * 100m, 2)
                })
                .OrderByDescending(x => x.NombreReservations)
                .ToList();
        }

        public static async Task<List<RepartitionParTypeVehiculeDto>> GetRepartitionParTypeVehiculeAsync(
            CongoTravelDbContext context,
            int idSociete,
            DateTime debutUtc,
            DateTime finUtc,
            CancellationToken cancellationToken = default)
        {
            var confirmed = SocieteTransportMetricsHelper.StatutsReservationConfirmes;

            var reservations = await context.Reservations.AsNoTracking()
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Vehicule)
                        .ThenInclude(vh => vh!.TypeVehicule)
                .Where(r => r.IdSociete == idSociete && r.Statut
                    && confirmed.Contains(r.StatutReservation)
                    && r.DateReservation >= debutUtc && r.DateReservation < finUtc)
                .ToListAsync(cancellationToken);

            if (reservations.Count == 0)
                return new List<RepartitionParTypeVehiculeDto>();

            var total = reservations.Count;
            return reservations
                .GroupBy(r => r.Voyage?.Vehicule?.TypeVehicule?.Libelle ?? "Standard")
                .Select(g => new RepartitionParTypeVehiculeDto
                {
                    TypeVehicule = g.Key,
                    NombreReservations = g.Count(),
                    MontantTotal = g.Sum(r => CaissierTransportMetricsHelper.ResolveMontantVoyage(r.Voyage)),
                    Pourcentage = Math.Round((decimal)g.Count() / total * 100m, 2)
                })
                .OrderByDescending(x => x.NombreReservations)
                .ToList();
        }

        public static async Task<List<StatistiqueVoyageMoisDto>> GetStatistiquesVoyagesMoisAsync(
            CongoTravelDbContext context,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var result = new List<StatistiqueVoyageMoisDto>();
            var nowUtc = DateTime.UtcNow;

            for (var i = 11; i >= 0; i--)
            {
                var dateRef = nowUtc.AddMonths(-i);
                var monthStart = new DateTime(dateRef.Year, dateRef.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var nextMonthStart = monthStart.AddMonths(1);

                var nbVoyages = await context.Voyages.AsNoTracking()
                    .CountAsync(v => v.IdSociete == idSociete && v.DateDepart >= monthStart
                        && v.DateDepart < nextMonthStart, cancellationToken);

                var nbBillets = await context.Billets.AsNoTracking()
                    .CountAsync(b => b.IdSociete == idSociete && b.DateGeneration >= monthStart
                        && b.DateGeneration < nextMonthStart, cancellationToken);

                var paiements = await context.Paiements.AsNoTracking()
                    .Where(p => p.IdSociete == idSociete && !p.IsDeleted && p.Statut
                        && p.DatePaiement >= monthStart && p.DatePaiement < nextMonthStart)
                    .ToListAsync(cancellationToken);

                result.Add(new StatistiqueVoyageMoisDto
                {
                    Mois = monthStart.ToString("MMM yyyy", CultureInfo.GetCultureInfo("fr-FR")),
                    NombreVoyages = nbVoyages,
                    NombreBillets = nbBillets,
                    MontantTotal = paiements.Sum(ResolveMontantPaye)
                });
            }

            return result;
        }

        public static async Task<ClientActiviteDto> GetClientActiviteAsync(
            CongoTravelDbContext context,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var clientIds = await context.Reservations.AsNoTracking()
                .Where(r => r.IdSociete == idSociete && r.Statut)
                .Select(r => r.IdClient)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (clientIds.Count == 0)
            {
                return new ClientActiviteDto();
            }

            var actifs = await context.Clients.AsNoTracking()
                .CountAsync(c => clientIds.Contains(c.IdClient)
                    && c.Statut && c.IsActif && (!c.IsDeleted.HasValue || !c.IsDeleted.Value),
                    cancellationToken);

            var total = clientIds.Count;
            var inactifs = Math.Max(total - actifs, 0);

            return new ClientActiviteDto
            {
                NombreClientsActifs = actifs,
                NombreClientsInactifs = inactifs,
                TotalClients = total,
                PourcentageActifs = total > 0 ? Math.Round((decimal)actifs / total * 100m, 2) : 0m,
                PourcentageInactifs = total > 0 ? Math.Round((decimal)inactifs / total * 100m, 2) : 0m
            };
        }

        public static async Task<List<TopAgentDto>> GetTopAgentsAsync(
            CongoTravelDbContext context,
            int idSociete,
            DateTime debutUtc,
            DateTime finUtc,
            int take = 10,
            CancellationToken cancellationToken = default)
        {
            var groupedByUser = await context.Paiements.AsNoTracking()
                .Where(p => p.IdSociete == idSociete && !p.IsDeleted && p.Statut
                    && p.DatePaiement >= debutUtc && p.DatePaiement < finUtc)
                .GroupBy(p => p.IdUtilisateur)
                .Select(g => new
                {
                    IdUtilisateur = g.Key,
                    MontantCollecte = g.Sum(x => (decimal?)(x.MontantPayeDevisePrincipale ?? x.MontantPaye) ?? 0m),
                    NombrePaiements = g.Count()
                })
                .OrderByDescending(x => x.MontantCollecte)
                .Take(take)
                .ToListAsync(cancellationToken);

            if (groupedByUser.Count == 0)
                return new List<TopAgentDto>();

            var userIds = groupedByUser.Select(x => x.IdUtilisateur).ToList();
            var users = await context.Utilisateurs.AsNoTracking()
                .Include(u => u.Agent)
                .Where(u => userIds.Contains(u.IdUtilisateur) && u.IdSociete == idSociete)
                .ToDictionaryAsync(u => u.IdUtilisateur, cancellationToken);

            return groupedByUser.Select(x =>
            {
                users.TryGetValue(x.IdUtilisateur, out var user);
                return new TopAgentDto
                {
                    IdAgent = user?.IdAgent ?? 0,
                    NomAgent = user?.Agent?.NomComplet ?? user?.NomComplet ?? $"Utilisateur {x.IdUtilisateur}",
                    Matricule = user?.Agent?.Matricule,
                    MontantCollecte = x.MontantCollecte,
                    NombrePaiements = x.NombrePaiements
                };
            }).ToList();
        }

        public static async Task<List<PerformanceMensuelleDto>> GetPerformanceMensuelleAsync(
            CongoTravelDbContext context,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var result = new List<PerformanceMensuelleDto>();
            var nowUtc = DateTime.UtcNow;
            var confirmed = SocieteTransportMetricsHelper.StatutsReservationConfirmes;

            for (var i = 11; i >= 0; i--)
            {
                var dateRef = nowUtc.AddMonths(-i);
                var monthStart = new DateTime(dateRef.Year, dateRef.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var nextMonthStart = monthStart.AddMonths(1);

                var paiements = await context.Paiements.AsNoTracking()
                    .Where(p => p.IdSociete == idSociete && !p.IsDeleted && p.Statut
                        && p.DatePaiement >= monthStart && p.DatePaiement < nextMonthStart)
                    .ToListAsync(cancellationToken);

                var ca = paiements.Sum(ResolveMontantPaye);
                var nbPaiements = paiements.Count;
                var ticketMoyen = nbPaiements > 0 ? Math.Round(ca / nbPaiements, 2) : 0m;

                var reservationIds = await context.Reservations.AsNoTracking()
                    .Where(r => r.IdSociete == idSociete && r.Statut
                        && confirmed.Contains(r.StatutReservation)
                        && r.DateReservation >= monthStart && r.DateReservation < nextMonthStart)
                    .Select(r => r.IdReservation)
                    .ToListAsync(cancellationToken);

                var nbReservations = reservationIds.Count;
                var nbPayees = reservationIds.Count == 0 ? 0 : await context.Paiements.AsNoTracking()
                    .CountAsync(p => p.IdReservation.HasValue
                        && reservationIds.Contains(p.IdReservation.Value)
                        && p.Statut && !p.IsDeleted, cancellationToken);

                var taux = nbReservations > 0
                    ? Math.Round((decimal)nbPayees / nbReservations * 100m, 2)
                    : 0m;

                result.Add(new PerformanceMensuelleDto
                {
                    Mois = monthStart.ToString("MMM yyyy", CultureInfo.GetCultureInfo("fr-FR")),
                    TauxPaiement = taux,
                    MontantCollecte = ca,
                    NombrePaiements = nbPaiements,
                    TicketMoyen = ticketMoyen
                });
            }

            return result;
        }

        public static async Task<(decimal Ca, decimal MontantDu, decimal TauxPaiement, int NbPaiements)> GetPeriodeFinanciereAsync(
            CongoTravelDbContext context,
            int idSociete,
            DateTime debutUtc,
            DateTime finUtc,
            CancellationToken cancellationToken = default)
        {
            var paiements = await context.Paiements.AsNoTracking()
                .Where(p => p.IdSociete == idSociete && !p.IsDeleted && p.Statut
                    && p.DatePaiement >= debutUtc && p.DatePaiement < finUtc)
                .ToListAsync(cancellationToken);

            var ca = paiements.Sum(ResolveMontantPaye);

            var reservationsNonPayees = await context.Reservations.AsNoTracking()
                .Include(r => r.Voyage)
                .Where(r => r.IdSociete == idSociete && r.Statut
                    && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation)
                    && r.DateReservation >= debutUtc && r.DateReservation < finUtc)
                .Where(r => !context.Paiements.Any(p =>
                    p.IdReservation == r.IdReservation && p.Statut && !p.IsDeleted))
                .ToListAsync(cancellationToken);

            var montantDu = reservationsNonPayees.Sum(r =>
                CaissierTransportMetricsHelper.ResolveMontantVoyage(r.Voyage) * Math.Max(r.NombreDePlace, 1));

            var denom = ca + montantDu;
            var taux = denom > 0m ? Math.Round(ca / denom * 100m, 2) : 0m;

            return (ca, montantDu, taux, paiements.Count);
        }
    }
}
