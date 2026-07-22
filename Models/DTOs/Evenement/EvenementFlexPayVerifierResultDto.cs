namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>
    /// Résultat discriminant du GET verifier événement :
    /// confirmation complète (succès) ou statut seul (pending / échec).
    /// </summary>
    public class EvenementFlexPayVerifierResultDto
    {
        public EvenementConfirmPaymentResponseDto? ConfirmPayment { get; set; }

        public EvenementFlexPayCallbackProcessResultDto? StatusOnly { get; set; }

        public bool IsConfirmSuccess => ConfirmPayment != null;
    }
}
