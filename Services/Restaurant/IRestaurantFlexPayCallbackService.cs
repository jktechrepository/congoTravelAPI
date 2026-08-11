using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.DTOs.FlexPay;

namespace CongoTravel.Services.Restaurant
{
    /// <summary>Traitement callback FlexPay pour le module restaurant (pipeline autonome).</summary>
    public interface IRestaurantFlexPayCallbackService
    {
        Task<RestaurantFlexPayCallbackProcessResultDto> ProcessCallbackAsync(
            FlexPayCallbackDto callback,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Vérifie le statut chez FlexPay et finalise la réservation HOLD si succès (secours callback).
        /// </summary>
        Task<RestaurantFlexPayVerifierResultDto> VerifyAndFinalizeAsync(
            string orderNumber,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marque le paiement PENDING en FAILED, libère le HOLD si présent, notifie SignalR Failed.
        /// Utilisé par cancel / decline FlexPay.
        /// </summary>
        Task<RestaurantFlexPayCallbackProcessResultDto> AbandonPendingPaymentAsync(
            string orderNumber,
            string message,
            CancellationToken cancellationToken = default);
    }
}
