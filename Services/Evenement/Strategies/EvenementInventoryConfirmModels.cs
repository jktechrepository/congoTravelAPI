using CongoTravel.Models.Evenement;

namespace CongoTravel.Services.Evenement.Strategies
{
    public class EvenementInventoryConfirmRequest
    {
        public EvenementReservation Reservation { get; set; } = null!;

        public EvenementSession Session { get; set; } = null!;
    }
}
