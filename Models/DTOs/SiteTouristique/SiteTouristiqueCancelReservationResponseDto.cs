namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>Réponse <c>POST /api/sites-touristiques/reservations/{id}/cancel</c>.</summary>
    public class SiteTouristiqueCancelReservationResponseDto
    {
        public SiteTouristiqueReservationResponseDto Reservation { get; set; } = new();

        /// <summary><c>true</c> si la réservation était déjà annulée avant cet appel.</summary>
        public bool AlreadyCancelled { get; set; }

        /// <summary>Nombre de tickets passés en <c>VOID</c> lors de cet appel.</summary>
        public int TicketsVoided { get; set; }
    }
}
