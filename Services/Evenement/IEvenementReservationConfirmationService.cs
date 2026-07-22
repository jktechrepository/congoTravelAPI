using CongoTravel.Models.Evenement;

namespace CongoTravel.Services.Evenement
{
    /// <summary>
    /// Cœur de confirmation réservation événement (inventaire + tickets).
    /// Réutilisé par paiement CASH et finalisation FlexPay (callback / verify).
    /// </summary>
    public interface IEvenementReservationConfirmationService
    {
        void EnsureHoldConfirmable(EvenementReservation reservation);

        /// <summary>
        /// Confirme le hold, émet les tickets et finalise le paiement (<c>SUCCEEDED</c>).
        /// N'appelle pas <c>SaveChanges</c> — le caller gère la transaction.
        /// </summary>
        Task ConfirmHoldAndEmitTicketsAsync(
            EvenementReservation reservation,
            EvenementPayment payment,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
