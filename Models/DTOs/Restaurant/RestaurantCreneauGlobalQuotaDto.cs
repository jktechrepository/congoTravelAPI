namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantCreneauGlobalQuotaDto
    {
        public int CapaciteTotale { get; set; }
        public int QuantiteHold { get; set; }
        public int QuantiteVendue { get; set; }
        public int QuantiteDisponible { get; set; }
        public decimal PrixUnitaire { get; set; }
        public string CodeDevise { get; set; } = "CDF";
    }
}
