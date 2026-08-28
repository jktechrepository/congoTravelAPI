using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service responsable de l'émission automatique des billets suite à un paiement complet
    /// </summary>
    public class BilletEmissionService
    {
        private readonly IBilletRepository _billetRepository;
        private readonly IQrCodeService _qrCodeService;
        private readonly CongoTravelDbContext _context;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly ILogger<BilletEmissionService> _logger;

        public BilletEmissionService(
            IBilletRepository billetRepository,
            IQrCodeService qrCodeService,
            CongoTravelDbContext context,
            IConfigSocieteRepository configSocieteRepository,
            ILogger<BilletEmissionService> logger)
        {
            _billetRepository = billetRepository;
            _qrCodeService = qrCodeService;
            _context = context;
            _configSocieteRepository = configSocieteRepository;
            _logger = logger;
        }

        /// <summary>
        /// Émet un ou plusieurs billets (un par passager si workflow V2), sinon billet unique legacy.
        /// </summary>
        public async Task<IReadOnlyList<Billet>> EmitBilletsPourPaiementAsync(Paiement paiement)
        {
            _logger.LogInformation(
                "Début émission billet(s) - Paiement: {PaiementId}, Montant: {Montant}",
                paiement.IdPaiement,
                paiement.MontantPaye);

            await ValiderConditionsEmissionAsync(paiement);

            if (!paiement.IdReservation.HasValue)
            {
                var infosSansRes = await ExtraireInformationsBilletAsync(paiement);
                var qrSansRes = await _qrCodeService.GenerateUniqueQrCodeAsync(
                    infosSansRes.IdSociete,
                    infosSansRes.IdReservation);
                var billetSansRes = new Billet
                {
                    IdReservation = infosSansRes.IdReservation,
                    IdClient = infosSansRes.IdClient,
                    QrCode = qrSansRes,
                    DateGeneration = DateTime.UtcNow,
                    IdSociete = infosSansRes.IdSociete,
                    IdSite = paiement.IdSite
                };
                var createdSansRes = await _billetRepository.CreateAsync(billetSansRes);
                return new List<Billet> { createdSansRes };
            }

            var reservation = await _context.Reservations
                .Include(r => r.Voyage)
                .FirstOrDefaultAsync(r => r.IdReservation == paiement.IdReservation.Value);

            if (reservation == null)
                throw new InvalidOperationException(
                    $"Réservation {paiement.IdReservation.Value} introuvable pour le paiement {paiement.IdPaiement}.");

            var idSociete = reservation.Voyage?.IdSociete ?? paiement.IdSociete;
            var config = await _configSocieteRepository.GetOrCreateAsync(idSociete);

            var passagers = await _context.ReservationPassengers
                .Where(p => p.IdReservation == reservation.IdReservation)
                .OrderBy(p => p.IdReservationPassenger)
                .ToListAsync();

            if (passagers.Count == 0)
            {
                var infos = await ExtraireInformationsBilletAsync(paiement);
                var qrCode = await _qrCodeService.GenerateUniqueQrCodeAsync(
                    infos.IdSociete,
                    infos.IdReservation);
                var billet = new Billet
                {
                    IdReservation = infos.IdReservation,
                    IdClient = infos.IdClient,
                    QrCode = qrCode,
                    DateGeneration = DateTime.UtcNow,
                    IdSociete = infos.IdSociete,
                    IdSite = paiement.IdSite
                };
                ApplyBilletValidityFromConfig(billet, reservation.Voyage, config.DureeValiditeBilletJours);
                var billetEmis = await _billetRepository.CreateAsync(billet);
                _logger.LogInformation(
                    "Billet legacy émis - ID: {BilletId}, Paiement: {PaiementId}",
                    billetEmis.IdBillet,
                    paiement.IdPaiement);
                return new List<Billet> { billetEmis };
            }

            var passengerIds = passagers.Select(p => p.IdReservationPassenger).ToList();
            var allocations = await _context.VoyageSeatAllocations
                .Include(a => a.Siege)
                .Where(a => a.IdVoyage == reservation.IdVoyage && passengerIds.Contains(a.IdReservationPassenger))
                .ToListAsync();

            if (allocations.Count != passagers.Count)
                throw new InvalidOperationException(
                    $"Attribution de sièges incomplète pour la réservation {reservation.IdReservation} ({allocations.Count}/{passagers.Count}).");

            var orderedAllocations = passengerIds
                .Select(pid => allocations.First(a => a.IdReservationPassenger == pid))
                .ToList();

            var results = new List<Billet>();
            foreach (var alloc in orderedAllocations)
            {
                var passenger = passagers.First(p => p.IdReservationPassenger == alloc.IdReservationPassenger);
                var qrCode = await _qrCodeService.GenerateUniqueQrCodeAsync(
                    paiement.IdSociete,
                    reservation.IdReservation);

                var billet = new Billet
                {
                    IdReservation = reservation.IdReservation,
                    IdReservationPassenger = passenger.IdReservationPassenger,
                    IdSiege = alloc.IdSiege,
                    CodeSiege = alloc.Siege?.CodeSiege,
                    IdClient = passenger.IdClient ?? reservation.IdClient,
                    QrCode = qrCode,
                    DateGeneration = DateTime.UtcNow,
                    IdSociete = paiement.IdSociete,
                    IdSite = paiement.IdSite
                };
                ApplyBilletValidityFromConfig(billet, reservation.Voyage, config.DureeValiditeBilletJours);

                results.Add(await _billetRepository.CreateAsync(billet));
            }

            _logger.LogInformation(
                "{Count} billet(s) émis pour paiement {PaiementId}",
                results.Count,
                paiement.IdPaiement);

            return results;
        }

        /// <summary>
        /// Émet les billets pour une réservation déjà allouée (ex. leg retour d'un aller-retour).
        /// Ne vérifie pas <see cref="Paiement.IdBilletEmis"/>.
        /// </summary>
        public async Task<IReadOnlyList<Billet>> EmitBilletsPourReservationAsync(
            int idReservation,
            int idSociete,
            int? idSite)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Voyage)
                .FirstOrDefaultAsync(r => r.IdReservation == idReservation)
                ?? throw new InvalidOperationException($"Réservation {idReservation} introuvable.");

            var config = await _configSocieteRepository.GetOrCreateAsync(
                reservation.Voyage?.IdSociete ?? idSociete);

            var passagers = await _context.ReservationPassengers
                .Where(p => p.IdReservation == reservation.IdReservation)
                .OrderBy(p => p.IdReservationPassenger)
                .ToListAsync();

            if (passagers.Count == 0)
                throw new InvalidOperationException(
                    $"Aucun passager pour la réservation {idReservation}.");

            foreach (var p in passagers)
            {
                if (await _context.Billets.AnyAsync(b => b.IdReservationPassenger == p.IdReservationPassenger))
                    throw new InvalidOperationException(
                        $"Un billet existe déjà pour le passager {p.IdReservationPassenger}.");
            }

            var passengerIds = passagers.Select(p => p.IdReservationPassenger).ToList();
            var allocations = await _context.VoyageSeatAllocations
                .Include(a => a.Siege)
                .Where(a => a.IdVoyage == reservation.IdVoyage && passengerIds.Contains(a.IdReservationPassenger))
                .ToListAsync();

            if (allocations.Count != passagers.Count)
                throw new InvalidOperationException(
                    $"Attribution de sièges incomplète pour la réservation {reservation.IdReservation} ({allocations.Count}/{passagers.Count}).");

            var orderedAllocations = passengerIds
                .Select(pid => allocations.First(a => a.IdReservationPassenger == pid))
                .ToList();

            var results = new List<Billet>();
            foreach (var alloc in orderedAllocations)
            {
                var passenger = passagers.First(p => p.IdReservationPassenger == alloc.IdReservationPassenger);
                var qrCode = await _qrCodeService.GenerateUniqueQrCodeAsync(idSociete, reservation.IdReservation);

                var billet = new Billet
                {
                    IdReservation = reservation.IdReservation,
                    IdReservationPassenger = passenger.IdReservationPassenger,
                    IdSiege = alloc.IdSiege,
                    CodeSiege = alloc.Siege?.CodeSiege,
                    IdClient = passenger.IdClient ?? reservation.IdClient,
                    QrCode = qrCode,
                    DateGeneration = DateTime.UtcNow,
                    IdSociete = idSociete,
                    IdSite = idSite
                };
                ApplyBilletValidityFromConfig(billet, reservation.Voyage, config.DureeValiditeBilletJours);
                results.Add(await _billetRepository.CreateAsync(billet));
            }

            _logger.LogInformation(
                "{Count} billet(s) émis pour réservation {IdReservation}",
                results.Count,
                idReservation);

            return results;
        }

        /// <summary>
        /// Émet les billets puis retourne le premier (compatibilité callers mono-billet).
        /// </summary>
        public async Task<Billet> EmitreBilletAsync(Paiement paiement)
        {
            var list = await EmitBilletsPourPaiementAsync(paiement);
            return list.Count > 0
                ? list[0]
                : throw new InvalidOperationException($"Aucun billet émis pour le paiement {paiement.IdPaiement}.");
        }

        /// <summary>
        /// Vérifie si un billet peut être émis pour ce paiement
        /// </summary>
        public async Task<bool> PeutEmettreBilletAsync(Paiement paiement)
        {
            try
            {
                await ValiderConditionsEmissionAsync(paiement);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valide les conditions préalables à l'émission d'un billet
        /// </summary>
        private async Task ValiderConditionsEmissionAsync(Paiement paiement)
        {
            if (!paiement.EstComplet)
            {
                throw new InvalidOperationException(
                    $"Paiement {paiement.IdPaiement} incomplet - impossible d'émettre un billet");
            }

            if (paiement.IdBilletEmis.HasValue)
            {
                throw new InvalidOperationException(
                    $"Un billet (ID: {paiement.IdBilletEmis}) a déjà été émis pour le paiement {paiement.IdPaiement}");
            }

            if (paiement.IdReservation.HasValue)
            {
                var idRes = paiement.IdReservation.Value;
                var passagersIds = await _context.ReservationPassengers
                    .Where(p => p.IdReservation == idRes)
                    .Select(p => p.IdReservationPassenger)
                    .ToListAsync();

                if (passagersIds.Count > 0)
                {
                    foreach (var pid in passagersIds)
                    {
                        if (await _context.Billets.AnyAsync(b => b.IdReservationPassenger == pid))
                        {
                            throw new InvalidOperationException(
                                $"Un billet existe déjà pour le passager {pid}");
                        }
                    }

                    if (await _context.Billets.AnyAsync(b =>
                            b.IdReservation == idRes && b.IdReservationPassenger == null))
                    {
                        throw new InvalidOperationException(
                            $"La réservation {idRes} comporte déjà un billet sans passager.");
                    }
                }
                else
                {
                    var billetsExistants = await _billetRepository.GetByReservationAsync(idRes);
                    if (billetsExistants.Any())
                    {
                        throw new InvalidOperationException(
                            $"Un billet existe déjà pour la réservation {idRes}");
                    }
                }
            }

            if (paiement.IdSociete <= 0)
            {
                throw new ArgumentException("ID de société invalide");
            }

            _logger.LogDebug("Validations pré-émission réussies pour paiement {PaiementId}", paiement.IdPaiement);
        }

        /// <summary>
        /// Extrait les informations nécessaires pour la création du billet (parcours sans passagers).
        /// </summary>
        private async Task<InformationsBillet> ExtraireInformationsBilletAsync(Paiement paiement)
        {
            var informations = new InformationsBillet
            {
                IdSociete = paiement.IdSociete,
                IdReservation = paiement.IdReservation
            };

            if (paiement.IdReservation.HasValue)
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Client)
                    .FirstOrDefaultAsync(r => r.IdReservation == paiement.IdReservation.Value);

                if (reservation != null)
                {
                    informations.IdClient = reservation.IdClient;
                    informations.ClientExiste = true;
                }
                else
                {
                    _logger.LogWarning(
                        "Réservation {IdReservation} non trouvée pour paiement {PaiementId}",
                        paiement.IdReservation,
                        paiement.IdPaiement);
                }
            }

            return informations;
        }

        private class InformationsBillet
        {
            public int IdSociete { get; set; }
            public int? IdReservation { get; set; }
            public int? IdClient { get; set; }
            public bool ClientExiste { get; set; }
        }

        private static void ApplyBilletValidityFromConfig(Billet billet, Voyage? voyage, int dureeValiditeBilletJours)
        {
            if (voyage == null)
                return;

            BilletValidityHelper.ApplyToBillet(billet, voyage.DateDepart, dureeValiditeBilletJours);
        }
    }
}
