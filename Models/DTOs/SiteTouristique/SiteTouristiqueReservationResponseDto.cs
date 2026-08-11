namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>Détail réservation site touristique (hold, confirmé ou annulé).</summary>
    public class SiteTouristiqueReservationResponseDto
    {
        public int IdSiteTouristiqueReservation { get; set; }

        public int IdSociete { get; set; }

        public int IdSiteTouristiqueJournee { get; set; }

        public int? IdSite { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public string? CustomerRef { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? ExpiresAtUtc { get; set; }

        public decimal MontantSousTotal { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }

        public List<SiteTouristiqueReservationLineResponseDto> Lines { get; set; } = new();

        public List<SiteTouristiqueTicketResponseDto> Tickets { get; set; } = new();

        public List<SiteTouristiquePaymentResponseDto> Payments { get; set; } = new();
    }
}
