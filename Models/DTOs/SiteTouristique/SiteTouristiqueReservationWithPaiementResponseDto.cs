namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>
    /// Réponse unifiée des endpoints hold+paiement (CASH confirmé ou FlexPay en attente).
    /// </summary>
    public class SiteTouristiqueReservationWithPaiementResponseDto
    {
        public SiteTouristiqueReservationResponseDto Reservation { get; set; } = new();

        public SiteTouristiquePaymentResponseDto? Payment { get; set; }

        public List<SiteTouristiqueTicketResponseDto> Tickets { get; set; } = new();

        /// <summary><c>Succes</c> (CASH) ou <c>EnAttente</c> (FlexPay initié).</summary>
        public string TransactionStatut { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        // --- Meta FlexPay (null / vides pour CASH) ---

        public string? OrderNumber { get; set; }

        public string? PaymentUrl { get; set; }

        public DateTime? ReservationExpiresAtUtc { get; set; }

        public decimal? MontantFlexPay { get; set; }

        public string? CodeDevisePaiement { get; set; }

        public decimal? MontantTarif { get; set; }

        public string? CodeDeviseTarif { get; set; }

        public decimal? TauxApplique { get; set; }

        public bool? FlexPayAccepted { get; set; }

        public bool AlreadyConfirmed { get; set; }

        public bool AlreadyInitiated { get; set; }
    }
}
