using CongoTravel.Models.DTOs.Hotel;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelRoomTypeService
    {
        Task<HotelRoomTypeResponseDto> CreateDraftAsync(HotelCreateRoomTypeRequestDto request, int idSociete, CancellationToken cancellationToken = default);
        Task<HotelRoomTypeResponseDto?> GetByIdAsync(int idHotelRoomType, int idSociete, CancellationToken cancellationToken = default);
        Task<HotelRoomTypeResponseDto?> GetPublishedByIdAsync(int idHotelRoomType, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HotelRoomTypeResponseDto>> ListAsync(int idSociete, HotelRoomTypeListFilter? filter = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HotelRoomTypeResponseDto>> ListPublishedGlobalAsync(HotelRoomTypeListFilter? filter = null, CancellationToken cancellationToken = default);
        Task<HotelRoomTypeResponseDto?> UpdateAsync(int idHotelRoomType, HotelUpdateRoomTypeRequestDto request, int idSociete, CancellationToken cancellationToken = default);
        Task<HotelRoomTypeResponseDto> PublishAsync(int idHotelRoomType, int idSociete, CancellationToken cancellationToken = default);
    }
}
