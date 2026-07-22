namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Ticket événement pour les listes (avec contexte réservation / session).</summary>
    public class EvenementTicketListItemDto
    {
        public int IdEvenementTicket { get; set; }

        public int IdEvenementReservationLine { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime IssuedAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }

        public int IdEvenementReservation { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public int IdEvenementSession { get; set; }
    }
}
