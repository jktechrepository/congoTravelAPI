namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Réponse contrôle entrée (inspirée de <see cref="CongoTravel.Models.DTOs.BilletCheckResponseDto"/> transport).</summary>
    public class EvenementTicketCheckResponseDto
    {
        public int? IdEvenementTicket { get; set; }

        public string? TicketCode { get; set; }

        /// <summary>ISSUED, USED, VOID ou null si ticket inconnu.</summary>
        public string? Status { get; set; }

        /// <summary>
        /// Synthèse : NonReconnu | DejaUtilise | Invalide | SessionInactive | HorsFenetre | Valide.
        /// </summary>
        public string Statut { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        /// <summary>Indique si l'entrée peut être validée (scan final).</summary>
        public bool EntreeAutorisee { get; set; }

        public int? IdEvenementReservation { get; set; }

        public string? ReferenceReservation { get; set; }

        public int? IdEvenementSession { get; set; }

        public string? CodeSession { get; set; }

        public string? LibelleSession { get; set; }

        public string? LogoOrganisateur { get; set; }

        public DateTime? StartAtUtc { get; set; }

        public string? CustomerRef { get; set; }
    }
}
