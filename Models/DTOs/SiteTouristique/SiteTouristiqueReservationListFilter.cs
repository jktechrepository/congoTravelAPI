using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>Filtres optionnels pour la liste des réservations site touristique.</summary>
    public class SiteTouristiqueReservationListFilter
    {
        public SiteTouristiqueReservationStatus? Status { get; set; }

        public int? IdSiteTouristiqueJournee { get; set; }

        public string? CustomerRef { get; set; }
    }
}
