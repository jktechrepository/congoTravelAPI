namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantTicketDetailResponseDto
    {
        public int IdRestaurantTicket { get; set; }

        public int IdRestaurantReservationLine { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime IssuedAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }

        public int IdRestaurantReservation { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public string? CustomerRef { get; set; }

        public string ReservationStatus { get; set; } = string.Empty;

        public int IdRestaurantCreneau { get; set; }

        public DateOnly DateService { get; set; }

        public DateTime StartAtUtc { get; set; }

        public DateTime EndAtUtc { get; set; }
    }
}
