using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Ligne d'inventaire demandée dans un hold (selon <c>InventoryMode</c> de la session).</summary>
    public class EvenementHoldItemRequestDto
    {
        /// <summary>Mode A (<c>SeatNumbered</c>) : identifiant <c>EvenementSessionSeat</c>.</summary>
        public int? SeatId { get; set; }

        /// <summary>Mode B (<c>ClassQuota</c>) : identifiant <c>EvenementClasse</c>.</summary>
        public int? ClassId { get; set; }

        /// <summary>Mode C (<c>GlobalQuota</c>) ou B : quantité demandée (&gt; 0).</summary>
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }
}
