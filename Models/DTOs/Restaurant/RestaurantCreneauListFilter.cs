using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantCreneauListFilter
    {
        public RestaurantStatus? Status { get; set; }
        public RestaurantInventoryMode? InventoryMode { get; set; }
        public int? IdRestaurant { get; set; }
        public int? IdSociete { get; set; }
        public DateOnly? DateService { get; set; }
    }
}
