namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Disponibilité mode <c>ClassQuota</c> pour une classe de session.</summary>
    public class EvenementClassQuotaAvailabilityDto
    {
        public int IdEvenementSessionClassQuota { get; set; }

        public int IdEvenementClasse { get; set; }

        public string CodeClasse { get; set; } = string.Empty;

        public string LibelleClasse { get; set; } = string.Empty;

        public int CapaciteTotale { get; set; }

        public int QuantiteHold { get; set; }

        public int QuantiteVendue { get; set; }

        public int QuantiteDisponible { get; set; }

        public decimal PrixUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";
    }
}
