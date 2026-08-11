using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.DTOs.FlexPay;

namespace CongoTravel.Services.SiteTouristique
{
    /// <summary>Traitement callback FlexPay pour le module site touristique (pipeline autonome).</summary>
    public interface ISiteTouristiqueFlexPayCallbackService
    {
        Task<SiteTouristiqueFlexPayCallbackProcessResultDto> ProcessCallbackAsync(
            FlexPayCallbackDto callback,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Vérifie le statut chez FlexPay et finalise la réservation HOLD si succès (secours callback).
        /// </summary>
        Task<SiteTouristiqueFlexPayVerifierResultDto> VerifyAndFinalizeAsync(
            string orderNumber,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marque le paiement PENDING en FAILED, libère le HOLD si présent, notifie SignalR Failed.
        /// Utilisé par cancel / decline FlexPay.
        /// </summary>
        Task<SiteTouristiqueFlexPayCallbackProcessResultDto> AbandonPendingPaymentAsync(
            string orderNumber,
            string message,
            CancellationToken cancellationToken = default);
    }
}
