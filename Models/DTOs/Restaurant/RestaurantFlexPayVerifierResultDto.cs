namespace CongoTravel.Models.DTOs.Restaurant
{
    /// <summary>
    /// Résultat discriminant du GET verifier restaurant :
    /// confirmation complète (succès) ou statut seul (pending / échec).
    /// </summary>
    public class RestaurantFlexPayVerifierResultDto
    {
        public RestaurantConfirmPaymentResponseDto? ConfirmPayment { get; set; }

        public RestaurantFlexPayCallbackProcessResultDto? StatusOnly { get; set; }

        public bool IsConfirmSuccess => ConfirmPayment != null;
    }
}
