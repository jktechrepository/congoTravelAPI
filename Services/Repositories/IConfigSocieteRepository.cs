using CongoTravel.Models;
using CongoTravel.Models.DTOs.ConfigSociete;

namespace CongoTravel.Services.Repositories
{
    public interface IConfigSocieteRepository
    {
        Task<ConfigSociete> GetOrCreateAsync(int idSociete, CancellationToken cancellationToken = default);
        Task<ConfigSociete?> GetBySocieteAsync(int idSociete, CancellationToken cancellationToken = default);
        Task<ConfigSociete> UpdateAsync(int idSociete, ConfigSocieteUpdateDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bloque si <see cref="ConfigSociete.ReservationIsActif"/> est false.
        /// Message : "La reservation n'est pas Activée pour cette société".
        /// </summary>
        Task EnsureReservationsActivesAsync(int idSociete, CancellationToken cancellationToken = default);
    }
}
