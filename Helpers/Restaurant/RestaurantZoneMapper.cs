using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;

namespace CongoTravel.Helpers.Restaurant
{
    public static class RestaurantZoneMapper
    {
        public static RestaurantZoneResponseDto ToResponseDto(RestaurantZone zone) =>
            new()
            {
                IdRestaurantZone = zone.IdRestaurantZone,
                IdSociete = zone.IdSociete,
                IdRestaurant = zone.IdRestaurant,
                Code = zone.Code,
                Libelle = zone.Libelle,
                Description = zone.Description,
                Actif = zone.Actif,
                DateCreation = zone.DateCreation,
                DateModification = zone.DateModification
            };
    }
}
