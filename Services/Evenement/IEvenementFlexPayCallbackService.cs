using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.DTOs.FlexPay;

namespace CongoTravel.Services.Evenement
{
    /// <summary>Traitement callback FlexPay pour le module événementiel (pipeline autonome).</summary>
    public interface IEvenementFlexPayCallbackService
    {
        Task<EvenementFlexPayCallbackProcessResultDto> ProcessCallbackAsync(
            FlexPayCallbackDto callback,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Vérifie le statut chez FlexPay et finalise la réservation HOLD si succès (secours callback).
        /// </summary>
        Task<EvenementFlexPayVerifierResultDto> VerifyAndFinalizeAsync(
            string orderNumber,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marque le paiement PENDING en FAILED, libère le HOLD si présent, notifie SignalR Failed.
        /// Utilisé par cancel / decline FlexPay.
        /// </summary>
        Task<EvenementFlexPayCallbackProcessResultDto> AbandonPendingPaymentAsync(
            string orderNumber,
            string message,
            CancellationToken cancellationToken = default);
    }
}
