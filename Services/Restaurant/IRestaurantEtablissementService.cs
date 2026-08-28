using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant
{
    public interface IRestaurantEtablissementService
    {
        Task<RestaurantEtablissementResponseDto> CreateDraftAsync(
            RestaurantCreateEtablissementRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<RestaurantEtablissementResponseDto?> GetByIdAsync(
            int idRestaurant,
            int? idSociete = null,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<RestaurantEtablissementResponseDto?> GetPublishedByIdAsync(
            int idRestaurant,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<IReadOnlyList<RestaurantEtablissementListItemDto>> ListAsync(
            int idSociete,
            RestaurantEtablissementListFilter? filter = null,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<IReadOnlyList<RestaurantEtablissementListItemDto>> ListPublishedGlobalAsync(
            RestaurantEtablissementListFilter? filter = null,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<RestaurantEtablissementResponseDto?> UpdateAsync(
            int idRestaurant,
            RestaurantUpdateEtablissementRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<RestaurantEtablissementResponseDto> PublishAsync(
            int idRestaurant,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
