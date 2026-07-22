namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Résultat traitement callback / verify FlexPay événement.</summary>
    public class EvenementFlexPayCallbackProcessResultDto
    {
        public bool Success { get; set; }

        public bool AlreadyProcessed { get; set; }

        public bool PaymentPending { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? IdEvenementReservation { get; set; }

        public int? IdEvenementPayment { get; set; }
    }
}
