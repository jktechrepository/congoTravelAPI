namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantCreneauZoneQuotaDto
    {
        public int IdRestaurantCreneauZoneQuota { get; set; }
        public int IdRestaurantZone { get; set; }
        public string? CodeZone { get; set; }
        public string LibelleZone { get; set; } = string.Empty;
        public int CapaciteTotale { get; set; }
        public int QuantiteHold { get; set; }
        public int QuantiteVendue { get; set; }
        public int QuantiteDisponible { get; set; }
        public decimal PrixUnitaire { get; set; }
        public string CodeDevise { get; set; } = "CDF";
    }
}
