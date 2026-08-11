using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Restaurant
{
    /// <summary>
    /// Corps unifié pour with-paiement (CASH) et with-paiement-electronique (FlexPay).
    /// </summary>
    public class RestaurantReservationWithPaiementRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdRestaurantCreneau { get; set; }

        [MaxLength(100)]
        public string? CustomerRef { get; set; }

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Au moins un item est requis.")]
        public List<RestaurantHoldItemRequestDto> Items { get; set; } = new();

        [Required]
        public RestaurantReservationPaiementBlockDto Paiement { get; set; } = new();
    }

    public class RestaurantReservationPaiementBlockDto
    {
        [Required]
        [MaxLength(50)]
        public string MethodePaiement { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ReferenceTransaction { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Range(1, int.MaxValue)]
        public int? IdSite { get; set; }

        [MaxLength(3)]
        public string? CodeDevisePaiement { get; set; }

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }
    }
}
