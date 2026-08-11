using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>Filtres optionnels pour la liste des tickets site touristique.</summary>
    public class SiteTouristiqueTicketListFilter
    {
        public SiteTouristiqueTicketStatus? Status { get; set; }

        public int? IdSiteTouristiqueReservation { get; set; }

        public int? IdSiteTouristiqueJournee { get; set; }
    }
}
