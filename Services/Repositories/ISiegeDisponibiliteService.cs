using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    public interface ISiegeDisponibiliteService
    {
        /// <summary>
        /// Sièges indisponibles pour un voyage : allocations CONFIRME + holds non expirés.
        /// </summary>
        Task<HashSet<int>> GetIndisponibleSiegeIdsAsync(int idVoyage, CancellationToken cancellationToken = default);

        /// <summary>Allocations CONFIRME + holds actifs, groupés par voyage.</summary>
        Task<IReadOnlyDictionary<int, HashSet<int>>> GetIndisponibleSiegeIdsParVoyagesAsync(
            IReadOnlyList<int> idVoyages,
            CancellationToken cancellationToken = default);

        Task<int> PurgeExpiredHoldsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Crée des holds pour les catégories demandées (ordre des passagers). Retourne les IdSiege retenus.
        /// </summary>
        Task<IReadOnlyList<int>> CreateHoldsForCategoriesAsync(
            int idVoyage,
            Guid idCommandeEnAttente,
            IReadOnlyList<int> idCategorieSiegeOrdered,
            int holdMinutes,
            CancellationToken cancellationToken = default);

        Task ReleaseHoldsForCommandeAsync(Guid idCommandeEnAttente, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convertit les holds d'une commande en allocations CONFIRME pour les passagers (ordre d'insertion des holds).
        /// </summary>
        Task<IReadOnlyList<VoyageSeatAllocation>> ConfirmHoldsAsAllocationsAsync(
            Guid idCommandeEnAttente,
            int idVoyage,
            IReadOnlyList<int> idReservationPassengersOrdered,
            CancellationToken cancellationToken = default);
    }
}
