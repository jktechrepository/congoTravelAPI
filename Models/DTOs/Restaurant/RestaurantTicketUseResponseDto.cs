namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantTicketUseResponseDto
    {
        public RestaurantTicketResponseDto Ticket { get; set; } = new();

        public bool AlreadyUsed { get; set; }
    }
}
