using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantCreateCreneauRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdRestaurant { get; set; }

        [Required]
        public DateOnly DateService { get; set; }

        [Required]
        public DateTime StartAtUtc { get; set; }

        [Required]
        public DateTime EndAtUtc { get; set; }

        /// <summary><c>GlobalQuota</c> ou <c>ClassQuota</c> (zones).</summary>
        [Required]
        public string InventoryMode { get; set; } = "GlobalQuota";

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CodeDevise { get; set; } = "CDF";

        [Range(0, double.MaxValue)]
        public decimal? MontantAcompte { get; set; }

        public RestaurantCreateCreneauGlobalQuotaDto? GlobalQuota { get; set; }

        /// <summary>Obligatoire si <c>InventoryMode = ClassQuota</c>.</summary>
        public List<RestaurantCreateCreneauZoneQuotaDto>? ZoneQuotas { get; set; }
    }
}
