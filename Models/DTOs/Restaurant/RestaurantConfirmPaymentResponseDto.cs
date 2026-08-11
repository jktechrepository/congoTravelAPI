namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantConfirmPaymentResponseDto
    {
        public RestaurantReservationResponseDto Reservation { get; set; } = new();

        public RestaurantPaymentResponseDto Payment { get; set; } = new();

        public bool AlreadyConfirmed { get; set; }
    }
}
