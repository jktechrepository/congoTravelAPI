namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>Ticket site touristique pour les listes (avec contexte réservation / journee).</summary>
    public class SiteTouristiqueTicketListItemDto
    {
        public int IdSiteTouristiqueTicket { get; set; }

        public int IdSiteTouristiqueReservationLine { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime IssuedAtUtc { get; set; }

        public DateTime? UsedAtUtc { get; set; }

        public int IdSiteTouristiqueReservation { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public int IdSiteTouristiqueJournee { get; set; }

        public string? LogoSociete { get; set; }
    }
}
