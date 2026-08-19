namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantTicketCheckResponseDto
    {
        public int? IdRestaurantTicket { get; set; }

        public string? TicketCode { get; set; }

        public string? Status { get; set; }

        /// <summary>
        /// Synthèse : NonReconnu | DejaUtilise | Invalide | CreneauInactif | HorsFenetre | Valide.
        /// </summary>
        public string Statut { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool EntreeAutorisee { get; set; }

        public int? IdRestaurantReservation { get; set; }

        public string? ReferenceReservation { get; set; }

        public int? IdRestaurantCreneau { get; set; }

        public string? LogoSociete { get; set; }

        public DateOnly? DateService { get; set; }

        public DateTime? StartAtUtc { get; set; }

        public string? CustomerRef { get; set; }
    }
}
