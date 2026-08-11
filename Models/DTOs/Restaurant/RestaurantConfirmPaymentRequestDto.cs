using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantConfirmPaymentRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string MethodePaiement { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ReferenceTransaction { get; set; }

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }
    }
}
