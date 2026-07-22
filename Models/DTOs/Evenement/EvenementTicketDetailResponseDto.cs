namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Détail ticket événement avec contexte réservation et session.</summary>
    public class EvenementTicketDetailResponseDto
    {
        public int IdEvenementTicket { get; set; }

        public int IdEvenementReservationLine { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime IssuedAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }

        public int IdEvenementReservation { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public string? CustomerRef { get; set; }

        public string ReservationStatus { get; set; } = string.Empty;

        public int IdEvenementSession { get; set; }

        public string CodeSession { get; set; } = string.Empty;

        public string LibelleSession { get; set; } = string.Empty;

        public DateTime StartAtUtc { get; set; }

        public DateTime? EndAtUtc { get; set; }
    }
}
