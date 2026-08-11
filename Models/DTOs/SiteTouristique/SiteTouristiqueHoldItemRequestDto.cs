using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>Ligne d'inventaire demandée dans un hold (selon <c>InventoryMode</c> de la journée).</summary>
    public class SiteTouristiqueHoldItemRequestDto
    {
        /// <summary>Mode <c>ClassQuota</c> : identifiant <c>SiteTouristiqueClasse</c>.</summary>
        public int? ClassId { get; set; }

        /// <summary>Mode <c>GlobalQuota</c> ou ClassQuota : quantité demandée (&gt; 0).</summary>
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }
}
