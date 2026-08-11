using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueCreateJourneeRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdSiteTouristique { get; set; }

        [Required]
        public DateOnly DateVisite { get; set; }

        [Required]
        public string InventoryMode { get; set; } = "GlobalQuota";

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CodeDevise { get; set; } = "CDF";

        public DateTime? SalesOpenAtUtc { get; set; }
        public DateTime? SalesCloseAtUtc { get; set; }

        public SiteTouristiqueCreateJourneeGlobalQuotaDto? GlobalQuota { get; set; }
        public List<SiteTouristiqueCreateJourneeClassQuotaDto>? ClassQuotas { get; set; }
    }
}
