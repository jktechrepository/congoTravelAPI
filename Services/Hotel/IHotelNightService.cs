using CongoTravel.Models.DTOs.Hotel;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelNightService
    {
        Task<HotelNightResponseDto> CreateDraftAsync(
            HotelCreateNightRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<HotelNightBatchResultDto> CreateDraftBatchAsync(
            HotelCreateNightBatchRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<HotelNightResponseDto?> GetByIdAsync(
            int idHotelNight,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<HotelNightResponseDto?> GetPublishedByIdAsync(
            int idHotelNight,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<HotelNightResponseDto>> ListAsync(
            int idSociete,
            HotelNightListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<HotelNightResponseDto>> ListPublishedGlobalAsync(
            HotelNightListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<HotelNightResponseDto?> UpdateAsync(
            int idHotelNight,
            HotelUpdateNightRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<HotelNightResponseDto> PublishAsync(
            int idHotelNight,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
