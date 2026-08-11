namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueTicketResponseDto
    {
        public int IdSiteTouristiqueTicket { get; set; }

        public int IdSiteTouristiqueReservationLine { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime IssuedAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }
    }
}
