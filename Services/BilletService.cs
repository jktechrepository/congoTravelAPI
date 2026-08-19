using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CongoTravel.Services
{
    public partial class BilletService : IBilletRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly IVoyageTarifService _voyageTarifService;
        private readonly ISiegeDisponibiliteService _siegeDisponibiliteService;
        private readonly ILogger<BilletService> _logger;

        public BilletService(
            CongoTravelDbContext context,
            IConfigSocieteRepository configSocieteRepository,
            IVoyageTarifService voyageTarifService,
            ISiegeDisponibiliteService siegeDisponibiliteService,
            ILogger<BilletService> logger)
        {
            _context = context;
            _configSocieteRepository = configSocieteRepository;
            _voyageTarifService = voyageTarifService;
            _siegeDisponibiliteService = siegeDisponibiliteService;
            _logger = logger;
        }

        private IQueryable<Billet> QueryBilletsWithEmbarquementIncludes() =>
            _context.Billets
                .Include(b => b.Societe)
                .Include(b => b.Site)
                .Include(b => b.ReservationPassenger)
                .Include(b => b.Siege!)
                    .ThenInclude(s => s.CategorieSiege)
                .Include(b => b.Reservation)
                    .ThenInclude(r => r!.Utilisateur)
                .Include(b => b.Reservation)
                    .ThenInclude(r => r!.Client)
                .Include(b => b.Reservation)
                    .ThenInclude(r => r!.Voyage!)
                        .ThenInclude(v => v.Destination)
                .Include(b => b.Reservation)
                    .ThenInclude(r => r!.Voyage!)
                        .ThenInclude(v => v.Vehicule!)
                            .ThenInclude(veh => veh.TypeVehicule);

        private IQueryable<Billet> QueryBilletsForOperationalLookup() =>
            _context.Billets
                .Include(b => b.ReservationPassenger)
                .Include(b => b.Reservation)
                    .ThenInclude(r => r!.Voyage);

        private IQueryable<Billet> QueryBilletsForQrCodeRead() =>
            _context.Billets
                .Include(b => b.ReservationPassenger)
                .Include(b => b.Siege!)
                    .ThenInclude(s => s.CategorieSiege)
                .Include(b => b.Reservation)
                    .ThenInclude(r => r!.Utilisateur)
                .Include(b => b.Reservation)
                    .ThenInclude(r => r!.Client)
                .Include(b => b.Reservation)
                    .ThenInclude(r => r!.Voyage!)
                        .ThenInclude(v => v.Destination)
                .Include(b => b.Reservation)
                    .ThenInclude(r => r!.Voyage!)
                        .ThenInclude(v => v.Vehicule!)
                            .ThenInclude(veh => veh.TypeVehicule);

        private async Task PopulateOptionalSocietesAsync(IEnumerable<Billet> billets)
        {
            var list = billets
                .Where(b => b.Societe == null && b.IdSociete > 0)
                .ToList();
            if (list.Count == 0)
                return;

            var societeIds = list
                .Select(b => b.IdSociete)
                .Distinct()
                .ToList();

            var societes = await _context.Societes
                .AsNoTracking()
                .Where(s => societeIds.Contains(s.IdSociete))
                .ToDictionaryAsync(s => s.IdSociete);

            foreach (var billet in list)
            {
                if (societes.TryGetValue(billet.IdSociete, out var societe))
                    billet.Societe = societe;
            }
        }

        private async Task<Billet?> GetBilletForOperationalLookupByIdAsync(int id)
        {
            var billet = await QueryBilletsForOperationalLookup()
                .FirstOrDefaultAsync(b => b.IdBillet == id);

            if (billet != null)
                await PopulateOptionalSocietesAsync(new[] { billet });

            return billet;
        }

        private async Task<Billet?> GetBilletForOperationalLookupByQrCodeAsync(string qrCode)
        {
            var billet = await QueryBilletsForOperationalLookup()
                .FirstOrDefaultAsync(b => b.QrCode == qrCode);

            if (billet != null)
                await PopulateOptionalSocietesAsync(new[] { billet });

            return billet;
        }

        // CRUD de base
        public async Task<IEnumerable<Billet>> GetAllAsync()
        {
            try
            {
                return await QueryBilletsWithEmbarquementIncludes()
                    .OrderByDescending(b => b.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les billets");
                throw;
            }
        }

        public async Task<IEnumerable<Billet>> GetAllBySocieteAsync(int idSociete)
        {
            try
            {
                return await QueryBilletsWithEmbarquementIncludes()
                    .ForSociete(idSociete)
                    .OrderByDescending(b => b.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des billets pour la société {IdSociete}", idSociete);
                throw;
            }
        }

        public async Task<Billet?> GetByIdAsync(int id)
        {
            try
            {
                return await QueryBilletsWithEmbarquementIncludes()
                    .FirstOrDefaultAsync(b => b.IdBillet == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du billet {id}", id);
                throw;
            }
        }
        public async Task<Billet> CreateAsync(Billet billet)
        {
            try
            {
                // Validation de la clé étrangère réservation (si présente)
                if (billet.IdReservation.HasValue)
                {
                    var reservationExists = await _context.Reservations.AnyAsync(r => r.IdReservation == billet.IdReservation.Value);
                    if (!reservationExists)
                    {
                        throw new ArgumentException($"La réservation avec l'ID {billet.IdReservation} n'existe pas");
                    }
                }

                // Validation de l'unicité du QR Code
                var qrCodeExists = await ExistsByQrCodeAsync(billet.QrCode);
                if (qrCodeExists)
                {
                    throw new InvalidOperationException($"Un billet avec le QR Code '{billet.QrCode}' existe déjà");
                }

                // Un billet par passager (V2) ; sinon un seul billet « legacy » sans passager par réservation
                if (billet.IdReservationPassenger.HasValue)
                {
                    var passengerBilletExists = await _context.Billets.AnyAsync(b => b.IdReservationPassenger == billet.IdReservationPassenger);
                    if (passengerBilletExists)
                    {
                        throw new InvalidOperationException($"Un billet existe déjà pour le passager {billet.IdReservationPassenger.Value}");
                    }
                }
                else if (billet.IdReservation.HasValue)
                {
                    var legacyDup = await _context.Billets.AnyAsync(b =>
                        b.IdReservation == billet.IdReservation && b.IdReservationPassenger == null);
                    if (legacyDup)
                    {
                        throw new InvalidOperationException($"Un billet sans passager existe déjà pour la réservation {billet.IdReservation.Value}");
                    }
                }

                billet.DateCreation = DateTime.Now;
                _context.Billets.Add(billet);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Billet créé avec succès - ID: {BilletId}, Réservation: {IdReservation}, QR Code: {QrCode}", 
                    billet.IdBillet, billet.IdReservation, billet.QrCode);

                return billet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du billet");
                throw;
            }
        }

        public async Task<Billet?> UpdateAsync(Billet billet)
        {
            try
            {
                var existingBillet = await _context.Billets.FindAsync(billet.IdBillet);
                if (existingBillet == null)
                    return null;

                if (billet.IdReservation.HasValue)
                {
                    var reservationExists = await _context.Reservations.AnyAsync(r => r.IdReservation == billet.IdReservation.Value);
                    if (!reservationExists)
                    {
                        throw new ArgumentException($"La réservation avec l'ID {billet.IdReservation} n'existe pas");
                    }
                }

                // Validation de l'unicité du QR Code (sauf pour le même billet)
                var qrCodeExists = await _context.Billets
                    .AnyAsync(b => b.QrCode == billet.QrCode && b.IdBillet != billet.IdBillet);
                
                if (qrCodeExists)
                {
                    throw new InvalidOperationException($"Un billet avec le QR Code '{billet.QrCode}' existe déjà");
                }

                if (billet.IdReservationPassenger.HasValue)
                {
                    var passengerDup = await _context.Billets.AnyAsync(b =>
                        b.IdReservationPassenger == billet.IdReservationPassenger && b.IdBillet != billet.IdBillet);
                    if (passengerDup)
                    {
                        throw new InvalidOperationException($"Un billet existe déjà pour ce passager");
                    }
                }
                else if (billet.IdReservation.HasValue)
                {
                    var legacyDup = await _context.Billets.AnyAsync(b =>
                        b.IdReservation == billet.IdReservation && b.IdReservationPassenger == null && b.IdBillet != billet.IdBillet);
                    if (legacyDup)
                    {
                        throw new InvalidOperationException($"Un billet sans passager existe déjà pour cette réservation");
                    }
                }

                existingBillet.IdReservation = billet.IdReservation;
                existingBillet.IdReservationPassenger = billet.IdReservationPassenger;
                existingBillet.IdSiege = billet.IdSiege;
                existingBillet.CodeSiege = billet.CodeSiege;
                existingBillet.QrCode = billet.QrCode;
                existingBillet.DateGeneration = billet.DateGeneration;
                existingBillet.DateModification = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Billet mis à jour avec succès - ID: {BilletId}", billet.IdBillet);

                return existingBillet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du billet {id}", billet.IdBillet);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var billet = await _context.Billets.FindAsync(id);
                if (billet == null)
                    return false;

                _context.Billets.Remove(billet);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Billet supprimé avec succès - ID: {id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du billet {id}", id);
                throw;
            }
        }

        // Méthodes de recherche
        public async Task<IEnumerable<Billet>> GetByReservationAsync(int idReservation)
        {
            try
            {
                return await QueryBilletsWithEmbarquementIncludes()
                    .Where(b => b.IdReservation == idReservation)
                    .OrderByDescending(b => b.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des billets pour la réservation {idReservation}", idReservation);
                throw;
            }
        }

        public async Task<IEnumerable<Billet>> GetByQrCodeAsync(string qrCode)
        {
            try
            {
                var billets = await QueryBilletsForQrCodeRead()
                    .Where(b => b.QrCode.Contains(qrCode))
                    .OrderByDescending(b => b.DateCreation)
                    .ToListAsync();

                await PopulateOptionalSocietesAsync(billets);
                return billets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des billets avec QR Code {qrCode}", qrCode);
                throw;
            }
        }

        public async Task<IEnumerable<Billet>> GetByDateGenerationAsync(DateTime dateGeneration)
        {
            try
            {
                return await QueryBilletsWithEmbarquementIncludes()
                    .Where(b => b.DateGeneration.Date == dateGeneration.Date)
                    .OrderByDescending(b => b.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des billets pour la date {dateGeneration}", dateGeneration);
                throw;
            }
        }

        public async Task<IEnumerable<Billet>> GetByDateRangeAsync(DateTime dateDebut, DateTime dateFin)
        {
            try
            {
                return await QueryBilletsWithEmbarquementIncludes()
                    .Where(b => b.DateGeneration.Date >= dateDebut.Date && b.DateGeneration.Date <= dateFin.Date)
                    .OrderByDescending(b => b.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des billets entre {dateDebut} et {dateFin}", dateDebut, dateFin);
                throw;
            }
        }

        // Méthodes d'existence
        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.Billets.AnyAsync(b => b.IdBillet == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence du billet {id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByQrCodeAsync(string qrCode)
        {
            try
            {
                return await _context.Billets.AnyAsync(b => b.QrCode == qrCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence du billet avec QR Code {qrCode}", qrCode);
                throw;
            }
        }

        public async Task<bool> ExistsByReservationAsync(int idReservation)
        {
            try
            {
                return await _context.Billets.AnyAsync(b => b.IdReservation == idReservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence du billet pour la réservation {idReservation}", idReservation);
                throw;
            }
        }

        public async Task<bool> ExistsByQrCodeAndReservationAsync(string qrCode, int idReservation)
        {
            try
            {
                return await _context.Billets.AnyAsync(b => b.QrCode == qrCode && b.IdReservation == idReservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence du billet avec QR Code {qrCode} et réservation {idReservation}", qrCode, idReservation);
                throw;
            }
        }

        // Pagination
        public async Task<PagedResult<Billet>> GetPagedAsync(PagedRequest request)
        {
            try
            {
                var query = QueryBilletsWithEmbarquementIncludes().AsQueryable();

                // Filtrage par terme de recherche
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(b => 
                        b.IdBillet.ToString().Contains(request.SearchTerm) ||
                        b.QrCode.Contains(request.SearchTerm) ||
                        b.DateGeneration.ToString("dd/MM/yyyy").Contains(request.SearchTerm) ||
                        (b.Reservation != null && b.Reservation.StatutReservation.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Utilisateur != null && b.Reservation.Utilisateur.NomComplet.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Client != null && b.Reservation.Client.NomClient.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Voyage != null && b.Reservation.Voyage.Prix.ToString().Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Voyage != null && b.Reservation.Voyage.Vehicule != null && b.Reservation.Voyage.Vehicule.AliasVehicule.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Voyage != null && b.Reservation.Voyage.Destination != null && b.Reservation.Voyage.Destination.VilleDepart.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Voyage != null && b.Reservation.Voyage.Destination != null && b.Reservation.Voyage.Destination.VilleArrivee.Contains(request.SearchTerm)));
                }

                var totalCount = await query.CountAsync();

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending 
                                ? query.OrderByDescending(b => b.DateGeneration)
                                : query.OrderBy(b => b.DateGeneration);
                            break;
                        case "qrCode":
                            query = request.SortDescending 
                                ? query.OrderByDescending(b => b.QrCode)
                                : query.OrderBy(b => b.QrCode);
                            break;
                        case "reservation":
                            query = request.SortDescending 
                                ? query.OrderByDescending(b => b.Reservation != null ? b.Reservation.StatutReservation : "")
                                : query.OrderBy(b => b.Reservation != null ? b.Reservation.StatutReservation : "");
                            break;
                        case "utilisateur":
                            query = request.SortDescending 
                                ? query.OrderByDescending(b => b.Reservation != null && b.Reservation.Utilisateur != null ? b.Reservation.Utilisateur.NomComplet : "")
                                : query.OrderBy(b => b.Reservation != null && b.Reservation.Utilisateur != null ? b.Reservation.Utilisateur.NomComplet : "");
                            break;
                        case "client":
                            query = request.SortDescending 
                                ? query.OrderByDescending(b => b.Reservation != null && b.Reservation.Client != null ? b.Reservation.Client.NomClient : "")
                                : query.OrderBy(b => b.Reservation != null && b.Reservation.Client != null ? b.Reservation.Client.NomClient : "");
                            break;
                        default:
                            query = query.OrderByDescending(b => b.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(b => b.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Billet>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des billets");
                throw;
            }
        }

        public async Task<PagedResult<Billet>> GetByReservationPagedAsync(int idReservation, PagedRequest request)
        {
            try
            {
                var query = QueryBilletsWithEmbarquementIncludes()
                    .Where(b => b.IdReservation == idReservation)
                    .AsQueryable();

                // Filtrage par terme de recherche
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(b => 
                        b.IdBillet.ToString().Contains(request.SearchTerm) ||
                        b.QrCode.Contains(request.SearchTerm) ||
                        b.DateGeneration.ToString("dd/MM/yyyy").Contains(request.SearchTerm) ||
                        (b.Reservation != null && b.Reservation.StatutReservation.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Utilisateur != null && b.Reservation.Utilisateur.NomComplet.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Client != null && b.Reservation.Client.NomClient.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Voyage != null && b.Reservation.Voyage.Prix.ToString().Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Voyage != null && b.Reservation.Voyage.Vehicule != null && b.Reservation.Voyage.Vehicule.AliasVehicule.Contains(request.SearchTerm)));
                }

                var totalCount = await query.CountAsync();

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending 
                                ? query.OrderByDescending(b => b.DateGeneration)
                                : query.OrderBy(b => b.DateGeneration);
                            break;
                        case "qrCode":
                            query = request.SortDescending 
                                ? query.OrderByDescending(b => b.QrCode)
                                : query.OrderBy(b => b.QrCode);
                            break;
                        default:
                            query = query.OrderByDescending(b => b.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(b => b.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Billet>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des billets pour la réservation {idReservation}", idReservation);
                throw;
            }
        }

        public async Task<PagedResult<Billet>> GetByDateGenerationPagedAsync(DateTime dateGeneration, PagedRequest request)
        {
            try
            {
                var query = QueryBilletsWithEmbarquementIncludes()
                    .Where(b => b.DateGeneration.Date == dateGeneration.Date)
                    .AsQueryable();

                // Filtrage par terme de recherche
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(b => 
                        b.IdBillet.ToString().Contains(request.SearchTerm) ||
                        b.QrCode.Contains(request.SearchTerm) ||
                        (b.Reservation != null && b.Reservation.StatutReservation.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Utilisateur != null && b.Reservation.Utilisateur.NomComplet.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Client != null && b.Reservation.Client.NomClient.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Voyage != null && b.Reservation.Voyage.Prix.ToString().Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Voyage != null && b.Reservation.Voyage.Vehicule != null && b.Reservation.Voyage.Vehicule.AliasVehicule.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Voyage != null && b.Reservation.Voyage.Destination != null && b.Reservation.Voyage.Destination.VilleDepart.Contains(request.SearchTerm)) ||
                        (b.Reservation != null && b.Reservation.Voyage != null && b.Reservation.Voyage.Destination != null && b.Reservation.Voyage.Destination.VilleArrivee.Contains(request.SearchTerm)));
                }

                var totalCount = await query.CountAsync();

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "qrCode":
                            query = request.SortDescending 
                                ? query.OrderByDescending(b => b.QrCode)
                                : query.OrderBy(b => b.QrCode);
                            break;
                        case "reservation":
                            query = request.SortDescending 
                                ? query.OrderByDescending(b => b.Reservation != null ? b.Reservation.StatutReservation : "")
                                : query.OrderBy(b => b.Reservation != null ? b.Reservation.StatutReservation : "");
                            break;
                        case "utilisateur":
                            query = request.SortDescending 
                                ? query.OrderByDescending(b => b.Reservation != null && b.Reservation.Utilisateur != null ? b.Reservation.Utilisateur.NomComplet : "")
                                : query.OrderBy(b => b.Reservation != null && b.Reservation.Utilisateur != null ? b.Reservation.Utilisateur.NomComplet : "");
                            break;
                        case "client":
                            query = request.SortDescending 
                                ? query.OrderByDescending(b => b.Reservation != null && b.Reservation.Client != null ? b.Reservation.Client.NomClient : "")
                                : query.OrderBy(b => b.Reservation != null && b.Reservation.Client != null ? b.Reservation.Client.NomClient : "");
                            break;
                        default:
                            query = query.OrderByDescending(b => b.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(b => b.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Billet>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des billets pour la date {dateGeneration}", dateGeneration);
                throw;
            }
        }

        // Compteurs
        public async Task<int> CountAsync()
        {
            try
            {
                return await _context.Billets.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des billets");
                throw;
            }
        }

        public async Task<int> CountByReservationAsync(int idReservation)
        {
            try
            {
                return await _context.Billets.CountAsync(b => b.IdReservation == idReservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des billets pour la réservation {idReservation}", idReservation);
                throw;
            }
        }

        public async Task<int> CountByDateGenerationAsync(DateTime dateGeneration)
        {
            try
            {
                return await _context.Billets.CountAsync(b => b.DateGeneration.Date == dateGeneration.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des billets pour la date {dateGeneration}", dateGeneration);
                throw;
            }
        }

        public async Task<int> CountByDateRangeAsync(DateTime dateDebut, DateTime dateFin)
        {
            try
            {
                return await _context.Billets.CountAsync(b => b.DateGeneration.Date >= dateDebut.Date && b.DateGeneration.Date <= dateFin.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des billets entre {dateDebut} et {dateFin}", dateDebut, dateFin);
                throw;
            }
        }

    }
}
