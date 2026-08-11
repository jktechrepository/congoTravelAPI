namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>
    /// Résultat discriminant du GET verifier site touristique :
    /// confirmation complète (succès) ou statut seul (pending / échec).
    /// </summary>
    public class SiteTouristiqueFlexPayVerifierResultDto
    {
        public SiteTouristiqueConfirmPaymentResponseDto? ConfirmPayment { get; set; }

        public SiteTouristiqueFlexPayCallbackProcessResultDto? StatusOnly { get; set; }

        public bool IsConfirmSuccess => ConfirmPayment != null;
    }
}
