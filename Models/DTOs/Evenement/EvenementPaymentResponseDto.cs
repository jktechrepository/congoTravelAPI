namespace CongoTravel.Models.DTOs.Evenement
{
    public class EvenementPaymentResponseDto
    {
        public int IdEvenementPayment { get; set; }

        public int? IdSite { get; set; }

        public string ReferencePaiement { get; set; } = string.Empty;

        public string Provider { get; set; } = string.Empty;

        public string? ProviderTxRef { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal Montant { get; set; }

        /// <summary>Devise FlexPay (<c>D_p</c>) — montant réellement envoyé au prestataire.</summary>
        public string CodeDevise { get; set; } = "CDF";

        /// <summary>Montant tarif métier (<c>D_t</c>) avant conversion.</summary>
        public decimal MontantTarif { get; set; }

        /// <summary>Devise tarif réservation (<c>D_t</c>).</summary>
        public string CodeDeviseTarif { get; set; } = "CDF";

        /// <summary>Taux <c>D_t</c> → <c>D_p</c> (1 si devises identiques).</summary>
        public decimal TauxVersDevisePaiement { get; set; } = 1m;

        public DateTime DateCreation { get; set; }
    }
}
