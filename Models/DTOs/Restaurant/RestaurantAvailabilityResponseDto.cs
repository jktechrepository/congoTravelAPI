namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantAvailabilityResponseDto
    {
        public int IdRestaurantCreneau { get; set; }

        public int IdSociete { get; set; }

        public string? NomSociete { get; set; }

        public string InventoryMode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public RestaurantGlobalQuotaAvailabilityDto? GlobalQuota { get; set; }

        public List<RestaurantZoneQuotaAvailabilityDto>? ZoneQuotas { get; set; }
    }

    public class RestaurantGlobalQuotaAvailabilityDto
    {
        public int CapaciteTotale { get; set; }

        public int QuantiteHold { get; set; }

        public int QuantiteVendue { get; set; }

        /// <summary>Couverts encore réservables : CapaciteTotale - Hold - Vendue.</summary>
        public int QuantiteDisponible { get; set; }

        public decimal PrixUnitaire { get; set; }

        public decimal? MontantAcompteUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";
    }

    public class RestaurantZoneQuotaAvailabilityDto
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

        public decimal? MontantAcompteUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";
    }
}
