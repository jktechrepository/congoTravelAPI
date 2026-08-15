using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>
    /// Corps unifié pour <c>POST /api/sites-touristiques/reservations/with-paiement</c>
    /// et <c>POST /api/sites-touristiques/reservations/with-paiement-electronique</c>
    /// (miroir Transport : hold + paiement en un appel).
    /// </summary>
    public class SiteTouristiqueReservationWithPaiementRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdSiteTouristiqueJournee { get; set; }

        [MaxLength(100)]
        public string? CustomerRef { get; set; }

        /// <summary>Client acheteur (optionnel). Prioritaire sur <c>Utilisateur.IdClient</c> du JWT.</summary>
        [Range(1, int.MaxValue)]
        public int? IdClient { get; set; }

        /// <summary>Clé d'idempotence hold (unique par société).</summary>
        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Au moins un item est requis.")]
        public List<SiteTouristiqueHoldItemRequestDto> Items { get; set; } = new();

        [Required]
        public SiteTouristiqueReservationPaiementBlockDto Paiement { get; set; } = new();
    }

    /// <summary>Bloc paiement (CASH ou FlexPay selon l'endpoint).</summary>
    public class SiteTouristiqueReservationPaiementBlockDto
    {
        [Required]
        [MaxLength(50)]
        public string MethodePaiement { get; set; } = string.Empty;

        /// <summary>Référence caisse / reçu (CASH).</summary>
        [MaxLength(100)]
        public string? ReferenceTransaction { get; set; }

        /// <summary>Obligatoire pour <c>MOBILE_MONEY</c>.</summary>
        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>Site marchand FlexPay (obligatoire électronique).</summary>
        [Range(1, int.MaxValue)]
        public int? IdSite { get; set; }

        [MaxLength(3)]
        public string? CodeDevisePaiement { get; set; }

        /// <summary>Clé d'idempotence paiement (sinon dérivée de la clé hold).</summary>
        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }
    }
}
