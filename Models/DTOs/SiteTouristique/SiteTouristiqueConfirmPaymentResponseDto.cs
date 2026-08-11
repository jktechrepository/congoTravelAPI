namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>Réponse après confirmation paiement (idempotent si déjà confirmé).</summary>
    public class SiteTouristiqueConfirmPaymentResponseDto
    {
        public SiteTouristiqueReservationResponseDto Reservation { get; set; } = new();

        public SiteTouristiquePaymentResponseDto Payment { get; set; } = new();

        /// <summary><c>true</c> si la réservation était déjà confirmée avant cet appel.</summary>
        public bool AlreadyConfirmed { get; set; }
    }
}
