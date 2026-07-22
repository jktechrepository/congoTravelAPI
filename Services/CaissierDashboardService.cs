using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class CaissierDashboardService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<CaissierDashboardService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public CaissierDashboardService(
            CongoTravelDbContext context,
            ILogger<CaissierDashboardService> logger,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<CaissierDashboardDto> GetDashboardDataAsync(CancellationToken cancellationToken = default)
        {
            var societeId = _currentUserService.SocieteId;
            if (societeId <= 0)
            {
                _logger.LogWarning("ID de société non trouvé pour le caissier");
                throw new UnauthorizedAccessException("ID de société non trouvé");
            }

            var utilisateurId = ValidateSocieteScope(societeId);
            _logger.LogInformation(
                "Génération du dashboard Caissier pour la société {SocieteId}, utilisateur {UtilisateurId}",
                societeId, utilisateurId);

            var codeDevisePrincipale = await CaissierTransportMetricsHelper.GetCodeDevisePrincipaleAsync(
                _context, societeId, cancellationToken);

            var statistiquesJournalieres = await GetStatistiquesJournalieresAsync(societeId, cancellationToken);
            var paiementsEnCours = await GetPaiementsEnCoursAsync(societeId, cancellationToken);
            var paiementsRecents = await GetPaiementsRecentsAsync(societeId, cancellationToken);
            var recettesJournalieres = await GetRecettesJournalieresAsync(societeId, cancellationToken);
            var alertesCaissier = await GetAlertesCaissierAsync(societeId, cancellationToken);
            var resumeCaisse = await GetResumeCaisseAsync(societeId, cancellationToken);
            var performancesMensuelles = await GetPerformancesMensuellesAsync(societeId, cancellationToken);

            return new CaissierDashboardDto
            {
                StatistiquesJournalieres = statistiquesJournalieres,
                PaiementsEnCours = paiementsEnCours,
                PaiementsRecents = paiementsRecents,
                RecettesJournalieres = recettesJournalieres,
                AlertesCaissier = alertesCaissier,
                ResumeCaisse = resumeCaisse,
                PerformancesMensuelles = performancesMensuelles,
                CodeDevisePrincipale = codeDevisePrincipale,
                DateGeneration = DateTime.UtcNow
            };
        }

        public async Task<RapportCaisseDto> GetRapportCaisseAsync(
            DateTime? datePrecise,
            DateTime? dateDebut,
            DateTime? dateFin,
            CancellationToken cancellationToken = default)
        {
            var societeId = _currentUserService.SocieteId;
            if (societeId <= 0)
            {
                throw new UnauthorizedAccessException("ID de société non trouvé");
            }

            var utilisateurId = ValidateSocieteScope(societeId);

            var (fromUtc, toUtc, modePeriode, isValid, errorMessage) =
                RapportCaisseMetricsHelper.ResolvePeriode(datePrecise, dateDebut, dateFin);

            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            var rangeEndExclusive = toUtc.AddTicks(1);
            var paiements = await QueryPaiementsEncaissesAsync(
                _context, societeId, utilisateurId, fromUtc, rangeEndExclusive, cancellationToken);

            var codeDevisePrincipale = await CaissierTransportMetricsHelper.GetCodeDevisePrincipaleAsync(
                _context, societeId, cancellationToken);

            _logger.LogInformation(
                "Rapport caisse caissier société {SocieteId}, utilisateur {UtilisateurId}, période {ModePeriode}",
                societeId, utilisateurId, modePeriode);

            return RapportCaisseMetricsHelper.BuildRapportCaisse(
                paiements,
                societeId,
                utilisateurId,
                fromUtc,
                toUtc,
                modePeriode,
                codeDevisePrincipale);
        }

        private async Task<CaissierStatistiquesDto> GetStatistiquesJournalieresAsync(
            int societeId,
            CancellationToken cancellationToken = default)
        {
            var utilisateurId = ValidateSocieteScope(societeId);
            var (todayUtc, _, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();
            var tomorrowUtc = todayUtc.AddDays(1);

            var paiementsJour = await QueryPaiementsEncaissesJourAsync(
                    _context, societeId, utilisateurId, todayUtc, tomorrowUtc, cancellationToken);

            var reservationsJour = await _context.Reservations
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Vehicule)
                .Include(r => r.Client)
                .Where(r => r.IdSociete == societeId
                    && r.IdUtilisateur == utilisateurId
                    && r.DateReservation >= todayUtc
                    && r.DateReservation < tomorrowUtc
                    && CaissierTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation))
                .ToListAsync(cancellationToken);

            var montants = paiementsJour.Select(CaissierTransportMetricsHelper.ResolveMontantPaye).ToList();
            var totalRevenusTransport = montants.Sum();
            var nombreTransactions = paiementsJour.Count;

            var reservationsNonPayees = await _context.Reservations
                .Include(r => r.Voyage)
                .Where(r => r.IdSociete == societeId
                    && r.IdUtilisateur == utilisateurId
                    && CaissierTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation))
                .ToListAsync(cancellationToken);

            var reservationsNonPayeesIds = reservationsNonPayees
                .Where(r => !_context.Paiements
                    .Any(p => p.IdReservation == r.IdReservation && p.Statut))
                .ToList();

            var totalReservationsNonPayees = reservationsNonPayeesIds
                .Sum(r => CaissierTransportMetricsHelper.ResolveMontantVoyage(r.Voyage));

            var nombrePassagers = await CountPassagersPayesAsync(paiementsJour, cancellationToken);
            var reservationsConfirmeesJour = reservationsJour.Count;
            var billetsEmisJour = await CountBilletsEmisJourAsync(
                _context, societeId, reservationsJour, todayUtc, tomorrowUtc, cancellationToken);

            var tauxRemplissageMoyen = CaissierTransportMetricsHelper.ComputeTauxRemplissageCaissierJour(reservationsJour);

            return new CaissierStatistiquesDto
            {
                TotalRevenusTransport = totalRevenusTransport,
                NombreTransactions = nombreTransactions,
                MoyenneTransaction = nombreTransactions > 0 ? totalRevenusTransport / nombreTransactions : 0,
                PlusGrosMontant = montants.Count > 0 ? montants.Max() : 0,
                PlusPetitMontant = montants.Count > 0 ? montants.Min() : 0,
                NombrePassagers = nombrePassagers,
                TotalReservationsNonPayees = totalReservationsNonPayees,
                NombreBilletsVendus = reservationsConfirmeesJour,
                ReservationsConfirmeesJour = reservationsConfirmeesJour,
                BilletsEmisJour = billetsEmisJour,
                TauxRemplissageMoyen = tauxRemplissageMoyen
            };
        }

        private async Task<List<PaiementEnCoursDto>> GetPaiementsEnCoursAsync(
            int societeId,
            CancellationToken cancellationToken = default)
        {
            var utilisateurId = ValidateSocieteScope(societeId);

            var paiementsEnCours = await _context.Paiements
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Voyage)
                        .ThenInclude(v => v!.Destination)
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Client)
                .Where(p => !p.IsDeleted
                    && p.IdUtilisateur == utilisateurId
                    && p.IdSociete == societeId
                    && !p.Statut
                    && (p.MontantPaye == null || p.MontantPaye < p.MontantAPaye))
                .OrderByDescending(p => p.DateCreation)
                .Take(20)
                .ToListAsync(cancellationToken);

            return paiementsEnCours.Select(p => new PaiementEnCoursDto
            {
                IdPaiement = p.IdPaiement,
                Reference = $"PAY-{p.IdPaiement:D6}",
                NomPassager = p.Reservation?.Client?.NomClient ?? "Passager inconnu",
                MontantAPaye = p.MontantAPaye,
                MontantVerse = CaissierTransportMetricsHelper.ResolveMontantPaye(p),
                ResteAPayer = p.ResteAPayeCalcule,
                DatePaiement = CaissierTransportMetricsHelper.ResolveDateEncaissement(p),
                MethodePaiement = p.MethodePaiement ?? "Non spécifié",
                Statut = p.Statut ? "Validé" : "En attente",
                IdReservation = p.IdReservation ?? 0,
                ReferenceReservation = p.IdReservation.HasValue ? $"RES-{p.IdReservation.Value:D6}" : "",
                VoyageInfo = p.Reservation?.Voyage != null
                    ? $"{p.Reservation.Voyage.Destination?.VilleDepart} - {p.Reservation.Voyage.Destination?.VilleArrivee}"
                    : "Voyage non spécifié",
                DateVoyage = p.Reservation?.Voyage?.DateDepart ?? DateTime.MinValue
            }).ToList();
        }

        private async Task<List<PaiementRecentDto>> GetPaiementsRecentsAsync(
            int societeId,
            CancellationToken cancellationToken = default)
        {
            var utilisateurId = ValidateSocieteScope(societeId);

            var paiementsRecents = await _context.Paiements
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Voyage)
                        .ThenInclude(v => v!.Destination)
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Client)
                .Include(p => p.Utilisateur)
                .Where(p => !p.IsDeleted
                    && p.IdUtilisateur == utilisateurId
                    && p.IdSociete == societeId
                    && p.Statut)
                .OrderByDescending(p => p.DatePaiement)
                .ThenByDescending(p => p.DateCreation)
                .Take(20)
                .ToListAsync(cancellationToken);

            return paiementsRecents.Select(p => new PaiementRecentDto
            {
                IdPaiement = p.IdPaiement,
                Reference = $"PAY-{p.IdPaiement:D6}",
                NomPassager = p.Reservation?.Client?.NomClient ?? "Passager inconnu",
                MontantPaye = CaissierTransportMetricsHelper.ResolveMontantPaye(p),
                DatePaiement = CaissierTransportMetricsHelper.ResolveDateEncaissement(p),
                MethodePaiement = p.MethodePaiement ?? "Non spécifié",
                Statut = p.Statut ? "Validé" : "En attente",
                UtilisateurEnregistrement = p.Utilisateur?.NomComplet ?? "Système",
                IdReservation = p.IdReservation ?? 0,
                ReferenceReservation = p.IdReservation.HasValue ? $"RES-{p.IdReservation.Value:D6}" : "",
                VoyageInfo = p.Reservation?.Voyage != null
                    ? $"{p.Reservation.Voyage.Destination?.VilleDepart} - {p.Reservation.Voyage.Destination?.VilleArrivee}"
                    : "Voyage non spécifié",
                DateVoyage = p.Reservation?.Voyage?.DateDepart ?? DateTime.MinValue
            }).ToList();
        }

        private async Task<List<RecetteJournaliereDto>> GetRecettesJournalieresAsync(
            int societeId,
            CancellationToken cancellationToken = default)
        {
            var utilisateurId = ValidateSocieteScope(societeId);
            var (todayUtc, _, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();
            var recettes = new List<RecetteJournaliereDto>();

            for (var i = 6; i >= 0; i--)
            {
                var date = todayUtc.AddDays(-i);
                var nextDate = date.AddDays(1);

                var paiementsJour = await QueryPaiementsEncaissesJourAsync(
                    _context, societeId, utilisateurId, date, nextDate, cancellationToken);

                var reservationsJour = await _context.Reservations
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v!.Vehicule)
                    .Where(r => r.IdSociete == societeId
                        && r.IdUtilisateur == utilisateurId
                        && r.DateReservation >= date
                        && r.DateReservation < nextDate
                        && CaissierTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation))
                    .ToListAsync(cancellationToken);

                var totalMontant = paiementsJour.Sum(CaissierTransportMetricsHelper.ResolveMontantPaye);
                var recettesMethode = CaissierTransportMetricsHelper.BuildRecettesParMethode(paiementsJour);

                recettes.Add(new RecetteJournaliereDto
                {
                    Date = date,
                    MontantTotal = totalMontant,
                    NombreTransactions = paiementsJour.Count,
                    RecetteEspece = recettesMethode.Espece,
                    RecetteMobileMoney = recettesMethode.MobileMoney,
                    RecetteVirement = recettesMethode.Virement,
                    RecetteCarte = recettesMethode.Carte,
                    RecetteAutre = recettesMethode.Autre,
                    NombreBilletsVendus = reservationsJour.Count,
                    RecetteVehiculeStandard = paiementsJour
                        .Where(p => p.Reservation?.Voyage?.Vehicule?.TypeVehicule?.Libelle?.Contains("standard", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(CaissierTransportMetricsHelper.ResolveMontantPaye),
                    RecetteVehiculeVIP = paiementsJour
                        .Where(p => p.Reservation?.Voyage?.Vehicule?.TypeVehicule?.Libelle?.Contains("vip", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(CaissierTransportMetricsHelper.ResolveMontantPaye),
                    RecetteDestinationPrincipale = CaissierTransportMetricsHelper.ComputeRecetteDestinationPrincipale(paiementsJour)
                });
            }

            return recettes;
        }

        private async Task<List<AlerteCaissierDto>> GetAlertesCaissierAsync(
            int societeId,
            CancellationToken cancellationToken = default)
        {
            var utilisateurId = ValidateSocieteScope(societeId);
            var (todayUtc, _, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();
            var nowUtc = DateTime.UtcNow;
            var alertes = new List<AlerteCaissierDto>();

            var paiementsEnAttente = await _context.Paiements
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Client)
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Voyage)
                        .ThenInclude(v => v!.Destination)
                .Where(p => !p.IsDeleted
                    && p.IdUtilisateur == utilisateurId
                    && p.IdSociete == societeId
                    && !p.Statut
                    && p.DateCreation < nowUtc.AddHours(-24))
                .ToListAsync(cancellationToken);

            foreach (var paiement in paiementsEnAttente)
            {
                alertes.Add(new AlerteCaissierDto
                {
                    IdAlerte = alertes.Count + 1,
                    TypeAlerte = "Paiement réservation en attente",
                    Description = $"Paiement en attente depuis plus de 24h pour {paiement.Reservation?.Client?.NomClient}",
                    NiveauCriticite = "Moyenne",
                    DateAlerte = nowUtc,
                    IdPassager = paiement.Reservation?.IdClient ?? 0,
                    NomPassager = paiement.Reservation?.Client?.NomClient ?? "Passager inconnu",
                    MontantConcerne = paiement.MontantAPaye,
                    EstLue = false,
                    IdReservation = paiement.IdReservation,
                    ReferenceReservation = paiement.IdReservation.HasValue ? $"RES-{paiement.IdReservation.Value:D6}" : "",
                    DateVoyage = paiement.Reservation?.Voyage?.DateDepart ?? DateTime.MinValue,
                    Destination = paiement.Reservation?.Voyage?.Destination?.VilleArrivee ?? ""
                });
            }

            var grosMontantsEnAttente = await _context.Paiements
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Client)
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Voyage)
                        .ThenInclude(v => v!.Destination)
                .Where(p => !p.IsDeleted
                    && p.IdUtilisateur == utilisateurId
                    && p.IdSociete == societeId
                    && !p.Statut
                    && p.MontantAPaye > 100000)
                .ToListAsync(cancellationToken);

            foreach (var paiement in grosMontantsEnAttente)
            {
                alertes.Add(new AlerteCaissierDto
                {
                    IdAlerte = alertes.Count + 1,
                    TypeAlerte = "Gros montant réservation en attente",
                    Description = $"Gros montant en attente de validation: {paiement.MontantAPaye:N0} FC pour {paiement.Reservation?.Client?.NomClient}",
                    NiveauCriticite = "Élevée",
                    DateAlerte = nowUtc,
                    IdPassager = paiement.Reservation?.IdClient ?? 0,
                    NomPassager = paiement.Reservation?.Client?.NomClient ?? "Passager inconnu",
                    MontantConcerne = paiement.MontantAPaye,
                    EstLue = false,
                    IdReservation = paiement.IdReservation,
                    ReferenceReservation = paiement.IdReservation.HasValue ? $"RES-{paiement.IdReservation.Value:D6}" : "",
                    DateVoyage = paiement.Reservation?.Voyage?.DateDepart ?? DateTime.MinValue,
                    Destination = paiement.Reservation?.Voyage?.Destination?.VilleArrivee ?? ""
                });
            }

            var demainUtc = todayUtc.AddDays(1);
            var demainFinUtc = demainUtc.AddDays(1);

            var voyagesBientotComplets = await _context.Voyages
                .Include(v => v.Vehicule)
                .Include(v => v.Destination)
                .Where(v => v.IdSociete == societeId
                    && v.DateDepart >= demainUtc
                    && v.DateDepart < demainFinUtc
                    && v.Statut == true)
                .ToListAsync(cancellationToken);

            foreach (var voyage in voyagesBientotComplets)
            {
                var nombreReservations = await _context.Reservations
                    .Where(r => r.IdVoyage == voyage.Id
                        && CaissierTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation))
                    .CountAsync(cancellationToken);

                var placesDisponibles = voyage.Vehicule?.NombreSiege - nombreReservations ?? 0;
                if (placesDisponibles <= 5)
                {
                    alertes.Add(new AlerteCaissierDto
                    {
                        IdAlerte = alertes.Count + 1,
                        TypeAlerte = "Voyage bientôt complet",
                        Description = $"Voyage {voyage.Destination?.VilleDepart} - {voyage.Destination?.VilleArrivee} n'a plus que {placesDisponibles} places disponibles",
                        NiveauCriticite = placesDisponibles <= 2 ? "Élevée" : "Moyenne",
                        DateAlerte = nowUtc,
                        IdPassager = 0,
                        NomPassager = "Système",
                        MontantConcerne = voyage.Prix,
                        EstLue = false,
                        DateVoyage = voyage.DateDepart,
                        Destination = $"{voyage.Destination?.VilleDepart} - {voyage.Destination?.VilleArrivee}"
                    });
                }
            }

            return alertes.OrderByDescending(a => a.DateAlerte).ToList();
        }

        private async Task<ResumeCaisseDto> GetResumeCaisseAsync(
            int societeId,
            CancellationToken cancellationToken = default)
        {
            var utilisateurId = ValidateSocieteScope(societeId);
            var (todayUtc, _, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();
            var tomorrowUtc = todayUtc.AddDays(1);

            var paiementsJour = await QueryPaiementsEncaissesJourAsync(
                _context, societeId, utilisateurId, todayUtc, tomorrowUtc, cancellationToken);

            var totalEntrees = paiementsJour.Sum(CaissierTransportMetricsHelper.ResolveMontantPaye);

            var reservationsJour = await _context.Reservations
                .Include(r => r.Voyage)
                    .ThenInclude(v => v!.Vehicule)
                .Where(r => r.IdSociete == societeId
                    && r.IdUtilisateur == utilisateurId
                    && r.DateReservation >= todayUtc
                    && r.DateReservation < tomorrowUtc
                    && CaissierTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation))
                .ToListAsync(cancellationToken);

            var reservationsConfirmeesJour = reservationsJour.Count;
            var billetsEmisJour = await CountBilletsEmisJourAsync(
                _context, societeId, reservationsJour, todayUtc, tomorrowUtc, cancellationToken);

            var reservationsEnAttenteCount = reservationsJour
                .Count(r => !_context.Paiements
                    .Any(p => p.IdReservation == r.IdReservation && p.Statut));

            var tauxRemplissageMoyen = CaissierTransportMetricsHelper.ComputeTauxRemplissageCaissierJour(reservationsJour);

            return new ResumeCaisseDto
            {
                TotalEntrees = totalEntrees,
                DateCloture = DateTime.UtcNow,
                StatutCaisse = "Ouverte",
                TotalBilletsVendus = reservationsConfirmeesJour,
                ReservationsConfirmeesJour = reservationsConfirmeesJour,
                BilletsEmisJour = billetsEmisJour,
                ReservationsConfirmees = reservationsConfirmeesJour,
                ReservationsEnAttente = reservationsEnAttenteCount,
                TauxRemplissageMoyen = tauxRemplissageMoyen
            };
        }

        private async Task<CaissierPerformancesMensuellesDto> GetPerformancesMensuellesAsync(
            int societeId,
            CancellationToken cancellationToken = default)
        {
            var utilisateurId = ValidateSocieteScope(societeId);
            var (todayUtc, monthStartUtc, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();
            var nextMonthStartUtc = monthStartUtc.AddMonths(1);
            var previousMonthStartUtc = monthStartUtc.AddMonths(-1);

            var candidats = await LoadPaiementsEncaissesCandidatsAsync(
                _context, societeId, utilisateurId, cancellationToken);

            var paiementsMoisEnCours = FilterPaiementsEncaisses(candidats, monthStartUtc, nextMonthStartUtc);
            var paiementsMoisPrecedent = FilterPaiementsEncaisses(candidats, previousMonthStartUtc, monthStartUtc);

            var joursEcoules = Math.Max(1, (todayUtc - monthStartUtc).Days + 1);

            var moisEnCours = await BuildPeriodeStatistiquesAsync(
                societeId,
                utilisateurId,
                monthStartUtc,
                nextMonthStartUtc,
                paiementsMoisEnCours,
                joursEcoules,
                cancellationToken);

            var moisPrecedent = await BuildPeriodeStatistiquesAsync(
                societeId,
                utilisateurId,
                previousMonthStartUtc,
                monthStartUtc,
                paiementsMoisPrecedent,
                joursEcoules: null,
                cancellationToken);

            return new CaissierPerformancesMensuellesDto
            {
                MoisEnCours = moisEnCours,
                MoisPrecedent = moisPrecedent,
                Synthese = new CaissierPerformancesMensuellesSyntheseDto
                {
                    VariationEncaissementsPourcentage = SocieteTransportMetricsHelper.ComputeVariationPercent(
                        moisEnCours.TotalEncaissements, moisPrecedent.TotalEncaissements),
                    VariationTransactionsPourcentage = SocieteTransportMetricsHelper.ComputeVariationPercent(
                        moisEnCours.NombreTransactions, moisPrecedent.NombreTransactions),
                    VariationReservationsPourcentage = SocieteTransportMetricsHelper.ComputeVariationPercent(
                        moisEnCours.ReservationsConfirmees, moisPrecedent.ReservationsConfirmees),
                    VariationBilletsEmisPourcentage = SocieteTransportMetricsHelper.ComputeVariationPercent(
                        moisEnCours.BilletsEmis, moisPrecedent.BilletsEmis)
                }
            };
        }

        private async Task<CaissierPeriodeStatistiquesDto> BuildPeriodeStatistiquesAsync(
            int societeId,
            int utilisateurId,
            DateTime periodeDebutUtc,
            DateTime periodeFinUtc,
            IReadOnlyList<Paiement> paiementsPeriode,
            int? joursEcoules,
            CancellationToken cancellationToken)
        {
            var totalEncaissements = paiementsPeriode.Sum(CaissierTransportMetricsHelper.ResolveMontantPaye);
            var nombreTransactions = paiementsPeriode.Count;
            var recettesMethode = CaissierTransportMetricsHelper.BuildRecettesParMethode(paiementsPeriode);

            var reservationsPeriode = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.IdSociete == societeId
                    && r.IdUtilisateur == utilisateurId
                    && r.DateReservation >= periodeDebutUtc
                    && r.DateReservation < periodeFinUtc
                    && CaissierTransportMetricsHelper.StatutsReservationConfirmes.Contains(r.StatutReservation))
                .ToListAsync(cancellationToken);

            var billetsEmis = await CountBilletsEmisAsync(
                _context, societeId, utilisateurId, periodeDebutUtc, periodeFinUtc, cancellationToken);

            var nombrePassagers = await CountPassagersPayesAsync(paiementsPeriode, cancellationToken);

            decimal? moyenneJournaliere = null;
            if (joursEcoules.HasValue)
            {
                moyenneJournaliere = Math.Round(totalEncaissements / joursEcoules.Value, 2);
            }

            return new CaissierPeriodeStatistiquesDto
            {
                PeriodeDebut = periodeDebutUtc,
                PeriodeFin = periodeFinUtc,
                Libelle = CaissierTransportMetricsHelper.BuildPeriodeLibelle(periodeDebutUtc),
                TotalEncaissements = totalEncaissements,
                NombreTransactions = nombreTransactions,
                MoyenneTransaction = nombreTransactions > 0
                    ? Math.Round(totalEncaissements / nombreTransactions, 2)
                    : 0m,
                NombrePassagers = nombrePassagers,
                ReservationsConfirmees = reservationsPeriode.Count,
                BilletsEmis = billetsEmis,
                JoursEcoules = joursEcoules,
                MoyenneEncaissementsJournaliers = moyenneJournaliere,
                RecetteEspece = recettesMethode.Espece,
                RecetteMobileMoney = recettesMethode.MobileMoney,
                RecetteVirement = recettesMethode.Virement,
                RecetteCarte = recettesMethode.Carte,
                RecetteAutre = recettesMethode.Autre
            };
        }

        private static async Task<List<Paiement>> QueryPaiementsEncaissesAsync(
            CongoTravelDbContext context,
            int societeId,
            int utilisateurId,
            DateTime rangeStartUtc,
            DateTime rangeEndUtc,
            CancellationToken cancellationToken)
        {
            var candidats = await LoadPaiementsEncaissesCandidatsAsync(
                context, societeId, utilisateurId, cancellationToken);

            return FilterPaiementsEncaisses(candidats, rangeStartUtc, rangeEndUtc);
        }

        private static async Task<List<Paiement>> QueryPaiementsEncaissesJourAsync(
            CongoTravelDbContext context,
            int societeId,
            int utilisateurId,
            DateTime dayStartUtc,
            DateTime dayEndUtc,
            CancellationToken cancellationToken) =>
            await QueryPaiementsEncaissesAsync(
                context, societeId, utilisateurId, dayStartUtc, dayEndUtc, cancellationToken);

        private static async Task<List<Paiement>> LoadPaiementsEncaissesCandidatsAsync(
            CongoTravelDbContext context,
            int societeId,
            int utilisateurId,
            CancellationToken cancellationToken) =>
            await context.Paiements
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Voyage)
                        .ThenInclude(v => v!.Vehicule)
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Client)
                .Include(p => p.Reservation)
                    .ThenInclude(r => r!.Voyage)
                        .ThenInclude(v => v!.Destination)
                .Where(p => !p.IsDeleted
                    && p.IdUtilisateur == utilisateurId
                    && p.IdSociete == societeId
                    && p.Statut)
                .ToListAsync(cancellationToken);

        private static List<Paiement> FilterPaiementsEncaisses(
            IReadOnlyList<Paiement> candidats,
            DateTime rangeStartUtc,
            DateTime rangeEndUtc) =>
            candidats
                .Where(p => CaissierTransportMetricsHelper.IsEncaissementInUtcRange(p, rangeStartUtc, rangeEndUtc))
                .ToList();

        private static async Task<int> CountBilletsEmisAsync(
            CongoTravelDbContext context,
            int societeId,
            int utilisateurId,
            DateTime rangeStartUtc,
            DateTime rangeEndUtc,
            CancellationToken cancellationToken)
        {
            var reservationIds = await context.Reservations
                .AsNoTracking()
                .Where(r => r.IdSociete == societeId && r.IdUtilisateur == utilisateurId)
                .Select(r => r.IdReservation)
                .ToListAsync(cancellationToken);

            if (reservationIds.Count == 0)
                return 0;

            return await context.Billets
                .AsNoTracking()
                .CountAsync(b =>
                    b.IdSociete == societeId
                    && b.IdReservation.HasValue
                    && reservationIds.Contains(b.IdReservation.Value)
                    && b.DateGeneration >= rangeStartUtc
                    && b.DateGeneration < rangeEndUtc,
                    cancellationToken);
        }

        private static async Task<int> CountBilletsEmisJourAsync(
            CongoTravelDbContext context,
            int societeId,
            IReadOnlyList<Reservation> reservationsJour,
            DateTime dayStartUtc,
            DateTime dayEndUtc,
            CancellationToken cancellationToken)
        {
            if (reservationsJour.Count == 0)
                return 0;

            var reservationIds = reservationsJour.Select(r => r.IdReservation).ToList();

            return await context.Billets
                .AsNoTracking()
                .CountAsync(b =>
                    b.IdSociete == societeId
                    && b.IdReservation.HasValue
                    && reservationIds.Contains(b.IdReservation.Value)
                    && b.DateGeneration >= dayStartUtc
                    && b.DateGeneration < dayEndUtc,
                    cancellationToken);
        }

        private int GetCurrentUtilisateurId()
        {
            var utilisateurId = _currentUserService.UserId;
            if (utilisateurId == 0)
            {
                throw new UnauthorizedAccessException("ID utilisateur non trouvé");
            }

            return utilisateurId;
        }

        private int ValidateSocieteScope(int societeId)
        {
            var utilisateurId = GetCurrentUtilisateurId();
            var tokenSocieteId = _currentUserService.SocieteId;

            if (tokenSocieteId <= 0 || societeId != tokenSocieteId)
            {
                throw new UnauthorizedAccessException("Accès non autorisé à cette société");
            }

            return utilisateurId;
        }

        private async Task<int> CountPassagersPayesAsync(
            IReadOnlyList<Paiement> paiementsPeriode,
            CancellationToken cancellationToken = default)
        {
            var reservationIds = paiementsPeriode
                .Where(p => p.IdReservation.HasValue)
                .Select(p => p.IdReservation!.Value)
                .Distinct()
                .ToList();

            if (reservationIds.Count == 0)
                return 0;

            var passengers = await _context.ReservationPassengers
                .AsNoTracking()
                .Where(rp => reservationIds.Contains(rp.IdReservation))
                .ToListAsync(cancellationToken);

            var reservationIdsWithPassengers = passengers
                .Where(p => p.Statut)
                .Select(p => p.IdReservation)
                .Distinct()
                .ToHashSet();

            var legacyReservationIds = reservationIds
                .Where(id => !reservationIdsWithPassengers.Contains(id))
                .ToList();

            var legacyReservations = legacyReservationIds.Count == 0
                ? new List<Reservation>()
                : await _context.Reservations
                    .AsNoTracking()
                    .Where(r => legacyReservationIds.Contains(r.IdReservation))
                    .ToListAsync(cancellationToken);

            return CaissierDashboardMetrics.CountPassagersFromPaidReservations(
                reservationIds,
                passengers,
                legacyReservations);
        }
    }
}
