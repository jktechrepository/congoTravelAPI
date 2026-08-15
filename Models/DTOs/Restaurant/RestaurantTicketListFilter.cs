using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantTicketListFilter
    {
        public RestaurantTicketStatus? Status { get; set; }

        public int? IdRestaurantReservation { get; set; }

        public int? IdRestaurantCreneau { get; set; }
    }
}
