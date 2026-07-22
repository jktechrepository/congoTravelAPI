using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    public interface IPaiementRepository
    {
        Task<IEnumerable<Paiement>> GetAllAsync();
        Task<Paiement?> GetByIdAsync(int id);
        Task<IEnumerable<Paiement>> GetByReservationAsync(int idReservation);
        Task<IEnumerable<Paiement>> GetByFactureAsync(int idFacture);
        Task<IEnumerable<Paiement>> GetByClientAsync(int idClient);
        Task<IEnumerable<Paiement>> GetBySocieteAsync(int idSociete);
        Task<PagedResult<Paiement>> GetBySocietePagedAsync(int idSociete, PaiementPagedRequest request);
        Task<Paiement> CreateAsync(Paiement paiement);
        Task<Paiement?> UpdateAsync(Paiement paiement);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<PagedResult<Paiement>> GetPagedAsync(PaiementPagedRequest request);
        Task<decimal> GetTotalPaiementsByFactureAsync(int idFacture);
    }
}

