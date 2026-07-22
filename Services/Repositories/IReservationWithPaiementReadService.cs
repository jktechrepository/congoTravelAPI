using CongoTravel.Models.DTOs.Reservation;

namespace CongoTravel.Services.Repositories
{
    public interface IReservationWithPaiementReadService
    {
        Task<ReservationWithPaiementResponseDto?> BuildByReservationIdAsync(
            int idReservation,
            string? transactionId = null,
            string? message = null,
            CancellationToken cancellationToken = default);
    }
}
