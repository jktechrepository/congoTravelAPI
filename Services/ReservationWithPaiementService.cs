using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.ConfigSociete;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Services.Repositories;
using CongoTravel.Helpers;
using CongoTravel.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour la création de réservations avec paiement en une seule transaction atomique
    /// </summary>
    public class ReservationWithPaiementService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<ReservationWithPaiementService> _logger;
        private readonly IReservationRepository _reservationRepository;
        private readonly IPaiementRepository _paiementRepository;
        private readonly BilletEmissionService _billetEmissionService;
        private readonly IVoyageSeatAllocationService _seatAllocationService;
        private readonly IVoyageTarifService _voyageTarifService;
        private readonly IBilletPricingEnrichmentService _billetPricingEnrichment;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly ICurrentUserService _currentUserService;

        public ReservationWithPaiementService(
            CongoTravelDbContext context,
            ILogger<ReservationWithPaiementService> logger,
            IReservationRepository reservationRepository,
            IPaiementRepository paiementRepository,
            BilletEmissionService billetEmissionService,
            IVoyageSeatAllocationService seatAllocationService,
            IVoyageTarifService voyageTarifService,
            IBilletPricingEnrichmentService billetPricingEnrichment,
            IConfigSocieteRepository configSocieteRepository,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _reservationRepository = reservationRepository;
            _paiementRepository = paiementRepository;
            _billetEmissionService = billetEmissionService;
            _seatAllocationService = seatAllocationService;
            _voyageTarifService = voyageTarifService;
            _billetPricingEnrichment = billetPricingEnrichment;
            _configSocieteRepository = configSocieteRepository;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Crée une réservation avec paiement en une seule transaction atomique
        /// </summary>
        /// <param name="dto">DTO contenant les données de réservation et de paiement</param>
        /// <returns>Résultat de l'opération avec réservation, paiement et billet si émis</returns>
        public async Task<ReservationWithPaiementResponseDto> CreateReservationWithPaiementAsync(
            CreateReservationWithPaiementDto dto)
        {
            var transactionId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            _logger.LogInformation("Début transaction unifiée réservation+paiement - TransactionID: {TransactionId}", transactionId);

            // Utiliser CreateExecutionStrategy() pour gérer TOUTES les opérations automatiquement
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                IDbContextTransaction? unitOfWork = null;
                if (_context.Database.IsRelational())
                    unitOfWork = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    // 1. Validation complète des données
                    await ValidateAllDataAsync(dto, transactionId);

                    var origine = OrigineOperationResolver.Resolve(_currentUserService);

                    // 2. Création de la réservation
                    var reservation = await CreateReservationAsync(dto.Reservation, origine, transactionId);

                    // 3. Passagers + attribution sièges (workflow V2)
                    var passengerIds = await CreateReservationPassengersAsync(dto.Reservation, reservation, transactionId);
                    if (dto.Reservation.Passagers == null || passengerIds.Count != dto.Reservation.Passagers.Count)
                        throw new InvalidOperationException("Incohérence interne passagers/catégories pour l'allocation des sièges.");
                    var allocationRequests = dto.Reservation.Passagers!
                        .Select((p, idx) => (IdReservationPassenger: passengerIds[idx], IdCategorieSiege: p.IdCategorieSiege))
                        .ToList();

                    IReadOnlyList<VoyageSeatAllocation> allocations;
                    using (_logger.BeginScope(new Dictionary<string, object>
                           {
                               ["TransactionId"] = transactionId,
                               ["IdVoyage"] = reservation.IdVoyage,
                               ["IdReservation"] = reservation.IdReservation
                           }))
                    {
                        allocations = await _seatAllocationService.AllocateSeatsForPassengersAsync(
                            reservation.IdVoyage,
                            reservation.IdReservation,
                            allocationRequests);
                    }

                    _logger.LogInformation(
                        "Sièges attribués — TransactionID={TransactionId}, IdReservation={IdReservation}, Passagers=[{PassengerIds}], IdSieges=[{SiegeIds}]",
                        transactionId,
                        reservation.IdReservation,
                        string.Join(',', passengerIds),
                        string.Join(',', allocations.Select(a => a.IdSiege)));

                    var voyageTarif = await _context.Voyages.AsNoTracking()
                        .FirstOrDefaultAsync(v => v.Id == reservation.IdVoyage);
                    if (voyageTarif == null)
                        throw new InvalidOperationException($"Voyage {reservation.IdVoyage} introuvable après attribution des sièges.");

                    var montantAttendu = await _voyageTarifService.ComputeTotalForSiegesAsync(
                        voyageTarif.Id,
                        allocations.Select(a => a.IdSiege).ToList(),
                        voyageTarif.Prix);

                    const decimal toleranceMontant = 0.05m;
                    if (Math.Abs(dto.Paiement.MontantAPaye - montantAttendu) > toleranceMontant)
                    {
                        throw new InvalidOperationException(
                            $"Montant à payer incohérent avec les tarifs des sièges attribués : attendu {montantAttendu}, reçu {dto.Paiement.MontantAPaye}.");
                    }

                    // 4. Création du paiement
                    var paiement = await CreatePaiementAsync(dto.Paiement, reservation.IdReservation, origine, transactionId);

                    // 5. Émission billet(s) si paiement complet
                    List<Billet>? billets = null;
                    if (paiement.EstComplet)
                    {
                        _logger.LogInformation(
                            "Émission billet(s) — TransactionID: {TransactionId}, PaiementID: {PaiementId}",
                            transactionId,
                            paiement.IdPaiement);

                        try
                        {
                            billets = (await EmitBilletsPourPaiementInternalAsync(paiement, transactionId)).ToList();
                            reservation.StatutReservation = "CONFIRMEE";

                            _logger.LogInformation(
                                "Billet(s) émis — TransactionID: {TransactionId}, Count: {Count}",
                                transactionId,
                                billets.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Échec émission billet(s) — TransactionID: {TransactionId}, PaiementID: {PaiementId}",
                                transactionId,
                                paiement.IdPaiement);

                            reservation.StatutReservation = "CONFIRMEE";
                            _logger.LogWarning(
                                "Paiement validé mais billet(s) non émis — TransactionID: {TransactionId}",
                                transactionId);
                        }
                    }
                    else
                    {
                        reservation.StatutReservation = "EN_ATTENTE";
                        _logger.LogInformation(
                            "Paiement partiel — aucun billet — TransactionID: {TransactionId}",
                            transactionId);
                    }

                    await _context.SaveChangesAsync();

                    if (unitOfWork != null)
                        await unitOfWork.CommitAsync();

                    _logger.LogInformation(
                        "Transaction unifiée réussie — TransactionID: {TransactionId}, Réservation: {ReservationId}, Paiement: {PaiementId}",
                        transactionId,
                        reservation.IdReservation,
                        paiement.IdPaiement);

                    return await BuildResponseAsync(reservation, paiement, billets, transactionId);
                }
                catch (Exception ex)
                {
                    if (unitOfWork != null)
                        await unitOfWork.RollbackAsync();

                    _logger.LogError(ex, "Échec transaction unifiée - TransactionID: {TransactionId}", transactionId);
                    
                    return new ReservationWithPaiementResponseDto
                    {
                        TransactionId = transactionId,
                        Statut = TransactionStatut.Echec,
                        Message = "La transaction a échoué: " + ex.Message,
                        DateCreation = DateTime.UtcNow
                    };
                }
                finally
                {
                    if (unitOfWork != null)
                        await unitOfWork.DisposeAsync();
                }
            });
        }

        /// <summary>
        /// Valide toutes les données avant la création (DÉSACTIVÉ POUR DÉBOGAGE)
        /// </summary>
        private async Task ValidateAllDataAsync(CreateReservationWithPaiementDto dto, string transactionId)
        {
            _logger.LogDebug("Validation des données - TransactionID: {TransactionId}", transactionId);

            if (dto.Paiement.MontantPaye > dto.Paiement.MontantAPaye)
                throw new InvalidOperationException("Le montant payé ne peut pas dépasser le montant à payer");

            var voyage = await _context.Voyages
                .Include(v => v.Vehicule)
                .FirstOrDefaultAsync(v => v.Id == dto.Reservation.IdVoyage);

            if (voyage == null)
                throw new InvalidOperationException($"Voyage {dto.Reservation.IdVoyage} introuvable.");

            var config = await _configSocieteRepository.GetOrCreateAsync(voyage.IdSociete);
            await _configSocieteRepository.EnsureReservationsActivesAsync(voyage.IdSociete);
            ConfigSocieteDefaults.EnsureReservationHorizon(voyage, config);

            if (voyage.Vehicule == null)
                throw new InvalidOperationException("Véhicule introuvable pour ce voyage.");

            var societeOp = voyage.IdSociete;
            if (dto.Reservation.IdSociete > 0 && dto.Reservation.IdSociete != societeOp)
                throw new InvalidOperationException(
                    $"La société de la réservation ({dto.Reservation.IdSociete}) ne correspond pas au voyage ({societeOp}).");

            if (dto.Paiement.IdSociete > 0 && dto.Paiement.IdSociete != societeOp)
                throw new InvalidOperationException(
                    $"La société du paiement ({dto.Paiement.IdSociete}) ne correspond pas au voyage ({societeOp}).");

            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(_context, dto.Reservation.IdSite, societeOp);
            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(_context, dto.Paiement.IdSite, societeOp);

            if (dto.Reservation.IdSite.HasValue && dto.Paiement.IdSite.HasValue &&
                dto.Reservation.IdSite.Value != dto.Paiement.IdSite.Value)
                throw new InvalidOperationException(
                    "Les sites réservation et paiement doivent être identiques lorsque les deux sont renseignées.");

            var demandees = dto.Reservation.NombreDePlace;
            if (demandees > voyage.Vehicule.NombreSiege)
                throw new InvalidOperationException(
                    $"Le nombre de places demandées ({demandees}) dépasse la capacité du véhicule ({voyage.Vehicule.NombreSiege}).");

            var prises = await _context.VoyageSeatAllocations.CountAsync(a =>
                a.IdVoyage == voyage.Id && a.Statut == "CONFIRME");

            var disponibles = voyage.Vehicule.NombreSiege - prises;
            if (disponibles < demandees)
                throw new InvalidOperationException(
                    $"Places insuffisantes sur ce voyage (disponibles: {disponibles}, demandées: {demandees}).");

            var passagers = dto.Reservation.Passagers;
            if (passagers == null || passagers.Count == 0)
            {
                throw new InvalidOperationException(
                    "Fournissez Reservation.Passagers avec un passager par place et sa catégorie de siège.");
            }

            if (passagers.Count != dto.Reservation.NombreDePlace)
                throw new InvalidOperationException(
                    "Le nombre d'entrées dans Passagers doit être égal à NombreDePlace.");

            foreach (var p in passagers)
            {
                if (string.IsNullOrWhiteSpace(p.NomComplet))
                    throw new InvalidOperationException("Chaque passager doit avoir un nom complet.");
                if (p.IdCategorieSiege <= 0)
                    throw new InvalidOperationException("Chaque passager doit avoir une catégorie de siège valide.");
            }

            var idsCategorie = passagers.Select(p => p.IdCategorieSiege).Distinct().ToList();
            var categoriesCount = await _context.CategorieSieges.AsNoTracking()
                .CountAsync(c => idsCategorie.Contains(c.IdCategorieSiege) && c.IdSociete == societeOp && c.Statut);
            if (categoriesCount != idsCategorie.Count)
                throw new InvalidOperationException(
                    "Une ou plusieurs catégories de siège sont invalides pour la société du voyage.");

            var clientOk = await _context.Clients.AnyAsync(c => c.IdClient == dto.Reservation.IdClient);
            if (!clientOk)
                throw new InvalidOperationException($"Client {dto.Reservation.IdClient} introuvable.");
        }

        /// <summary>
        /// Crée la réservation dans la transaction (sans transaction imbriquée)
        /// </summary>
        private async Task<Reservation> CreateReservationAsync(
            ReservationDataDto reservationData,
            string origine,
            string transactionId)
        {
            _logger.LogDebug("Création réservation - TransactionID: {TransactionId}", transactionId);

            var reservation = new Reservation
            {
                IdVoyage = reservationData.IdVoyage,
                IdClient = reservationData.IdClient,
                IdUtilisateur = reservationData.IdUtilisateur > 0 ? reservationData.IdUtilisateur : 1, // Forcer valeur positive
                IdSociete = reservationData.IdSociete > 0 ? reservationData.IdSociete : 1, // Forcer valeur positive
                IdSite = reservationData.IdSite,
                NombreDePlace = reservationData.NombreDePlace,
                DateReservation = DateTime.UtcNow,
                StatutReservation = "EN_ATTENTE", // Sera mis à jour après paiement
                Statut = true,
                Origine = origine
            };

            // Ajout direct au contexte pour éviter les transactions imbriquées
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Réservation créée - TransactionID: {TransactionId}, RéservationID: {ReservationId}", 
                transactionId, reservation.IdReservation);

            return reservation;
        }

        /// <summary>
        /// Crée les lignes passagers et retourne leurs IDs dans l’ordre d’insertion.
        /// </summary>
        private async Task<IReadOnlyList<int>> CreateReservationPassengersAsync(
            ReservationDataDto data,
            Reservation reservation,
            string transactionId)
        {
            var utcNow = DateTime.UtcNow;
            var ids = new List<int>();

            if (data.Passagers != null && data.Passagers.Count > 0)
            {
                var added = new List<ReservationPassenger>();
                foreach (var p in data.Passagers)
                {
                    var rp = new ReservationPassenger
                    {
                        IdReservation = reservation.IdReservation,
                        IdClient = p.IdClient,
                        NomComplet = p.NomComplet.Trim(),
                        Telephone = string.IsNullOrWhiteSpace(p.Telephone) ? null : p.Telephone.Trim(),
                        Email = string.IsNullOrWhiteSpace(p.Email) ? null : p.Email.Trim(),
                        DocumentType = string.IsNullOrWhiteSpace(p.DocumentType) ? null : p.DocumentType.Trim(),
                        DocumentNumero = string.IsNullOrWhiteSpace(p.DocumentNumero) ? null : p.DocumentNumero.Trim(),
                        Genre = string.IsNullOrWhiteSpace(p.Genre) ? null : p.Genre.Trim(),
                        IdSociete = reservation.IdSociete,
                        Statut = true,
                        DateCreation = utcNow
                    };
                    added.Add(rp);
                    _context.ReservationPassengers.Add(rp);
                }

                await _context.SaveChangesAsync();

                ids.AddRange(added.OrderBy(e => e.IdReservationPassenger).Select(e => e.IdReservationPassenger));
                _logger.LogDebug(
                    "{Count} passagers créés — TransactionID: {TransactionId}",
                    ids.Count,
                    transactionId);

                return ids;
            }

            throw new InvalidOperationException(
                "Reservation.Passagers est requis pour ce workflow (un passager par place).");
        }

        /// <summary>
        /// Crée le paiement dans la transaction (sans transaction imbriquée)
        /// </summary>
        private async Task<Paiement> CreatePaiementAsync(
            PaiementDataDto paiementData,
            int idReservation,
            string origine,
            string transactionId)
        {
            _logger.LogDebug("Création paiement - TransactionID: {TransactionId}", transactionId);

            var paiement = new Paiement
            {
                MontantAPaye = paiementData.MontantAPaye,
                MontantPaye = paiementData.MontantPaye,
                MethodePaiement = MethodePaiementHelper.NormalizeForStorage(paiementData.MethodePaiement),
                ReferenceTransaction = paiementData.ReferenceTransaction,
                Statut = true,
                StatutPaiementMetier = (int)StatutPaiementMetier.Reussi,
                IdUtilisateur = paiementData.IdUtilisateur > 0 ? paiementData.IdUtilisateur : 1, // Forcer valeur positive
                IdReservation = idReservation,
                IdSociete = paiementData.IdSociete > 0 ? paiementData.IdSociete : 1, // Forcer valeur positive
                IdSite = paiementData.IdSite,
                DateCreation = DateTime.UtcNow,
                Origine = origine
            };

            paiement.MettreAJourResteAPaye();

            // Ajout direct au contexte pour éviter les transactions imbriquées
            _context.Paiements.Add(paiement);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Paiement créé - TransactionID: {TransactionId}, PaiementID: {PaiementId}", 
                transactionId, paiement.IdPaiement);

            return paiement;
        }

        /// <summary>
        /// Émet un ou plusieurs billets et rattache le premier au paiement (<see cref="Paiement.IdBilletEmis"/>).
        /// </summary>
        private async Task<IReadOnlyList<Billet>> EmitBilletsPourPaiementInternalAsync(
            Paiement paiement,
            string transactionId)
        {
            _logger.LogInformation(
                "Émission billet(s) — TransactionID: {TransactionId}, PaiementID: {PaiementId}",
                transactionId,
                paiement.IdPaiement);

            try
            {
                var list = await _billetEmissionService.EmitBilletsPourPaiementAsync(paiement);

                if (list.Count > 0)
                {
                    paiement.DateEmissionBillet = DateTime.UtcNow;
                    paiement.IdBilletEmis = list[0].IdBillet;
                    await _context.SaveChangesAsync();
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Échec émission billet(s) — TransactionID: {TransactionId}, PaiementID: {PaiementId}",
                    transactionId,
                    paiement.IdPaiement);

                throw new InvalidOperationException("Paiement créé mais échec de l'émission du/des billet(s).", ex);
            }
        }

        /// <summary>
        /// Construit la réponse DTO (recharge les billets avec voyage / siège / tarifs pour <see cref="BilletResponseDto.PrixVoyage"/>).
        /// </summary>
        private async Task<ReservationWithPaiementResponseDto> BuildResponseAsync(
            Reservation reservation,
            Paiement paiement,
            IReadOnlyList<Billet>? billets,
            string transactionId)
        {
            var liste = await ToBilletResponseDtosAsync(billets);

            return new ReservationWithPaiementResponseDto
            {
                TransactionId = transactionId,
                Reservation = MapToReservationResponse(reservation),
                Paiement = MapToPaiementResponse(paiement),
                Billets = liste,
                Billet = liste.FirstOrDefault(),
                DateCreation = DateTime.UtcNow,
                Statut = TransactionStatut.Succes,
                Message = "Réservation créée avec succès"
            };
        }

        /// <summary>
        /// Map Reservation vers ReservationResponseDto
        /// </summary>
        private ReservationResponseDto MapToReservationResponse(Reservation reservation)
        {
            return new ReservationResponseDto
            {
                IdReservation = reservation.IdReservation,
                IdVoyage = reservation.IdVoyage,
                IdClient = reservation.IdClient,
                IdUtilisateur = reservation.IdUtilisateur,
                IdSociete = reservation.IdSociete,
                IdSite = reservation.IdSite,
                StatutReservation = reservation.StatutReservation,
                Statut = reservation.Statut,
                DateReservation = reservation.DateReservation,
                DateCreation = reservation.DateCreation,
                DateModification = reservation.DateModification,
                Origine = reservation.Origine
            };
        }

        /// <summary>
        /// Map Paiement vers PaiementResponseDto
        /// </summary>
        private CongoTravel.Models.DTOs.Reservation.PaiementResponseDto MapToPaiementResponse(Paiement paiement) =>
            PaiementResponseMapper.Map(paiement);

        /// <summary>
        /// Recharge les billets avec le graphe nécessaire au calcul du prix par catégorie de siège.
        /// </summary>
        private async Task<List<CongoTravel.Models.DTOs.BilletResponseDto>> ToBilletResponseDtosAsync(
            IReadOnlyList<Billet>? billets)
        {
            if (billets == null || billets.Count == 0)
                return new List<CongoTravel.Models.DTOs.BilletResponseDto>();

            var ids = billets.Select(b => b.IdBillet).ToList();
            var loaded = await _context.Billets
                .AsNoTracking()
                .Include(b => b.Siege)
                .Include(b => b.ReservationPassenger)
                .Include(b => b.Reservation)
                    .ThenInclude(r => r!.Voyage)
                        .ThenInclude(v => v!.VoyageTarifsCategorieSiege)
                .Where(b => ids.Contains(b.IdBillet))
                .OrderBy(b => b.IdReservationPassenger)
                .ToListAsync();

            var dtos = loaded.Select(MapToBilletResponse).ToList();
            await _billetPricingEnrichment.EnrichPrixVoyageAsync(loaded, dtos);
            return dtos;
        }

        /// <summary>
        /// Map Billet vers BilletResponseDto (prix complété par <see cref="IBilletPricingEnrichmentService"/>).
        /// </summary>
        private CongoTravel.Models.DTOs.BilletResponseDto MapToBilletResponse(Billet billet)
        {
            return new CongoTravel.Models.DTOs.BilletResponseDto
            {
                IdBillet = billet.IdBillet,
                IsUsed = billet.IsUsed,
                QrCode = billet.QrCode ?? string.Empty,
                DateGeneration = billet.DateGeneration,
                DateValiditeDebut = billet.DateValiditeDebut,
                DateValiditeFin = billet.DateValiditeFin,
                IdReservation = billet.IdReservation,
                IdReservationPassenger = billet.IdReservationPassenger,
                IdSiege = billet.IdSiege,
                CodeSiege = billet.CodeSiege,
                NomPassager = billet.ReservationPassenger?.NomComplet,
                IdSociete = billet.IdSociete,
                IdSite = billet.IdSite,
                DateCreation = billet.DateCreation,
                DateModification = billet.DateModification
            };
        }
    }
}
