using CongoTravel.Data;
using CongoTravel.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Helpers
{
    public static class GerantTransportMetricsHelper
    {
        public static async Task<List<int>> GetSocieteClientIdsAsync(
            CongoTravelDbContext context,
            int idSociete,
            int? idSite = null,
            CancellationToken cancellationToken = default)
        {
            var query = context.Reservations.AsNoTracking()
                .Where(r => r.IdSociete == idSociete && r.Statut);

            if (idSite.HasValue)
                query = query.Where(r => r.IdSite == idSite.Value);

            return await query
                .Select(r => r.IdClient)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public static async Task<int> CountClientsActifsAsync(
            CongoTravelDbContext context,
            int idSociete,
            int? idSite = null,
            CancellationToken cancellationToken = default)
        {
            var query = context.Reservations.AsNoTracking()
                .Where(r => r.IdSociete == idSociete && r.Statut
                    && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation));

            if (idSite.HasValue)
                query = query.Where(r => r.IdSite == idSite.Value);

            return await query
                .Select(r => r.IdClient)
                .Distinct()
                .Join(
                    context.Clients.AsNoTracking().Where(c => c.Statut && c.IsActif && (!c.IsDeleted.HasValue || !c.IsDeleted.Value)),
                    idClient => idClient,
                    client => client.IdClient,
                    (idClient, _) => idClient)
                .CountAsync(cancellationToken);
        }

        public static async Task<List<TopClientDto>> GetTop5ClientsCaAsync(
            CongoTravelDbContext context,
            int idSociete,
            DateTime monthStartUtc,
            DateTime nextMonthStartUtc,
            int? idSite = null,
            CancellationToken cancellationToken = default)
        {
            var paiementsQuery = context.Paiements.AsNoTracking()
                .Where(p => p.IdSociete == idSociete && !p.IsDeleted && p.Statut
                    && p.DatePaiement >= monthStartUtc && p.DatePaiement < nextMonthStartUtc
                    && p.IdReservation.HasValue);

            if (idSite.HasValue)
                paiementsQuery = paiementsQuery.Where(p => p.IdSite == idSite.Value);

            var grouped = await paiementsQuery
                .Join(context.Reservations.AsNoTracking(),
                    p => p.IdReservation!.Value,
                    r => r.IdReservation,
                    (p, r) => new { r.IdClient, p.MontantPayeDevisePrincipale, p.MontantPaye })
                .GroupBy(x => x.IdClient)
                .Select(g => new
                {
                    IdClient = g.Key,
                    Montant = g.Sum(x => (x.MontantPayeDevisePrincipale ?? 0m) > 0m
                        ? x.MontantPayeDevisePrincipale ?? 0m
                        : (x.MontantPaye ?? 0m))
                })
                .OrderByDescending(x => x.Montant)
                .Take(5)
                .ToListAsync(cancellationToken);

            if (grouped.Count == 0)
                return new List<TopClientDto>();

            var clientIds = grouped.Select(x => x.IdClient).ToList();
            var clients = await context.Clients.AsNoTracking()
                .Where(c => clientIds.Contains(c.IdClient))
                .ToDictionaryAsync(c => c.IdClient, c => c.NomClient, cancellationToken);

            return grouped.Select((x, index) => new TopClientDto
            {
                Rang = index + 1,
                IdClient = x.IdClient,
                NomClient = clients.GetValueOrDefault(x.IdClient) ?? $"Client {x.IdClient}",
                Valeur = x.Montant
            }).ToList();
        }

        public static async Task<List<TopClientDto>> GetTop5ClientsNonPayesAsync(
            CongoTravelDbContext context,
            int idSociete,
            int? idSite = null,
            CancellationToken cancellationToken = default)
        {
            var query = context.Reservations.AsNoTracking()
                .Include(r => r.Voyage)
                .Include(r => r.Client)
                .Where(r => r.IdSociete == idSociete && r.Statut
                    && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation));

            if (idSite.HasValue)
                query = query.Where(r => r.IdSite == idSite.Value);

            var unpaid = await query
                .Where(r => !context.Paiements.Any(p =>
                    p.IdReservation == r.IdReservation && p.Statut && !p.IsDeleted))
                .ToListAsync(cancellationToken);

            var grouped = unpaid
                .GroupBy(r => r.IdClient)
                .Select(g => new
                {
                    IdClient = g.Key,
                    Montant = g.Sum(r => (r.Voyage?.Prix ?? 0m) * r.NombreDePlace),
                    Nom = g.First().Client?.NomClient ?? $"Client {g.Key}"
                })
                .OrderByDescending(x => x.Montant)
                .Take(5)
                .ToList();

            return grouped.Select((x, index) => new TopClientDto
            {
                Rang = index + 1,
                IdClient = x.IdClient,
                NomClient = x.Nom,
                Valeur = x.Montant
            }).ToList();
        }

        public static async Task<PaiementsStatistiquesDto> GetPaiementsStatistiquesAsync(
            CongoTravelDbContext context,
            int idSociete,
            int? idSite = null,
            CancellationToken cancellationToken = default)
        {
            var (todayUtc, monthStartUtc, weekStartUtc) = SocieteTransportMetricsHelper.GetUtcBoundaries();
            var nextMonthStartUtc = monthStartUtc.AddMonths(1);

            var paiementsQuery = context.Paiements.AsNoTracking()
                .Where(p => p.IdSociete == idSociete && !p.IsDeleted && p.Statut
                    && p.DatePaiement >= monthStartUtc && p.DatePaiement < nextMonthStartUtc);

            if (idSite.HasValue)
                paiementsQuery = paiementsQuery.Where(p => p.IdSite == idSite.Value);

            var paiements = await paiementsQuery
                .Select(p => new { p.DatePaiement, p.MontantPayeDevisePrincipale, p.MontantPaye })
                .ToListAsync(cancellationToken);

            decimal ResolveAmount(decimal? montantDevisePrincipale, decimal? montantPaye) =>
                (montantDevisePrincipale ?? 0m) > 0m ? montantDevisePrincipale ?? 0m : (montantPaye ?? 0m);

            var jour = paiements.Where(p => p.DatePaiement.Date == todayUtc).ToList();
            var semaine = paiements.Where(p => p.DatePaiement.Date >= weekStartUtc && p.DatePaiement.Date <= todayUtc).ToList();

            var joursEcoules = Math.Max(1, (todayUtc - monthStartUtc).Days + 1);

            return new PaiementsStatistiquesDto
            {
                PaiementsJour = jour.Sum(p => ResolveAmount(p.MontantPayeDevisePrincipale, p.MontantPaye)),
                PaiementsSemaine = semaine.Sum(p => ResolveAmount(p.MontantPayeDevisePrincipale, p.MontantPaye)),
                PaiementsMois = paiements.Sum(p => ResolveAmount(p.MontantPayeDevisePrincipale, p.MontantPaye)),
                NombrePaiementsJour = jour.Count,
                NombrePaiementsSemaine = semaine.Count,
                NombrePaiementsMois = paiements.Count,
                MoyennePaiementsJournaliers = paiements.Count > 0
                    ? Math.Round(paiements.Sum(p => ResolveAmount(p.MontantPayeDevisePrincipale, p.MontantPaye)) / joursEcoules, 2)
                    : 0m
            };
        }

        public static async Task<TendancesDto> BuildTendancesGerantAsync(
            CongoTravelDbContext context,
            int idSociete,
            int? idSite = null,
            CancellationToken cancellationToken = default)
        {
            var ca = new List<TendanceMensuelleDto>();
            var tauxPaiement = new List<TendanceMensuelleDto>();
            var reservations = new List<TendanceMensuelleDto>();

            var nowUtc = DateTime.UtcNow;
            for (var i = 11; i >= 0; i--)
            {
                var dateRef = nowUtc.AddMonths(-i);
                var monthStart = new DateTime(dateRef.Year, dateRef.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var nextMonthStart = monthStart.AddMonths(1);
                var moisLabel = monthStart.ToString("MMM yyyy");

                var stats = await FinancierTransportMetricsHelper.GetSocieteFinancierStatsAsync(
                    context, idSociete, monthStart, nextMonthStart, monthStart.AddMonths(-1), idSite, cancellationToken);

                var reservationsQuery = context.Reservations.AsNoTracking()
                    .Where(r => r.IdSociete == idSociete && r.Statut
                        && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation)
                        && r.DateReservation >= monthStart && r.DateReservation < nextMonthStart);

                if (idSite.HasValue)
                    reservationsQuery = reservationsQuery.Where(r => r.IdSite == idSite.Value);

                var reservationsMois = await reservationsQuery
                    .Select(r => r.IdReservation)
                    .ToListAsync(cancellationToken);

                var reservationsPayees = reservationsMois.Count == 0 ? 0 : await context.Paiements.AsNoTracking()
                    .CountAsync(p => p.IdReservation.HasValue
                        && reservationsMois.Contains(p.IdReservation.Value)
                        && p.Statut && !p.IsDeleted, cancellationToken);

                var taux = reservationsMois.Count > 0
                    ? Math.Round((decimal)reservationsPayees / reservationsMois.Count * 100m, 2)
                    : 0m;

                ca.Add(new TendanceMensuelleDto { Mois = moisLabel, Annee = monthStart.Year, Valeur = stats.ChiffreAffairesMois });
                tauxPaiement.Add(new TendanceMensuelleDto { Mois = moisLabel, Annee = monthStart.Year, Valeur = taux });
                reservations.Add(new TendanceMensuelleDto { Mois = moisLabel, Annee = monthStart.Year, Valeur = reservationsMois.Count });
            }

            return new TendancesDto
            {
                EvolutionChiffreAffaires = ca,
                EvolutionTauxPaiement = tauxPaiement,
                EvolutionReservationsConfirmees = reservations
            };
        }

        public static async Task<List<AlerteSocieteDto>> BuildAlertesTransportAsync(
            CongoTravelDbContext context,
            int idSociete,
            SocieteStatistiquesDto stats,
            int? idSite = null,
            CancellationToken cancellationToken = default)
        {
            var alertes = new List<AlerteSocieteDto>();
            var id = 1;

            if (stats.TauxPaiement < 70m)
            {
                alertes.Add(new AlerteSocieteDto
                {
                    IdAlerte = id++,
                    TypeAlerte = "Taux de paiement faible",
                    Description = $"Taux de paiement faible pour {stats.NomSociete}: {stats.TauxPaiement:F1}%",
                    NiveauCriticite = stats.TauxPaiement < 50m ? "Élevée" : "Moyenne",
                    DateAlerte = DateTime.UtcNow,
                    Statut = "Non lue"
                });
            }

            if (stats.MontantReservationsNonPayees > 1_000_000m)
            {
                alertes.Add(new AlerteSocieteDto
                {
                    IdAlerte = id++,
                    TypeAlerte = "Réservations non payées",
                    Description = $"Montant de réservations non payées élevé: {stats.MontantReservationsNonPayees:N0} {stats.CodeDevisePrincipale}",
                    NiveauCriticite = stats.MontantReservationsNonPayees > 5_000_000m ? "Élevée" : "Moyenne",
                    DateAlerte = DateTime.UtcNow,
                    Statut = "Non lue"
                });
            }

            var tomorrow = DateTime.UtcNow.Date.AddDays(1);
            var voyagesQuery = context.Voyages.AsNoTracking()
                .Include(v => v.Destination)
                .Where(v => v.IdSociete == idSociete && v.Statut == true && v.DateDepart.Date == tomorrow);

            if (idSite.HasValue)
                voyagesQuery = voyagesQuery.Where(v => v.IdSite == idSite.Value);

            var voyagesDemain = await voyagesQuery.CountAsync(cancellationToken);

            if (voyagesDemain > 0)
            {
                var reservationsQuery = context.Reservations.AsNoTracking()
                    .Where(r => r.IdSociete == idSociete && r.Statut
                        && SocieteTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation));

                if (idSite.HasValue)
                    reservationsQuery = reservationsQuery.Where(r => r.IdSite == idSite.Value);

                var unpaidTomorrow = await reservationsQuery
                    .Join(context.Voyages.AsNoTracking(),
                        r => r.IdVoyage,
                        v => v.Id,
                        (r, v) => new { r.IdReservation, v.DateDepart, v.IdSite })
                    .Where(x => x.DateDepart.Date == tomorrow)
                    .Where(x => !idSite.HasValue || x.IdSite == idSite.Value)
                    .Where(x => !context.Paiements.Any(p =>
                        p.IdReservation == x.IdReservation && p.Statut && !p.IsDeleted))
                    .CountAsync(cancellationToken);

                if (unpaidTomorrow > 0)
                {
                    alertes.Add(new AlerteSocieteDto
                    {
                        IdAlerte = id++,
                        TypeAlerte = "Voyages demain non soldés",
                        Description = $"{unpaidTomorrow} réservation(s) non payée(s) pour des voyages demain",
                        NiveauCriticite = "Moyenne",
                        DateAlerte = DateTime.UtcNow,
                        Statut = "Non lue"
                    });
                }
            }

            var topUnpaid = await GetTop5ClientsNonPayesAsync(context, idSociete, idSite, cancellationToken);
            foreach (var client in topUnpaid.Where(c => c.Valeur > 500_000m))
            {
                alertes.Add(new AlerteSocieteDto
                {
                    IdAlerte = id++,
                    TypeAlerte = "Client non payé",
                    Description = $"Client avec réservations non payées importantes: {client.NomClient}",
                    NiveauCriticite = "Élevée",
                    DateAlerte = DateTime.UtcNow,
                    IdClient = client.IdClient,
                    NomClient = client.NomClient,
                    Statut = "Non lue"
                });
            }

            return alertes.OrderByDescending(a => a.DateAlerte).ToList();
        }
    }
}
