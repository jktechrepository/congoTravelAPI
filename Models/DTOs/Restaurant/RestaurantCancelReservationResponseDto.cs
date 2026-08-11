namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantCancelReservationResponseDto
    {
        public RestaurantReservationResponseDto Reservation { get; set; } = new();

        public bool AlreadyCancelled { get; set; }
    }
}
