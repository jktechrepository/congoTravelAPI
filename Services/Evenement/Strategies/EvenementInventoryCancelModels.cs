using CongoTravel.Models.Evenement;

namespace CongoTravel.Services.Evenement.Strategies
{
    public class EvenementInventoryCancelRequest
    {
        public EvenementReservation Reservation { get; set; } = null!;

        public EvenementSession Session { get; set; } = null!;

        /// <summary><c>true</c> si la réservation était confirmée (libération <c>QuantiteVendue</c>).</summary>
        public bool FromConfirmedSale { get; set; }
    }
}
