using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    public interface IReservationRepository
    {
        // CRUD de base
        Task<IEnumerable<Reservation>> GetAllAsync();
        Task<IEnumerable<Reservation>> GetAllBySocieteAsync(int idSociete);
        Task<Reservation?> GetByIdAsync(int id);
        Task<Reservation> CreateAsync(Reservation reservation);
        Task<Reservation?> UpdateAsync(Reservation reservation);
        Task<bool> DeleteAsync(int id);
        
        // Méthodes de recherche
        Task<IEnumerable<Reservation>> GetByUtilisateurAsync(int idUtilisateur);
        Task<IEnumerable<Reservation>> GetByClientAsync(int idClient);
        Task<IEnumerable<Reservation>> GetByVoyageAsync(int idVoyage);
        Task<IEnumerable<Reservation>> GetByStatutReservationAsync(string statutReservation);
        Task<IEnumerable<Reservation>> GetByDateAsync(DateTime date);
        Task<IEnumerable<Reservation>> GetByDateRangeAsync(DateTime dateDebut, DateTime dateFin);
        Task<IEnumerable<Reservation>> GetByUtilisateurAndClientAsync(int idUtilisateur, int idClient);
        Task<IEnumerable<Reservation>> GetByVoyageAndStatutAsync(int idVoyage, string statutReservation);
        
        // Méthodes de filtrage
        Task<IEnumerable<Reservation>> GetByStatutAsync(bool statut);
        Task<IEnumerable<Reservation>> GetActiveAsync();
        Task<IEnumerable<Reservation>> GetInactiveAsync();
        
        // Méthodes d'existence
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByVoyageAndClientAsync(int idVoyage, int idClient);
        Task<bool> ExistsByVoyageAndClientAndDateAsync(int idVoyage, int idClient, DateTime date);
        
        // Pagination
        Task<PagedResult<Reservation>> GetPagedAsync(PagedRequest request);
        Task<PagedResult<Reservation>> GetByUtilisateurPagedAsync(int idUtilisateur, PagedRequest request);
        Task<PagedResult<Reservation>> GetByClientPagedAsync(int idClient, PagedRequest request);
        Task<PagedResult<Reservation>> GetByVoyagePagedAsync(int idVoyage, PagedRequest request);
        Task<PagedResult<Reservation>> GetByStatutReservationPagedAsync(string statutReservation, PagedRequest request);
        
        // Compteurs
        Task<int> CountAsync();
        Task<int> CountByUtilisateurAsync(int idUtilisateur);
        Task<int> CountByClientAsync(int idClient);
        Task<int> CountByVoyageAsync(int idVoyage);
        Task<int> CountByStatutReservationAsync(string statutReservation);
        Task<int> CountByDateAsync(DateTime date);
        Task<int> CountByStatutAsync(bool statut);
        Task<int> CountActiveAsync();
        Task<int> CountInactiveAsync();

        /// <summary>
        /// Passagers de la réservation, triés par <see cref="ReservationPassenger.IdReservationPassenger"/>.
        /// </summary>
        Task<IReadOnlyList<ReservationPassenger>> GetPassagersByReservationAsync(int idReservation);

        /// <summary>
        /// Réservations d'une société, avec passagers et navigations utiles pour la réponse.
        /// Retourne <see langword="null"/> si la société n'existe pas.
        /// </summary>
        Task<IReadOnlyList<Reservation>?> GetBySocieteWithPassagersAsync(int idSociete);

        /// <summary>
        /// Réservations pour un voyage donné dans une société, avec passagers et navigations utiles pour la réponse.
        /// Retourne <see langword="null"/> si le voyage n'existe pas ou n'appartient pas à la société.
        /// </summary>
        Task<IReadOnlyList<Reservation>?> GetBySocieteAndVoyageWithPassagersAsync(int idSociete, int idVoyage);
    }
}
