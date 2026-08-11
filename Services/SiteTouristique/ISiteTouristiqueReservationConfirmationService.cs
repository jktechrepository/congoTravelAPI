using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    /// <summary>
    /// Cœur de confirmation réservation site touristique (inventaire + tickets).
    /// Réutilisé par paiement CASH et finalisation FlexPay (callback / verify).
    /// </summary>
    public interface ISiteTouristiqueReservationConfirmationService
    {
        void EnsureHoldConfirmable(SiteTouristiqueReservation reservation);

        /// <summary>
        /// Confirme le hold, émet les tickets et finalise le paiement (<c>SUCCEEDED</c>).
        /// N'appelle pas <c>SaveChanges</c> — le caller gère la transaction.
        /// </summary>
        Task ConfirmHoldAndEmitTicketsAsync(
            SiteTouristiqueReservation reservation,
            SiteTouristiquePayment payment,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
