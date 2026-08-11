using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    public class SiteTouristiqueInventoryCancelRequest
    {
        public SiteTouristiqueReservation Reservation { get; set; } = null!;

        public SiteTouristiqueJournee Journee { get; set; } = null!;

        /// <summary><c>true</c> si la réservation était confirmée (libération <c>QuantiteVendue</c>).</summary>
        public bool FromConfirmedSale { get; set; }
    }
}
