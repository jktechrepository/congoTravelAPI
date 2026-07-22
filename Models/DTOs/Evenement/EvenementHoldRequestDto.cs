using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Corps de <c>POST /api/events/sessions/{idSession}/holds</c>.</summary>
    public class EvenementHoldRequestDto
    {
        /// <summary>Référence opaque client (téléphone, code interne, etc.).</summary>
        [MaxLength(100)]
        public string? CustomerRef { get; set; }

        /// <summary>Clé d'idempotence (unique par société).</summary>
        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Au moins un item est requis.")]
        public List<EvenementHoldItemRequestDto> Items { get; set; } = new();
    }
}
