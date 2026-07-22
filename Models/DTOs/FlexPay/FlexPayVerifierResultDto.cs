using CongoTravel.Models.DTOs.Reservation;

namespace CongoTravel.Models.DTOs.FlexPay
{
    /// <summary>
    /// Résultat discriminant du GET verifier : DTO unifié (succès) ou statut seul (pending/échec).
    /// </summary>
    public class FlexPayVerifierResultDto
    {
        public ReservationWithPaiementResponseDto? ReservationWithPaiement { get; set; }

        public FlexPayCallbackProcessResultDto? StatusOnly { get; set; }

        public bool IsUnifiedSuccess => ReservationWithPaiement != null;
    }
}
