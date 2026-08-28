using CongoTravel.Models.DTOs.Hotel;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelPlanificationService
    {
        Task<IReadOnlyList<HotelPlanificationListItemDto>> ListAsync(
            int idSociete,
            int? idHotel = null,
            CancellationToken cancellationToken = default);

        Task<HotelPlanificationResponseDto?> GetByIdAsync(
            int id,
            int? idSociete = null,
            CancellationToken cancellationToken = default);

        Task<HotelPlanificationResponseDto> CreateAsync(
            HotelCreatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<HotelPlanificationResponseDto?> UpdateAsync(
            HotelUpdatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<bool> ToggleStatutAsync(int id, int idSociete, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, int idSociete, CancellationToken cancellationToken = default);
    }
}
