using CongoTravel.Models.DTOs.Hotel;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelAllotmentService
    {
        Task<HotelAllotmentResponseDto> CreateDraftAsync(
            HotelCreateAllotmentRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<HotelAllotmentBatchResultDto> CreateDraftBatchAsync(
            HotelCreateAllotmentBatchRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<HotelAllotmentResponseDto?> GetByIdAsync(
            int idHotelNightAllotment,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<HotelAllotmentResponseDto?> GetPublishedByIdAsync(
            int idHotelNightAllotment,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<HotelAllotmentResponseDto>> ListAsync(
            int idSociete,
            HotelAllotmentListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<HotelAllotmentResponseDto>> ListPublishedGlobalAsync(
            HotelAllotmentListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<HotelAllotmentResponseDto?> UpdateAsync(
            int idHotelNightAllotment,
            HotelUpdateAllotmentRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<HotelAllotmentResponseDto> PublishAsync(
            int idHotelNightAllotment,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
