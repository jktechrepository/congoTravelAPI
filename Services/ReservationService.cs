using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class ReservationService : IReservationRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(
            CongoTravelDbContext context,
            IConfigSocieteRepository configSocieteRepository,
            ILogger<ReservationService> logger)
        {
            _context = context;
            _configSocieteRepository = configSocieteRepository;
            _logger = logger;
        }

        // CRUD de base
        public async Task<IEnumerable<Reservation>> GetAllAsync()
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de toutes les réservations");
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetAllBySocieteAsync(int idSociete)
        {
            try
            {
                return await _context.Reservations
                    .ForSociete(idSociete)
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour la société {IdSociete}", idSociete);
                throw;
            }
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .FirstOrDefaultAsync(r => r.IdReservation == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la réservation {ReservationId}", id);
                throw;
            }
        }

        public async Task<Reservation> CreateAsync(Reservation reservation)
        {
            try
            {
                // Validation des clés étrangères
                var utilisateurExists = await _context.Utilisateurs.AnyAsync(u => u.IdUtilisateur == reservation.IdUtilisateur);
                if (!utilisateurExists)
                {
                    throw new ArgumentException($"L'utilisateur avec l'ID {reservation.IdUtilisateur} n'existe pas");
                }

                var clientExists = await _context.Clients.AnyAsync(c => c.IdClient == reservation.IdClient);
                if (!clientExists)
                {
                    throw new ArgumentException($"Le client avec l'ID {reservation.IdClient} n'existe pas");
                }

                var voyage = await _context.Voyages.FirstOrDefaultAsync(v => v.Id == reservation.IdVoyage);
                if (voyage == null)
                {
                    throw new ArgumentException($"Le voyage avec l'ID {reservation.IdVoyage} n'existe pas");
                }

                var config = await _configSocieteRepository.GetOrCreateAsync(voyage.IdSociete);
                await _configSocieteRepository.EnsureReservationsActivesAsync(voyage.IdSociete);
                ConfigSocieteDefaults.EnsureReservationHorizon(voyage, config);

                // Validation du statut de réservation
                var statutsValid = new[] { "EN_ATTENTE", "CONFIRME", "ANNULE" };
                if (!statutsValid.Contains(reservation.StatutReservation))
                {
                    throw new ArgumentException($"Le statut de réservation '{reservation.StatutReservation}' n'est pas valide. Valeurs autorisées : {string.Join(", ", statutsValid)}");
                }

                // Validation de l'unicité (pas deux réservations pour le même voyage et client à la même date)
                var exists = await ExistsByVoyageAndClientAndDateAsync(reservation.IdVoyage, reservation.IdClient, reservation.DateReservation);
                if (exists)
                {
                    throw new InvalidOperationException($"Une réservation existe déjà pour le client {reservation.IdClient} et le voyage {reservation.IdVoyage} à la date {reservation.DateReservation:dd/MM/yyyy}");
                }

                reservation.DateCreation = DateTime.Now;
                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Réservation créée avec succès - ID: {ReservationId}, Utilisateur: {IdUtilisateur}, Client: {IdClient}, Voyage: {IdVoyage}", 
                    reservation.IdReservation, reservation.IdUtilisateur, reservation.IdClient, reservation.IdVoyage);

                return reservation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la réservation");
                throw;
            }
        }

        public async Task<Reservation?> UpdateAsync(Reservation reservation)
        {
            try
            {
                var existingReservation = await _context.Reservations.FindAsync(reservation.IdReservation);
                if (existingReservation == null)
                    return null;

                // Validation des clés étrangères
                var utilisateurExists = await _context.Utilisateurs.AnyAsync(u => u.IdUtilisateur == reservation.IdUtilisateur);
                if (!utilisateurExists)
                {
                    throw new ArgumentException($"L'utilisateur avec l'ID {reservation.IdUtilisateur} n'existe pas");
                }

                var clientExists = await _context.Clients.AnyAsync(c => c.IdClient == reservation.IdClient);
                if (!clientExists)
                {
                    throw new ArgumentException($"Le client avec l'ID {reservation.IdClient} n'existe pas");
                }

                var voyage = await _context.Voyages.FirstOrDefaultAsync(v => v.Id == reservation.IdVoyage);
                if (voyage == null)
                {
                    throw new ArgumentException($"Le voyage avec l'ID {reservation.IdVoyage} n'existe pas");
                }

                var config = await _configSocieteRepository.GetOrCreateAsync(voyage.IdSociete);
                ConfigSocieteDefaults.EnsureReservationHorizon(voyage, config);

                // Validation du statut de réservation
                var statutsValid = new[] { "EN_ATTENTE", "CONFIRME", "ANNULE" };
                if (!statutsValid.Contains(reservation.StatutReservation))
                {
                    throw new ArgumentException($"Le statut de réservation '{reservation.StatutReservation}' n'est pas valide. Valeurs autorisées : {string.Join(", ", statutsValid)}");
                }

                // Validation de l'unicité (sauf pour la même réservation)
                var exists = await _context.Reservations
                    .AnyAsync(r => r.IdVoyage == reservation.IdVoyage && 
                                  r.IdClient == reservation.IdClient && 
                                  r.DateReservation == reservation.DateReservation && 
                                  r.IdReservation != reservation.IdReservation);
                
                if (exists)
                {
                    throw new InvalidOperationException($"Une réservation existe déjà pour le client {reservation.IdClient} et le voyage {reservation.IdVoyage} à la date {reservation.DateReservation:dd/MM/yyyy}");
                }

                existingReservation.IdUtilisateur = reservation.IdUtilisateur;
                existingReservation.IdClient = reservation.IdClient;
                existingReservation.IdVoyage = reservation.IdVoyage;
                existingReservation.IdSite = reservation.IdSite;
                existingReservation.StatutReservation = reservation.StatutReservation;
                existingReservation.Statut = reservation.Statut;
                existingReservation.DateReservation = reservation.DateReservation;
                existingReservation.DateModification = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Réservation mise à jour avec succès - ID: {ReservationId}", reservation.IdReservation);

                return existingReservation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la réservation {ReservationId}", reservation.IdReservation);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var reservation = await _context.Reservations.FindAsync(id);
                if (reservation == null)
                    return false;

                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Réservation supprimée avec succès - ID: {ReservationId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la réservation {ReservationId}", id);
                throw;
            }
        }

        // Méthodes de recherche
        public async Task<IEnumerable<Reservation>> GetByUtilisateurAsync(int idUtilisateur)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.IdUtilisateur == idUtilisateur)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour l'utilisateur {UtilisateurId}", idUtilisateur);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetByClientAsync(int idClient)
        {
            try
            {
                return await _context.Reservations
                    .AsNoTracking()
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Passagers)
                    .Include(r => r.Voyage!)
                        .ThenInclude(v => v.Destination)
                    .Include(r => r.Voyage!)
                        .ThenInclude(v => v.Vehicule)
                    .Where(r => r.IdClient == idClient)
                    .OrderByDescending(r => r.DateCreation)
                    .ThenBy(r => r.IdReservation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour le client {ClientId}", idClient);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetByVoyageAsync(int idVoyage)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.IdVoyage == idVoyage)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour le voyage {VoyageId}", idVoyage);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetByStatutReservationAsync(string statutReservation)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.StatutReservation == statutReservation)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations avec statut {StatutReservation}", statutReservation);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetByDateAsync(DateTime date)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.DateReservation.Date == date.Date)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour la date {Date}", date);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetByDateRangeAsync(DateTime dateDebut, DateTime dateFin)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.DateReservation.Date >= dateDebut.Date && r.DateReservation.Date <= dateFin.Date)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations entre {DateDebut} et {DateFin}", dateDebut, dateFin);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetByUtilisateurAndClientAsync(int idUtilisateur, int idClient)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.IdUtilisateur == idUtilisateur && r.IdClient == idClient)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour l'utilisateur {UtilisateurId} et le client {ClientId}", idUtilisateur, idClient);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetByVoyageAndStatutAsync(int idVoyage, string statutReservation)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.IdVoyage == idVoyage && r.StatutReservation == statutReservation)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour le voyage {VoyageId} et statut {StatutReservation}", idVoyage, statutReservation);
                throw;
            }
        }

        // Méthodes de filtrage
        public async Task<IEnumerable<Reservation>> GetByStatutAsync(bool statut)
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.Statut == statut)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations avec statut {Statut}", statut);
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetActiveAsync()
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.Statut == true)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations actives");
                throw;
            }
        }

        public async Task<IEnumerable<Reservation>> GetInactiveAsync()
        {
            try
            {
                return await _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.Statut == false)
                    .OrderByDescending(r => r.DateCreation)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations inactives");
                throw;
            }
        }

        // Méthodes d'existence
        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.Reservations.AnyAsync(r => r.IdReservation == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence de la réservation {ReservationId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByVoyageAndClientAsync(int idVoyage, int idClient)
        {
            try
            {
                return await _context.Reservations.AnyAsync(r => r.IdVoyage == idVoyage && r.IdClient == idClient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence de réservation pour le voyage {VoyageId} et le client {ClientId}", idVoyage, idClient);
                throw;
            }
        }

        public async Task<bool> ExistsByVoyageAndClientAndDateAsync(int idVoyage, int idClient, DateTime date)
        {
            try
            {
                return await _context.Reservations.AnyAsync(r => r.IdVoyage == idVoyage && 
                                                             r.IdClient == idClient && 
                                                             r.DateReservation.Date == date.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence de réservation pour le voyage {VoyageId}, le client {ClientId} et la date {Date}", idVoyage, idClient, date);
                throw;
            }
        }

        // Pagination
        public async Task<PagedResult<Reservation>> GetPagedAsync(PagedRequest request)
        {
            try
            {
                var query = _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .AsQueryable();

                if (request.IdSociete.HasValue && request.IdSociete.Value > 0)
                    query = query.ForSociete(request.IdSociete.Value);

                // Filtrage par terme de recherche
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(r => 
                        r.IdReservation.ToString().Contains(request.SearchTerm) ||
                        r.StatutReservation.Contains(request.SearchTerm) ||
                        r.DateReservation.ToString("dd/MM/yyyy").Contains(request.SearchTerm) ||
                        (r.Utilisateur != null && r.Utilisateur.NomComplet.Contains(request.SearchTerm)) ||
                        (r.Client != null && r.Client.NomClient.Contains(request.SearchTerm)) ||
                        (r.Voyage != null && r.Voyage.Prix.ToString().Contains(request.SearchTerm)) ||
                        (r.Voyage != null && r.Voyage.Vehicule != null && r.Voyage.Vehicule.AliasVehicule.Contains(request.SearchTerm)) ||
                        (r.Voyage != null && r.Voyage.Destination != null && r.Voyage.Destination.VilleDepart.Contains(request.SearchTerm)) ||
                        (r.Voyage != null && r.Voyage.Destination != null && r.Voyage.Destination.VilleArrivee.Contains(request.SearchTerm)));
                }

                var totalCount = await query.CountAsync();

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.DateReservation)
                                : query.OrderBy(r => r.DateReservation);
                            break;
                        case "statut":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.StatutReservation)
                                : query.OrderBy(r => r.StatutReservation);
                            break;
                        case "utilisateur":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.Utilisateur != null ? r.Utilisateur.NomComplet : "")
                                : query.OrderBy(r => r.Utilisateur != null ? r.Utilisateur.NomComplet : "");
                            break;
                        case "client":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.Client != null ? r.Client.NomClient : "")
                                : query.OrderBy(r => r.Client != null ? r.Client.NomClient : "");
                            break;
                        default:
                            query = query.OrderByDescending(r => r.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(r => r.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Reservation>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des réservations");
                throw;
            }
        }

        public async Task<PagedResult<Reservation>> GetByUtilisateurPagedAsync(int idUtilisateur, PagedRequest request)
        {
            try
            {
                var query = _context.Reservations
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.IdUtilisateur == idUtilisateur)
                    .AsQueryable();

                // Filtrage par terme de recherche
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(r => 
                        r.IdReservation.ToString().Contains(request.SearchTerm) ||
                        r.StatutReservation.Contains(request.SearchTerm) ||
                        r.DateReservation.ToString("dd/MM/yyyy").Contains(request.SearchTerm) ||
                        (r.Client != null && r.Client.NomClient.Contains(request.SearchTerm)) ||
                        (r.Voyage != null && r.Voyage.Prix.ToString().Contains(request.SearchTerm)) ||
                        (r.Voyage != null && r.Voyage.Vehicule != null && r.Voyage.Vehicule.AliasVehicule.Contains(request.SearchTerm)));
                }

                var totalCount = await query.CountAsync();

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.DateReservation)
                                : query.OrderBy(r => r.DateReservation);
                            break;
                        case "statut":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.StatutReservation)
                                : query.OrderBy(r => r.StatutReservation);
                            break;
                        default:
                            query = query.OrderByDescending(r => r.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(r => r.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Reservation>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des réservations pour l'utilisateur {UtilisateurId}", idUtilisateur);
                throw;
            }
        }

        public async Task<PagedResult<Reservation>> GetByClientPagedAsync(int idClient, PagedRequest request)
        {
            try
            {
                var query = _context.Reservations
                    .AsNoTracking()
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Passagers)
                    .Include(r => r.Voyage!)
                        .ThenInclude(v => v.Destination)
                    .Include(r => r.Voyage!)
                        .ThenInclude(v => v.Vehicule)
                    .Where(r => r.IdClient == idClient)
                    .AsQueryable();

                // Filtrage par terme de recherche
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(r => 
                        r.IdReservation.ToString().Contains(request.SearchTerm) ||
                        r.StatutReservation.Contains(request.SearchTerm) ||
                        r.DateReservation.ToString("dd/MM/yyyy").Contains(request.SearchTerm) ||
                        (r.Utilisateur != null && r.Utilisateur.NomComplet.Contains(request.SearchTerm)) ||
                        (r.Voyage != null && r.Voyage.Prix.ToString().Contains(request.SearchTerm)) ||
                        (r.Voyage != null && r.Voyage.Vehicule != null && r.Voyage.Vehicule.AliasVehicule.Contains(request.SearchTerm)));
                }

                var totalCount = await query.CountAsync();

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.DateReservation)
                                : query.OrderBy(r => r.DateReservation);
                            break;
                        case "statut":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.StatutReservation)
                                : query.OrderBy(r => r.StatutReservation);
                            break;
                        default:
                            query = query.OrderByDescending(r => r.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(r => r.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Reservation>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des réservations pour le client {ClientId}", idClient);
                throw;
            }
        }

        public async Task<PagedResult<Reservation>> GetByVoyagePagedAsync(int idVoyage, PagedRequest request)
        {
            try
            {
                var query = _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.IdVoyage == idVoyage)
                    .AsQueryable();

                // Filtrage par terme de recherche
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(r => 
                        r.IdReservation.ToString().Contains(request.SearchTerm) ||
                        r.StatutReservation.Contains(request.SearchTerm) ||
                        r.DateReservation.ToString("dd/MM/yyyy").Contains(request.SearchTerm) ||
                        (r.Utilisateur != null && r.Utilisateur.NomComplet.Contains(request.SearchTerm)) ||
                        (r.Client != null && r.Client.NomClient.Contains(request.SearchTerm)));
                }

                var totalCount = await query.CountAsync();

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.DateReservation)
                                : query.OrderBy(r => r.DateReservation);
                            break;
                        case "statut":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.StatutReservation)
                                : query.OrderBy(r => r.StatutReservation);
                            break;
                        default:
                            query = query.OrderByDescending(r => r.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(r => r.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Reservation>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des réservations pour le voyage {VoyageId}", idVoyage);
                throw;
            }
        }

        public async Task<PagedResult<Reservation>> GetByStatutReservationPagedAsync(string statutReservation, PagedRequest request)
        {
            try
            {
                var query = _context.Reservations
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Vehicule)
                            .ThenInclude(vh => vh.TypeVehicule)
                    .Include(r => r.Voyage)
                        .ThenInclude(v => v.Destination)
                    .Where(r => r.StatutReservation == statutReservation)
                    .AsQueryable();

                // Filtrage par terme de recherche
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(r => 
                        r.IdReservation.ToString().Contains(request.SearchTerm) ||
                        r.DateReservation.ToString("dd/MM/yyyy").Contains(request.SearchTerm) ||
                        (r.Utilisateur != null && r.Utilisateur.NomComplet.Contains(request.SearchTerm)) ||
                        (r.Client != null && r.Client.NomClient.Contains(request.SearchTerm)) ||
                        (r.Voyage != null && r.Voyage.Prix.ToString().Contains(request.SearchTerm)) ||
                        (r.Voyage != null && r.Voyage.Vehicule != null && r.Voyage.Vehicule.AliasVehicule.Contains(request.SearchTerm)));
                }

                var totalCount = await query.CountAsync();

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "date":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.DateReservation)
                                : query.OrderBy(r => r.DateReservation);
                            break;
                        case "utilisateur":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.Utilisateur != null ? r.Utilisateur.NomComplet : "")
                                : query.OrderBy(r => r.Utilisateur != null ? r.Utilisateur.NomComplet : "");
                            break;
                        case "client":
                            query = request.SortDescending 
                                ? query.OrderByDescending(r => r.Client != null ? r.Client.NomClient : "")
                                : query.OrderBy(r => r.Client != null ? r.Client.NomClient : "");
                            break;
                        default:
                            query = query.OrderByDescending(r => r.DateCreation);
                            break;
                    }
                }
                else
                {
                    query = query.OrderByDescending(r => r.DateCreation);
                }

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<Reservation>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des réservations avec statut {StatutReservation}", statutReservation);
                throw;
            }
        }

        // Compteurs
        public async Task<int> CountAsync()
        {
            try
            {
                return await _context.Reservations.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations");
                throw;
            }
        }

        public async Task<int> CountByUtilisateurAsync(int idUtilisateur)
        {
            try
            {
                return await _context.Reservations.CountAsync(r => r.IdUtilisateur == idUtilisateur);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations pour l'utilisateur {UtilisateurId}", idUtilisateur);
                throw;
            }
        }

        public async Task<int> CountByClientAsync(int idClient)
        {
            try
            {
                return await _context.Reservations.CountAsync(r => r.IdClient == idClient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations pour le client {ClientId}", idClient);
                throw;
            }
        }

        public async Task<int> CountByVoyageAsync(int idVoyage)
        {
            try
            {
                return await _context.Reservations.CountAsync(r => r.IdVoyage == idVoyage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations pour le voyage {VoyageId}", idVoyage);
                throw;
            }
        }

        public async Task<int> CountByStatutReservationAsync(string statutReservation)
        {
            try
            {
                return await _context.Reservations.CountAsync(r => r.StatutReservation == statutReservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations avec statut {StatutReservation}", statutReservation);
                throw;
            }
        }

        public async Task<int> CountByDateAsync(DateTime date)
        {
            try
            {
                return await _context.Reservations.CountAsync(r => r.DateReservation.Date == date.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations pour la date {Date}", date);
                throw;
            }
        }

        public async Task<int> CountByStatutAsync(bool statut)
        {
            try
            {
                return await _context.Reservations.CountAsync(r => r.Statut == statut);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations avec statut {Statut}", statut);
                throw;
            }
        }

        public async Task<int> CountActiveAsync()
        {
            try
            {
                return await _context.Reservations.CountAsync(r => r.Statut == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations actives");
                throw;
            }
        }

        public async Task<int> CountInactiveAsync()
        {
            try
            {
                return await _context.Reservations.CountAsync(r => r.Statut == false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations inactives");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ReservationPassenger>> GetPassagersByReservationAsync(int idReservation)
        {
            try
            {
                return await _context.ReservationPassengers
                    .Where(p => p.IdReservation == idReservation)
                    .OrderBy(p => p.IdReservationPassenger)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture des passagers pour la réservation {ReservationId}", idReservation);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Reservation>?> GetBySocieteWithPassagersAsync(int idSociete)
        {
            try
            {
                var societeExists = await _context.Societes.AsNoTracking()
                    .AnyAsync(s => s.IdSociete == idSociete);
                if (!societeExists)
                    return null;

                var reservations = await _context.Reservations
                    .AsNoTracking()
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage!)
                        .ThenInclude(v => v.Destination)
                    .Include(r => r.Voyage!)
                        .ThenInclude(v => v.Vehicule)
                    .Include(r => r.Passagers)
                    .Where(r => r.IdSociete == idSociete)
                    .OrderByDescending(r => r.DateCreation)
                    .ThenBy(r => r.IdReservation)
                    .ToListAsync();

                return reservations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors de la lecture des réservations pour la société {IdSociete}",
                    idSociete);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Reservation>?> GetBySocieteAndVoyageWithPassagersAsync(int idSociete, int idVoyage)
        {
            try
            {
                var voyageOk = await _context.Voyages.AsNoTracking()
                    .AnyAsync(v => v.Id == idVoyage && v.IdSociete == idSociete);
                if (!voyageOk)
                    return null;

                var reservations = await _context.Reservations
                    .AsNoTracking()
                    .Include(r => r.Utilisateur)
                    .Include(r => r.Client)
                    .Include(r => r.Voyage!)
                        .ThenInclude(v => v.Destination)
                    .Include(r => r.Voyage!)
                        .ThenInclude(v => v.Vehicule)
                    .Include(r => r.Passagers)
                    .Where(r => r.IdSociete == idSociete && r.IdVoyage == idVoyage)
                    .OrderBy(r => r.DateCreation)
                    .ThenBy(r => r.IdReservation)
                    .ToListAsync();

                return reservations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors de la lecture des réservations société {IdSociete} voyage {IdVoyage}",
                    idSociete, idVoyage);
                throw;
            }
        }
    }
}
