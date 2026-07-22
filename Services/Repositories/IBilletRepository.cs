using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    public interface IBilletRepository
    {
        // CRUD de base
        Task<IEnumerable<Billet>> GetAllAsync();
        Task<IEnumerable<Billet>> GetAllBySocieteAsync(int idSociete);
        Task<Billet?> GetByIdAsync(int id);
        Task<Billet> CreateAsync(Billet billet);
        Task<Billet?> UpdateAsync(Billet billet);
        Task<bool> DeleteAsync(int id);
        
        // Méthodes de recherche
        Task<IEnumerable<Billet>> GetByReservationAsync(int idReservation);
        Task<IEnumerable<Billet>> GetByQrCodeAsync(string qrCode);
        Task<IEnumerable<Billet>> GetByDateGenerationAsync(DateTime dateGeneration);
        Task<IEnumerable<Billet>> GetByDateRangeAsync(DateTime dateDebut, DateTime dateFin);
        
        // Méthodes d'existence
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByQrCodeAsync(string qrCode);
        Task<bool> ExistsByReservationAsync(int idReservation);
        Task<bool> ExistsByQrCodeAndReservationAsync(string qrCode, int idReservation);
        
        // Pagination
        Task<PagedResult<Billet>> GetPagedAsync(PagedRequest request);
        Task<PagedResult<Billet>> GetByReservationPagedAsync(int idReservation, PagedRequest request);
        Task<PagedResult<Billet>> GetByDateGenerationPagedAsync(DateTime dateGeneration, PagedRequest request);
        
        // Compteurs
        Task<int> CountAsync();
        Task<int> CountByReservationAsync(int idReservation);
        Task<int> CountByDateGenerationAsync(DateTime dateGeneration);
        Task<int> CountByDateRangeAsync(DateTime dateDebut, DateTime dateFin);

        /// <summary>
        /// Contrôle métier du billet : <see cref="Billet.IsUsed"/>, réservation, voyage, fenêtre d’embarquement.
        /// </summary>
        Task<BilletCheckResponseDto> CheckBilletAsync(int idBillet, int? idVoyageCible = null);

        /// <summary>
        /// Même contrôle que <see cref="CheckBilletAsync"/> mais résolution du billet par <see cref="Billet.QrCode"/> (égalité exacte).
        /// </summary>
        Task<BilletCheckResponseDto> CheckBilletByQrCodeAsync(string qrCode, int? idVoyageCible = null);

        /// <summary>
        /// Marque le billet comme utilisé et crée une ligne d’historique d’embarquement (transaction).
        /// </summary>
        Task<BilletEmbarquementOperationResult> EnregistrerEmbarquementAsync(
            int idSociete,
            int idBillet,
            int idReservationPassenger,
            int? idVoyageCible,
            int? idUtilisateurEnregistrement);

        /// <summary>
        /// Réaffecte un billet vers un autre voyage compatible (même destination) et calcule le différentiel tarifaire.
        /// </summary>
        Task<BilletReaffectationResult> ReaffecterBilletAsync(
            int idSociete,
            int idBillet,
            int idVoyageCible,
            int? idUtilisateurEnregistrement,
            bool confirmerPaiementDifferentiel = false,
            string? methodePaiement = null,
            string? referenceTransaction = null,
            string? commentaire = null);
    }
}
