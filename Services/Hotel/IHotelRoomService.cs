using CongoTravel.Models.DTOs.Hotel;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelRoomService
    {
        Task<HotelRoomResponseDto> CreateAsync(
            HotelCreateRoomRequestDto request, int idSociete, CancellationToken cancellationToken = default);
        Task<HotelRoomResponseDto?> GetByIdAsync(
            int idHotelRoom, int idSociete, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HotelRoomResponseDto>> ListAsync(
            int idSociete, HotelRoomListFilter? filter = null, CancellationToken cancellationToken = default);
        Task<HotelRoomResponseDto?> UpdateAsync(
            int idHotelRoom, HotelUpdateRoomRequestDto request, int idSociete,
            CancellationToken cancellationToken = default);
        Task DeleteAsync(int idHotelRoom, int idSociete, CancellationToken cancellationToken = default);
    }
}
