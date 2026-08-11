using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantReservationListFilter
    {
        public RestaurantReservationStatus? Status { get; set; }

        public int? IdRestaurant { get; set; }

        public int? IdRestaurantCreneau { get; set; }

        public string? CustomerRef { get; set; }
    }
}
