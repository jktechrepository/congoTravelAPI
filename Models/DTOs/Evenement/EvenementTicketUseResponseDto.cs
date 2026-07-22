namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Réponse <c>POST /api/events/tickets/{ticketCode}/use</c>.</summary>
    public class EvenementTicketUseResponseDto
    {
        public EvenementTicketResponseDto Ticket { get; set; } = new();

        /// <summary><c>true</c> si le ticket était déjà <c>USED</c> (appel idempotent).</summary>
        public bool AlreadyUsed { get; set; }
    }
}
