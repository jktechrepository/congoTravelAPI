using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using Microsoft.AspNetCore.Http;

namespace CongoTravel.Services.Restaurant
{
    public interface IRestaurantPhotoService
    {
        Task<IReadOnlyList<RestaurantPhoto>> GetByRestaurantIdAsync(
            int idRestaurant,
            int idSociete,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<PhotoContentPayload?> GetContentAsync(
            int idRestaurant,
            int idSociete,
            int idRestaurantPhoto,
            CancellationToken cancellationToken = default);

        Task<RestaurantPhoto> AddPhotoAsync(
            int idRestaurant,
            int idSociete,
            AddRestaurantPhotoDto dto,
            CancellationToken cancellationToken = default);

        Task<RestaurantPhoto> AddPhotoFromFileAsync(
            int idRestaurant,
            int idSociete,
            IFormFile file,
            int? ordre = null,
            string? fileName = null,
            CancellationToken cancellationToken = default);

        /// <summary>Ajoute 1 à 3 photos à la création de l'établissement (liste vide ou null = rien).</summary>
        Task AddPhotosOnCreateAsync(
            int idRestaurant,
            int idSociete,
            IReadOnlyList<AddRestaurantPhotoDto>? photos,
            CancellationToken cancellationToken = default);

        /// <summary>Remplacement complet via fichiers multipart (0–3). Liste vide = vider la galerie.</summary>
        Task<IReadOnlyList<RestaurantPhoto>> ReplaceAllFromFilesAsync(
            int idRestaurant,
            int idSociete,
            IReadOnlyList<IFormFile> files,
            IReadOnlyList<int>? ordres = null,
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
