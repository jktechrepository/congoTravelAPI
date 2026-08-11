using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Restaurant
{
    /// <summary>Ligne d'inventaire demandée dans un hold (selon InventoryMode du créneau).</summary>
    public class RestaurantHoldItemRequestDto
    {
        /// <summary>Mode ClassQuota : identifiant zone.</summary>
        public int? ZoneId { get; set; }

        /// <summary>Nombre de couverts (&gt; 0).</summary>
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }
}
