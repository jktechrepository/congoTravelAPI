using CongoTravel.Models.DTOs.Hotel;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelExtraService
    {
        Task<HotelExtraResponseDto> CreateAsync(
            HotelCreateExtraRequestDto request, int idSociete, CancellationToken cancellationToken = default);
        Task<HotelExtraResponseDto?> GetByIdAsync(
            int idHotelExtra, int idSociete, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HotelExtraResponseDto>> ListAsync(
            int idSociete, HotelExtraListFilter? filter = null, CancellationToken cancellationToken = default);
        Task<HotelExtraResponseDto?> UpdateAsync(
            int idHotelExtra, HotelUpdateExtraRequestDto request, int idSociete,
            CancellationToken cancellationToken = default);
        Task DeleteAsync(int idHotelExtra, int idSociete, CancellationToken cancellationToken = default);
    }
}
