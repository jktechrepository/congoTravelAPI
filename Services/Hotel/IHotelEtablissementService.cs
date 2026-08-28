using CongoTravel.Models.DTOs.Hotel;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelEtablissementService
    {
        Task<HotelEtablissementResponseDto> CreateDraftAsync(HotelCreateEtablissementRequestDto request, int idSociete, CancellationToken cancellationToken = default);
        Task<HotelEtablissementResponseDto?> GetByIdAsync(int idHotel, int? idSociete = null, CancellationToken cancellationToken = default, bool includePhotoBase64 = false);
        Task<HotelEtablissementResponseDto?> GetPublishedByIdAsync(int idHotel, CancellationToken cancellationToken = default, bool includePhotoBase64 = false);
        Task<IReadOnlyList<HotelEtablissementListItemDto>> ListAsync(int idSociete, HotelEtablissementListFilter? filter = null, CancellationToken cancellationToken = default, bool includePhotoBase64 = false);
        Task<IReadOnlyList<HotelEtablissementListItemDto>> ListPublishedGlobalAsync(HotelEtablissementListFilter? filter = null, CancellationToken cancellationToken = default, bool includePhotoBase64 = false);
        Task<HotelEtablissementResponseDto?> UpdateAsync(int idHotel, HotelUpdateEtablissementRequestDto request, int idSociete, CancellationToken cancellationToken = default);
        Task<HotelEtablissementResponseDto> PublishAsync(int idHotel, int idSociete, CancellationToken cancellationToken = default);
    }
}
