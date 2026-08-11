namespace CongoTravel.Models.DTOs.Restaurant
{
    /// <summary>Résultat traitement callback / verify FlexPay restaurant.</summary>
    public class RestaurantFlexPayCallbackProcessResultDto
    {
        public bool Success { get; set; }

        public bool AlreadyProcessed { get; set; }

        public bool PaymentPending { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? IdRestaurantReservation { get; set; }

        public int? IdRestaurantPayment { get; set; }
    }
}
