namespace CongoTravel.Models.DTOs.Evenement
{
    public class EvenementTicketResponseDto
    {
        public int IdEvenementTicket { get; set; }

        public int IdEvenementReservationLine { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? LogoOrganisateur { get; set; }

        public DateTime IssuedAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }
    }
}
