using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantHoldRequestDto
    {
        [MaxLength(100)]
        public string? CustomerRef { get; set; }

        /// <summary>Client acheteur (optionnel). Prioritaire sur <c>Utilisateur.IdClient</c> du JWT.</summary>
        [Range(1, int.MaxValue)]
        public int? IdClient { get; set; }

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        /// <summary>Site opérationnel (override du site établissement).</summary>
        [Range(1, int.MaxValue)]
        public int? IdSite { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Au moins un item est requis.")]
        public List<RestaurantHoldItemRequestDto> Items { get; set; } = new();
    }
}
