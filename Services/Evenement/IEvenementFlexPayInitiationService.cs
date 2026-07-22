using CongoTravel.Models.DTOs.Evenement;

namespace CongoTravel.Services.Evenement
{
    /// <summary>
    /// Initiation paiement FlexPay sur une réservation événement <c>HOLD</c> existante (Phase 5).
    /// </summary>
    public interface IEvenementFlexPayInitiationService
    {
        /// <summary>
        /// Crée un <c>EvenementPayment</c> <c>PENDING</c> et appelle l'API FlexPay.
        /// Ne confirme pas la réservation — finalisation via callback ou verify.
        /// </summary>
        Task<EvenementInitiateFlexPayResponseDto> InitiateAsync(
            int idEvenementReservation,
            int idSociete,
            EvenementInitiateFlexPayRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
