using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using RestaurantEntity = CongoTravel.Models.Restaurant.Restaurant;

namespace CongoTravel.Helpers.Restaurant
{
    public static class RestaurantEtablissementMapper
    {
        public static RestaurantEtablissementListItemDto ToListItemDto(
            RestaurantEntity restaurant,
            bool includePhotoBase64 = false) =>
            new()
            {
                IdRestaurant = restaurant.IdRestaurant,
                IdSociete = restaurant.IdSociete,
                NomSociete = restaurant.Societe?.Nom,
                IdSite = restaurant.IdSite,
                NomSite = restaurant.Site?.NomSite,
                CodeRestaurant = restaurant.CodeRestaurant,
                Nom = restaurant.Nom,
                Description = restaurant.Description,
                Adresse = restaurant.Adresse,
                AcomptePourcentDefaut = restaurant.AcomptePourcentDefaut,
                Status = restaurant.Status.ToString(),
                DateCreation = restaurant.DateCreation,
                DateModification = restaurant.DateModification,
                PhotoCouverture = ResolveCoverPhoto(restaurant, includePhotoBase64)
            };

        public static RestaurantEtablissementResponseDto ToResponseDto(
            RestaurantEntity restaurant,
            bool includePhotoBase64 = false)
        {
            var dto = new RestaurantEtablissementResponseDto
            {
                IdRestaurant = restaurant.IdRestaurant,
                IdSociete = restaurant.IdSociete,
                NomSociete = restaurant.Societe?.Nom,
                IdSite = restaurant.IdSite,
                NomSite = restaurant.Site?.NomSite,
                CodeRestaurant = restaurant.CodeRestaurant,
                Nom = restaurant.Nom,
                Description = restaurant.Description,
                Adresse = restaurant.Adresse,
                AcomptePourcentDefaut = restaurant.AcomptePourcentDefaut,
                Status = restaurant.Status.ToString(),
                DateCreation = restaurant.DateCreation,
                DateModification = restaurant.DateModification,
                CreneauxCount = restaurant.Creneaux?.Count ?? 0,
                PhotoCouverture = ResolveCoverPhoto(restaurant, includePhotoBase64)
            };

            if (restaurant.Photos != null && restaurant.Photos.Count > 0)
            {
                dto.Photos = restaurant.Photos
                    .Where(p => p.Statut)
                    .OrderBy(p => p.Ordre)
                    .Select(p => ToPhotoDto(p, includePhotoBase64))
                    .ToList();
            }

            return dto;
        }

        public static RestaurantPhotoDto ToPhotoDto(
            RestaurantPhoto photo,
            bool includePhotoBase64 = false)
        {
            return new RestaurantPhotoDto
            {
                IdRestaurantPhoto = photo.IdRestaurantPhoto,
                IdRestaurant = photo.IdRestaurant,
                PhotoUrl = CongoTravelPhotoUrlBuilder.ForRestaurant(
                    photo.IdRestaurant,
                    photo.IdRestaurantPhoto),
                PhotoBase64 = PhotoContentHelper.EncodeBase64IfRequested(
                    photo.PhotoData,
                    photo.TypeMIME,
                    includePhotoBase64),
                Ordre = photo.Ordre,
                OriginalFileName = photo.OriginalFileName,
                TypeMIME = photo.TypeMIME,
                FileSize = photo.FileSize,
                Statut = photo.Statut,
                DateCreation = photo.DateCreation,
                DateModification = photo.DateModification
            };
        }

        private static RestaurantPhotoDto? ResolveCoverPhoto(
            RestaurantEntity restaurant,
            bool includePhotoBase64 = false)
        {
            var cover = restaurant.Photos?
                .Where(p => p.Statut)
                .OrderBy(p => p.Ordre)
                .FirstOrDefault();

            return cover == null ? null : ToPhotoDto(cover, includePhotoBase64);
        }
    }
}
