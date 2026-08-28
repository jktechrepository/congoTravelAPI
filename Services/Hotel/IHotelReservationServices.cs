using CongoTravel.Data;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelHoldService
    {
        Task<HotelHoldResponseDto> CreateHoldAsync(int idHotel, int idSociete,
            HotelHoldRequestDto request, CancellationToken cancellationToken = default);
    }
    public interface IHotelReservationConfirmationService
    {
        Task ConfirmHoldAsync(HotelReservation reservation, HotelPayment payment,
            CancellationToken cancellationToken = default);
    }
    public interface IHotelPaymentService
    {
        Task<HotelConfirmPaymentResponseDto> ConfirmPaymentAsync(int idHotelReservation,
            int idSociete, HotelConfirmPaymentRequestDto request,
            CancellationToken cancellationToken = default);
    }
    public interface IHotelReservationWithPaiementService
    {
        Task<HotelReservationWithPaiementResponseDto> CreateCashAsync(
            HotelReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default);
        Task<HotelReservationWithPaiementResponseDto> InitiateElectronicAsync(
            HotelReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default);
    }
    public interface IHotelReservationService
    {
        Task<HotelReservationResponseDto?> GetByIdAsync(int id, int idSociete,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HotelReservationListItemDto>> ListAsync(int idSociete,
            HotelReservationListFilter? filter = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HotelReservationListItemDto>> ListByClientAsync(int idClient,
            HotelReservationListFilter? filter = null, CancellationToken cancellationToken = default);
        Task<HotelCancelReservationResponseDto> CancelAsync(int id, int idSociete,
            CancellationToken cancellationToken = default);
        Task<HotelReservationResponseDto> AssignRoomsAsync(
            int idHotelReservation, int idSociete, HotelAssignRoomsRequestDto request,
            CancellationToken cancellationToken = default);
        Task<HotelReservationResponseDto> CheckInAsync(
            int idHotelReservation, int idSociete, CancellationToken cancellationToken = default);
        Task<HotelReservationResponseDto> CheckOutAsync(
            int idHotelReservation, int idSociete, CancellationToken cancellationToken = default);
        Task<HotelReservationResponseDto> SetExtrasAsync(
            int idHotelReservation, int idSociete, HotelSetReservationExtrasRequestDto request,
            CancellationToken cancellationToken = default);
    }
    public interface IHotelHoldExpirationRunner
    {
        Task ExpireHoldsAsync(CongoTravelDbContext context,
            CancellationToken cancellationToken = default);
    }
}
