namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantTicketListItemDto
    {
        public int IdRestaurantTicket { get; set; }

        public int IdRestaurantReservationLine { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime IssuedAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }

        public int IdRestaurantReservation { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public int IdRestaurantCreneau { get; set; }

        public string? LogoSociete { get; set; }
    }
}
