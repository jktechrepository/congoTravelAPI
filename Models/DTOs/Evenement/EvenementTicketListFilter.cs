using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Filtres optionnels pour la liste des tickets événement.</summary>
    public class EvenementTicketListFilter
    {
        public EvenementTicketStatus? Status { get; set; }

        public int? IdEvenementReservation { get; set; }

        public int? IdEvenementSession { get; set; }
    }
}
