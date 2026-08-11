using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    public class SiteTouristiqueInventoryConfirmRequest
    {
        public SiteTouristiqueReservation Reservation { get; set; } = null!;

        public SiteTouristiqueJournee Journee { get; set; } = null!;
    }
}
