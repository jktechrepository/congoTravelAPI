namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>Résultat traitement callback / verify FlexPay site touristique.</summary>
    public class SiteTouristiqueFlexPayCallbackProcessResultDto
    {
        public bool Success { get; set; }

        public bool AlreadyProcessed { get; set; }

        public bool PaymentPending { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? IdSiteTouristiqueReservation { get; set; }

        public int? IdSiteTouristiquePayment { get; set; }
    }
}
