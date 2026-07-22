using CongoTravel.Models.DTOs.Reservation;

namespace CongoTravel.Services.Repositories
{
    public interface IFlexPayReservationService
    {
        Task<ReservationWithPaiementResponseDto> InitiateAsync(InitiateFlexPayReservationDto dto, CancellationToken cancellationToken = default);
    }
}
