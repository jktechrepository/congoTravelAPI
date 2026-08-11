namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueClassQuotaAvailabilityDto
    {
        public int IdSiteTouristiqueClassQuota { get; set; }
        public int IdSiteTouristiqueClasse { get; set; }
        public string? CodeClasse { get; set; }
        public string LibelleClasse { get; set; } = string.Empty;
        public int CapaciteTotale { get; set; }
        public int QuantiteHold { get; set; }
        public int QuantiteVendue { get; set; }
        public int QuantiteDisponible { get; set; }
        public decimal PrixUnitaire { get; set; }
        public string CodeDevise { get; set; } = "CDF";
    }
}
