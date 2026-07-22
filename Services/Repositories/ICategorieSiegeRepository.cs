using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    public interface ICategorieSiegeRepository
    {
        Task<IReadOnlyList<CategorieSiege>> GetBySocieteAsync(int idSociete, bool actifsSeulement = false);
        Task<CategorieSiege?> GetByIdAsync(int idCategorieSiege);
        Task<CategorieSiege> CreateAsync(CategorieSiege categorie);
        Task<CategorieSiege?> UpdateAsync(CategorieSiege categorie);
        Task<CategorieSiege?> ToggleStatutAsync(int idCategorieSiege);
        Task<bool> DeleteAsync(int idCategorieSiege);
    }
}
