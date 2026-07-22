using CongoTravel.Models;

namespace CongoTravel.Services
{
    /// <summary>
    /// Attribution atomique de sièges libres à des passagers pour un voyage.
    /// </summary>
    public interface IVoyageSeatAllocationService
    {
        /// <summary>
        /// Attribue un siège distinct libre à chaque passager, dans l’ordre fourni,
        /// en respectant la catégorie de siège demandée.
        /// </summary>
        /// <exception cref="InvalidOperationException">Capacité insuffisante ou incohérence voyage/passagers.</exception>
        Task<IReadOnlyList<VoyageSeatAllocation>> AllocateSeatsForPassengersAsync(
            int idVoyage,
            int idReservation,
            IReadOnlyList<(int IdReservationPassenger, int IdCategorieSiege)> allocationRequestsOrdered,
            CancellationToken cancellationToken = default);
    }
}
