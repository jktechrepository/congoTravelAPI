namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantTicketResponseDto
    {
        public int IdRestaurantTicket { get; set; }

        public int IdRestaurantReservationLine { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime IssuedAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }
    }
}
