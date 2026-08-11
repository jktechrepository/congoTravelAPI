using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>
    /// Corps hold (items inventaire) — utiliséé via
    /// <c>POST /api/sites-touristiques/reservations/with-paiement</c> et <c>with-paiement-electronique</c>.
    /// </summary>
    public class SiteTouristiqueHoldRequestDto
    {
        /// <summary>Référence opaque client (téléphone, code interne, etc.).</summary>
        [MaxLength(100)]
        public string? CustomerRef { get; set; }

        /// <summary>Clé d'idempotence (unique par société).</summary>
        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// Site effectif à persister sur la réservation
        /// (résolu par la façade : <c>paiement.idSite ?? journee.idSite</c>).
        /// </summary>
        public int? IdSite { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Au moins un item est requis.")]
        public List<SiteTouristiqueHoldItemRequestDto> Items { get; set; } = new();
    }
}
