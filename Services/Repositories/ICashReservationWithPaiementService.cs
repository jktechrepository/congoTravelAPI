using CongoTravel.Models.DTOs.Reservation;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Création réservation + paiement guichet (CASH uniquement) — chemin isolé de FlexPay.
    /// </summary>
    public interface ICashReservationWithPaiementService
    {
        Task<ReservationWithPaiementResponseDto> CreateAsync(CreateReservationWithPaiementDto dto);
    }
}
