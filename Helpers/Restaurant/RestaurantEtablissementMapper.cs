using CongoTravel.Models.DTOs.Restaurant;
using RestaurantEntity = CongoTravel.Models.Restaurant.Restaurant;

namespace CongoTravel.Helpers.Restaurant
{
    public static class RestaurantEtablissementMapper
    {
        public static RestaurantEtablissementListItemDto ToListItemDto(RestaurantEntity restaurant) =>
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
                DateModification = restaurant.DateModification
            };

        public static RestaurantEtablissementResponseDto ToResponseDto(RestaurantEntity restaurant) =>
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
                CreneauxCount = restaurant.Creneaux?.Count ?? 0
            };
    }
}
