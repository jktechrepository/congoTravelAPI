namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueTicketCheckResponseDto
    {
        public int? IdSiteTouristiqueTicket { get; set; }
        public string? TicketCode { get; set; }
        public string? Status { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool EntreeAutorisee { get; set; }
        public int? IdSiteTouristiqueReservation { get; set; }
        public string? ReferenceReservation { get; set; }
        public int? IdSiteTouristiqueJournee { get; set; }
        public string? CodeLieu { get; set; }
        public string? NomLieu { get; set; }
        public DateOnly? DateVisite { get; set; }
        public string? CustomerRef { get; set; }
        public string? LogoSociete { get; set; }
    }
}
