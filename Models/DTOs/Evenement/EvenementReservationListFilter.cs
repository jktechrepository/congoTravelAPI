using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Filtres optionnels pour la liste des réservations événement.</summary>
    public class EvenementReservationListFilter
    {
        public EvenementReservationStatus? Status { get; set; }

        public int? IdEvenementSession { get; set; }

        public string? CustomerRef { get; set; }

        public int? IdUtilisateur { get; set; }

        public int? IdClient { get; set; }
    }
}
