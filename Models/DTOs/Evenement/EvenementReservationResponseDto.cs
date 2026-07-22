namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Détail réservation événement (hold, confirmé ou annulé).</summary>
    public class EvenementReservationResponseDto
    {
        public int IdEvenementReservation { get; set; }

        public int IdSociete { get; set; }

        public int IdEvenementSession { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public string? CustomerRef { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? ExpiresAtUtc { get; set; }

        public decimal MontantSousTotal { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }

        public List<EvenementReservationLineResponseDto> Lines { get; set; } = new();

        public List<EvenementTicketResponseDto> Tickets { get; set; } = new();

        public List<EvenementPaymentResponseDto> Payments { get; set; } = new();
    }
}
