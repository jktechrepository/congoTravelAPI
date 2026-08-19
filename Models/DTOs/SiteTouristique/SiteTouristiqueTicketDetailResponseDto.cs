namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueTicketDetailResponseDto
    {
        public int IdSiteTouristiqueTicket { get; set; }
        public int IdSiteTouristiqueReservationLine { get; set; }
        public string TicketCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime IssuedAtUtc { get; set; }
        public DateTime? UsedAtUtc { get; set; }
        public int IdSiteTouristiqueReservation { get; set; }
        public string ReferenceReservation { get; set; } = string.Empty;
        public string? CustomerRef { get; set; }
        public string ReservationStatus { get; set; } = string.Empty;
        public int IdSiteTouristiqueJournee { get; set; }
        public int IdSiteTouristique { get; set; }
        public string? CodeLieu { get; set; }
        public string? NomLieu { get; set; }
        public DateOnly DateVisite { get; set; }
        public string? LogoSociete { get; set; }
    }
}
