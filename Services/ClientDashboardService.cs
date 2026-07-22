using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class ClientDashboardService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<ClientDashboardService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public ClientDashboardService(
            CongoTravelDbContext context,
            ILogger<ClientDashboardService> logger,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<ClientDashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            var clientId = await RequireClientIdAsync(cancellationToken);
            _logger.LogInformation("Génération du dashboard Client {ClientId} pour le transport", clientId);

            var codeDevisePrincipale = await ClientTransportMetricsHelper.GetCodeDevisePrincipaleForClientAsync(
                _context,
                clientId,
                _currentUserService.SocieteId,
                cancellationToken);

            var statistiques = await GetClientStatistiquesAsync(clientId, cancellationToken);
            var reservationsRecentes = await GetReservationsRecentesAsync(clientId, cancellationToken);
            var paiementsRecents = await GetPaiementsRecentsAsync(clientId, cancellationToken);
            var voyagesClient = await GetVoyagesClientAsync(clientId, cancellationToken);
            var alertesClient = await GetAlertesClientAsync(clientId, cancellationToken);
            var resumeClient = await GetResumeClientAsync(clientId, cancellationToken);

            return new ClientDashboardDto
            {
                Statistiques = statistiques,
                ReservationsRecentes = reservationsRecentes,
                PaiementsRecents = paiementsRecents,
                VoyagesClient = voyagesClient,
                AlertesClient = alertesClient,
                ResumeClient = resumeClient,
                CodeDevisePrincipale = codeDevisePrincipale,
                DateGeneration = DateTime.UtcNow
            };
        }

        private async Task<int> RequireClientIdAsync(CancellationToken cancellationToken = default)
        {
            var clientId = await ResolveClientIdAsync(cancellationToken);
            if (!clientId.HasValue)
            {
                _logger.LogWarning("ID client non trouvé pour le dashboard Client");
                throw new UnauthorizedAccessException("ID client non trouvé");
            }

            return clientId.Value;
        }

        private async Task<int?> ResolveClientIdAsync(CancellationToken cancellationToken = default)
        {
            if (_currentUserService.ClientId is > 0)
                return _currentUserService.ClientId;

            var userId = _currentUserService.UserId;
            if (userId <= 0)
                return null;

            var clientId = await _context.Utilisateurs
                .AsNoTracking()
                .Where(u => u.IdUtilisateur == userId)
                .Select(u => u.IdClient)
                .FirstOrDefaultAsync(cancellationToken);

            return clientId is > 0 ? clientId : null;
        }

        private async Task<ClientStatistiquesDto> GetClientStatistiquesAsync(
            int clientId,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var confirmedStatuses = CaissierTransportMetricsHelper.StatutsReservationConfirmes;

            var reservationsClient = await _context.Reservations
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Destination)
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Vehicule)
                .Where(r => r.IdClient == clientId
                    && confirmedStatuses.Contains(r.StatutReservation))
                .ToListAsync(cancellationToken);

            var montantTotalReservations = reservationsClient
                .Sum(r => CaissierTransportMetricsHelper.ResolveMontantVoyage(r.Voyage));

            var reservationIds = reservationsClient.Select(r => r.IdReservation).ToList();

            var paiementsValides = await _context.Paiements
                .Where(p => p.IdReservation.HasValue
                    && reservationIds.Contains(p.IdReservation.Value)
                    && p.Statut
                    && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            var montantTotalPaye = paiementsValides.Sum(CaissierTransportMetricsHelper.ResolveMontantPaye);
            var montantTotalDu = Math.Max(montantTotalReservations - montantTotalPaye, 0);
            var nombreReservations = reservationsClient.Count;
            var nombreReservationsPayees = paiementsValides
                .Select(p => p.IdReservation)
                .Distinct()
                .Count();

            var nombreReservationsEnRetard = reservationsClient
                .Count(r => r.DateReservation < nowUtc.AddHours(-24)
                    && !paiementsValides.Any(p => p.IdReservation == r.IdReservation));

            var tauxPaiement = montantTotalReservations > 0
                ? Math.Round(montantTotalPaye / montantTotalReservations * 100, 2)
                : 0;

            var nombreVoyagesEffectues = reservationsClient
                .Count(r => r.Voyage?.DateDepart < nowUtc);

            var destinationFavorite = reservationsClient
                .Where(r => r.Voyage?.Destination != null)
                .GroupBy(r => r.Voyage!.Destination!.VilleArrivee)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? string.Empty;

            return new ClientStatistiquesDto
            {
                MontantTotalReservations = montantTotalReservations,
                MontantTotalPaye = montantTotalPaye,
                MontantTotalDu = montantTotalDu,
                NombreReservations = nombreReservations,
                NombreReservationsPayees = nombreReservationsPayees,
                NombreReservationsEnRetard = nombreReservationsEnRetard,
                TauxPaiement = tauxPaiement,
                NombreVoyagesEffectues = nombreVoyagesEffectues,
                DestinationFavorite = destinationFavorite
            };
        }

        private async Task<List<ReservationRecenteDto>> GetReservationsRecentesAsync(
            int clientId,
            CancellationToken cancellationToken = default)
        {
            var confirmedStatuses = CaissierTransportMetricsHelper.StatutsReservationConfirmes;

            var reservationsRecentes = await _context.Reservations
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Destination)
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Vehicule)
                .Where(r => r.IdClient == clientId
                    && confirmedStatuses.Contains(r.StatutReservation))
                .OrderByDescending(r => r.DateReservation)
                .Take(10)
                .ToListAsync(cancellationToken);

            var reservationIds = reservationsRecentes.Select(r => r.IdReservation).ToList();
            if (reservationIds.Count == 0)
                return new List<ReservationRecenteDto>();

            var paiements = await _context.Paiements
                .Where(p => p.IdReservation.HasValue
                    && reservationIds.Contains(p.IdReservation.Value)
                    && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            var paiementsByReservation = paiements
                .Where(p => p.IdReservation.HasValue)
                .GroupBy(p => p.IdReservation!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        MontantPaye = g.Where(x => x.Statut).Sum(CaissierTransportMetricsHelper.ResolveMontantPaye),
                        HasAnyValidated = g.Any(x => x.Statut)
                    });

            var billetReservations = await _context.Billets
                .Where(b => b.IdReservation.HasValue && reservationIds.Contains(b.IdReservation.Value))
                .Select(b => new { IdReservation = b.IdReservation!.Value, b.QrCode })
                .ToListAsync(cancellationToken);

            var billetsByReservation = billetReservations
                .GroupBy(x => x.IdReservation)
                .ToDictionary(g => g.Key, g => g.Select(x => x.QrCode).FirstOrDefault() ?? string.Empty);

            return reservationsRecentes.Select(r =>
            {
                paiementsByReservation.TryGetValue(r.IdReservation, out var paiementInfo);
                billetsByReservation.TryGetValue(r.IdReservation, out var qrCode);
                var montantTotal = CaissierTransportMetricsHelper.ResolveMontantVoyage(r.Voyage);
                var montantPaye = paiementInfo?.MontantPaye ?? 0;

                return new ReservationRecenteDto
                {
                    IdReservation = r.IdReservation,
                    Reference = $"RES-{r.IdReservation:D6}",
                    VoyageInfo = r.Voyage != null
                        ? $"{r.Voyage.Destination?.VilleDepart} - {r.Voyage.Destination?.VilleArrivee}"
                        : "Voyage non spécifié",
                    MontantTotal = montantTotal,
                    MontantPaye = montantPaye,
                    MontantDu = Math.Max(montantTotal - montantPaye, 0),
                    DateReservation = r.DateReservation,
                    DateVoyage = r.Voyage?.DateDepart ?? DateTime.MinValue,
                    Statut = r.StatutReservation,
                    StatutPaiement = paiementInfo?.HasAnyValidated == true ? "Validé" : "En attente",
                    NombrePlaces = r.NombreDePlace,
                    Destination = r.Voyage?.Destination?.VilleArrivee ?? string.Empty,
                    HeureDepart = r.Voyage?.HeureDepart.ToString(@"hh\:mm") ?? string.Empty,
                    PossedeBillet = !string.IsNullOrWhiteSpace(qrCode),
                    QrCodeBillet = qrCode ?? string.Empty
                };
            }).ToList();
        }

        private async Task<List<PaiementClientRecentDto>> GetPaiementsRecentsAsync(
            int clientId,
            CancellationToken cancellationToken = default)
        {
            var paiementsRecents = await _context.Paiements
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Voyage)
                        .ThenInclude(v => v!.Destination)
                .Where(p => !p.IsDeleted
                    && p.Statut
                    && p.IdReservation.HasValue
                    && _context.Reservations.Any(r =>
                        r.IdReservation == p.IdReservation!.Value && r.IdClient == clientId))
                .OrderByDescending(p => p.DateCreation)
                .Take(10)
                .ToListAsync(cancellationToken);

            return paiementsRecents.Select(p => new PaiementClientRecentDto
            {
                IdPaiement = p.IdPaiement,
                Reference = $"PAY-{p.IdPaiement:D6}",
                MontantPaye = CaissierTransportMetricsHelper.ResolveMontantPaye(p),
                DatePaiement = p.DatePaiement,
                MethodePaiement = p.MethodePaiement ?? "Non spécifié",
                Statut = p.Statut ? "Validé" : "En attente",
                ReferenceReservation = p.IdReservation.HasValue ? $"RES-{p.IdReservation.Value:D6}" : string.Empty,
                VoyageInfo = p.Reservation?.Voyage != null
                    ? $"{p.Reservation.Voyage.Destination?.VilleDepart} - {p.Reservation.Voyage.Destination?.VilleArrivee}"
                    : "Voyage non spécifié",
                DateVoyage = p.Reservation?.Voyage?.DateDepart ?? DateTime.MinValue,
                Destination = p.Reservation?.Voyage?.Destination?.VilleArrivee ?? string.Empty
            }).ToList();
        }

        private async Task<List<VoyageClientDto>> GetVoyagesClientAsync(
            int clientId,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var confirmedStatuses = CaissierTransportMetricsHelper.StatutsReservationConfirmes;

            var reservationsClient = await _context.Reservations
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Destination)
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Vehicule)
                        .ThenInclude(vh => vh!.TypeVehicule)
                .Where(r => r.IdClient == clientId
                    && confirmedStatuses.Contains(r.StatutReservation))
                .ToListAsync(cancellationToken);

            var voyageRows = reservationsClient
                .Where(r => r.Voyage != null)
                .GroupBy(r => r.Voyage!.Id)
                .Select(g => new
                {
                    Voyage = g.First().Voyage!,
                    PlacesReservees = g.Sum(x => x.NombreDePlace)
                })
                .ToList();

            return voyageRows.Select(x =>
            {
                var v = x.Voyage;
                var seatsTotal = v.Vehicule?.NombreSiege ?? 0;
                var tauxRemplissage = seatsTotal > 0
                    ? Math.Round((decimal)x.PlacesReservees / seatsTotal * 100, 2)
                    : 0m;

                return new VoyageClientDto
                {
                    IdVoyage = v.Id,
                    Reference = $"VOY-{v.Id:D6}",
                    VilleDepart = v.Destination?.VilleDepart ?? string.Empty,
                    VilleArrivee = v.Destination?.VilleArrivee ?? string.Empty,
                    DateDepart = v.DateDepart,
                    HeureDepart = v.HeureDepart,
                    Prix = CaissierTransportMetricsHelper.ResolveMontantVoyage(v),
                    TypeVehicule = v.Vehicule?.TypeVehicule?.Libelle ?? "Standard",
                    StatutVoyage = v.Statut == true ? "Actif" : "Inactif",
                    NombrePlacesReservees = x.PlacesReservees,
                    NombrePlacesTotal = seatsTotal,
                    TauxRemplissage = tauxRemplissage,
                    DateVoyageEffectue = v.DateDepart,
                    EstEffectue = v.DateDepart < nowUtc
                };
            }).ToList();
        }

        private async Task<List<AlerteClientDto>> GetAlertesClientAsync(
            int clientId,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var (todayUtc, _, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();
            var demainUtc = todayUtc.AddDays(1);
            var demainFinUtc = demainUtc.AddDays(1);
            var confirmedStatuses = CaissierTransportMetricsHelper.StatutsReservationConfirmes;
            var alertes = new List<AlerteClientDto>();

            var reservationsNonPayees = await _context.Reservations
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Destination)
                .Where(r => r.IdClient == clientId
                    && confirmedStatuses.Contains(r.StatutReservation)
                    && !_context.Paiements.Any(p => p.IdReservation == r.IdReservation && p.Statut))
                .ToListAsync(cancellationToken);

            foreach (var reservation in reservationsNonPayees)
            {
                alertes.Add(new AlerteClientDto
                {
                    IdAlerte = alertes.Count + 1,
                    TypeAlerte = "Paiement requis",
                    Description = $"Réservation en attente de paiement pour {reservation.Voyage?.Destination?.VilleArrivee}",
                    NiveauCriticite = reservation.DateReservation < nowUtc.AddHours(-24) ? "Élevée" : "Moyenne",
                    DateAlerte = nowUtc,
                    IdReservation = reservation.IdReservation,
                    ReferenceReservation = $"RES-{reservation.IdReservation:D6}",
                    MontantConcerne = CaissierTransportMetricsHelper.ResolveMontantVoyage(reservation.Voyage),
                    EstLue = false,
                    DateVoyage = reservation.Voyage?.DateDepart ?? DateTime.MinValue,
                    Destination = reservation.Voyage?.Destination?.VilleArrivee ?? string.Empty,
                    HeureDepart = reservation.Voyage?.HeureDepart.ToString(@"hh\:mm") ?? string.Empty,
                    ActionSuggeree = "Payer la réservation"
                });
            }

            var voyagesImminents = await _context.Reservations
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Destination)
                .Where(r => r.IdClient == clientId
                    && confirmedStatuses.Contains(r.StatutReservation)
                    && r.Voyage != null
                    && r.Voyage.DateDepart >= demainUtc
                    && r.Voyage.DateDepart < demainFinUtc)
                .ToListAsync(cancellationToken);

            foreach (var reservation in voyagesImminents)
            {
                alertes.Add(new AlerteClientDto
                {
                    IdAlerte = alertes.Count + 1,
                    TypeAlerte = "Voyage imminent",
                    Description = $"Votre voyage pour {reservation.Voyage?.Destination?.VilleArrivee} est demain",
                    NiveauCriticite = "Moyenne",
                    DateAlerte = nowUtc,
                    IdReservation = reservation.IdReservation,
                    ReferenceReservation = $"RES-{reservation.IdReservation:D6}",
                    MontantConcerne = 0,
                    EstLue = false,
                    DateVoyage = reservation.Voyage?.DateDepart ?? DateTime.MinValue,
                    Destination = reservation.Voyage?.Destination?.VilleArrivee ?? string.Empty,
                    HeureDepart = reservation.Voyage?.HeureDepart.ToString(@"hh\:mm") ?? string.Empty,
                    ActionSuggeree = "Préparer vos bagages"
                });
            }

            return alertes.OrderByDescending(a => a.DateAlerte).ToList();
        }

        private async Task<ResumeClientDto> GetResumeClientAsync(
            int clientId,
            CancellationToken cancellationToken = default)
        {
            var (_, monthStartUtc, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();
            var confirmedStatuses = CaissierTransportMetricsHelper.StatutsReservationConfirmes;

            var reservationsClient = await _context.Reservations
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Destination)
                .Where(r => r.IdClient == clientId
                    && confirmedStatuses.Contains(r.StatutReservation))
                .ToListAsync(cancellationToken);

            var reservationsCeMois = reservationsClient
                .Where(r => r.DateReservation >= monthStartUtc)
                .ToList();

            var depensesCeMois = reservationsCeMois
                .Sum(r => CaissierTransportMetricsHelper.ResolveMontantVoyage(r.Voyage));

            var destinationFavorite = reservationsClient
                .Where(r => r.Voyage?.Destination != null)
                .GroupBy(r => r.Voyage!.Destination!.VilleArrivee)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? string.Empty;

            var reservationIds = reservationsClient.Select(r => r.IdReservation).ToList();
            var paiementsValides = await _context.Paiements
                .Where(p => p.IdReservation.HasValue
                    && reservationIds.Contains(p.IdReservation.Value)
                    && p.Statut
                    && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            var montantTotalReservations = reservationsClient
                .Sum(r => CaissierTransportMetricsHelper.ResolveMontantVoyage(r.Voyage));
            var montantTotalPaye = paiementsValides.Sum(CaissierTransportMetricsHelper.ResolveMontantPaye);
            var montantTotalDu = Math.Max(montantTotalReservations - montantTotalPaye, 0);

            return new ResumeClientDto
            {
                StatutCompte = montantTotalDu > 0 ? "En attente de paiement" : "Actif",
                NombreReservationsActives = reservationsClient.Count,
                NombreVoyagesCeMois = reservationsCeMois.Count,
                DepensesCeMois = depensesCeMois,
                DestinationFavorite = destinationFavorite
            };
        }
    }
}
