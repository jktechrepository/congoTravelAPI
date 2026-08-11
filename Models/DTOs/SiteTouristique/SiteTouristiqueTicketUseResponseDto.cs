namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>Réponse <c>POST /api/sites-touristiques/tickets/{ticketCode}/use</c>.</summary>
    public class SiteTouristiqueTicketUseResponseDto
    {
        public SiteTouristiqueTicketResponseDto Ticket { get; set; } = new();

        /// <summary><c>true</c> si le ticket était déjà <c>USED</c> (appel idempotent).</summary>
        public bool AlreadyUsed { get; set; }
    }
}
