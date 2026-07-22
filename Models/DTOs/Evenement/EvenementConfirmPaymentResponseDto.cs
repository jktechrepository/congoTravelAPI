namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Réponse après confirmation paiement (idempotent si déjà confirmé).</summary>
    public class EvenementConfirmPaymentResponseDto
    {
        public EvenementReservationResponseDto Reservation { get; set; } = new();

        public EvenementPaymentResponseDto Payment { get; set; } = new();

        /// <summary><c>true</c> si la réservation était déjà confirmée avant cet appel.</summary>
        public bool AlreadyConfirmed { get; set; }
    }
}
