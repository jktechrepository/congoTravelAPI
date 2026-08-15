using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;

namespace CongoTravel.Services.Restaurant
{
    public interface IRestaurantPhotoService
    {
        Task<IReadOnlyList<RestaurantPhoto>> GetByRestaurantIdAsync(
            int idRestaurant,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<RestaurantPhoto> AddPhotoAsync(
            int idRestaurant,
            int idSociete,
            AddRestaurantPhotoDto dto,
            CancellationToken cancellationToken = default);

        /// <summary>Ajoute 1 à 3 photos à la création de l'établissement (liste vide ou null = rien).</summary>
        Task AddPhotosOnCreateAsync(
            int idRestaurant,
            int idSociete,
            IReadOnlyList<AddRestaurantPhotoDto>? photos,
            CancellationToken cancellationToken = default);

        Task<RestaurantPhoto?> UpdateOrdreAsync(
            int idRestaurant,
            int idSociete,
            int idRestaurantPhoto,
            int ordre,
            CancellationToken cancellationToken = default);

        Task<bool> DeletePhotoAsync(
            int idRestaurant,
            int idSociete,
            int idRestaurantPhoto,
            CancellationToken cancellationToken = default);
    }
}
