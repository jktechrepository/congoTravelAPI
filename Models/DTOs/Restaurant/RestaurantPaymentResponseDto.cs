namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantPaymentResponseDto
    {
        public int IdRestaurantPayment { get; set; }

        public int? IdSite { get; set; }

        public string ReferencePaiement { get; set; } = string.Empty;

        public string Provider { get; set; } = string.Empty;

        public string? ProviderTxRef { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal Montant { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public decimal MontantTarif { get; set; }

        public string CodeDeviseTarif { get; set; } = "CDF";

        public decimal TauxVersDevisePaiement { get; set; }

        public DateTime DateCreation { get; set; }
    }
}
