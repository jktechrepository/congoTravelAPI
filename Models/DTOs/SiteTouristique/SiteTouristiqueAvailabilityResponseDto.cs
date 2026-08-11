namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueAvailabilityResponseDto
    {
        public int IdSiteTouristiqueJournee { get; set; }
        public string InventoryMode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public SiteTouristiqueGlobalQuotaAvailabilityDto? GlobalQuota { get; set; }
        public List<SiteTouristiqueClassQuotaAvailabilityDto>? ClassQuotas { get; set; }
    }
}
