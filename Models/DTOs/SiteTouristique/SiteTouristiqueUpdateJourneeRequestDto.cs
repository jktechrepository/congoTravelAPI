using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>
    /// Corps de <c>PUT /api/sites-touristiques/journees/{id}</c>.
    /// <c>IdSiteTouristique</c> et <c>InventoryMode</c> sont immuables après création.
    /// </summary>
    public class SiteTouristiqueUpdateJourneeRequestDto
    {
        /// <summary>Draft uniquement. Format <c>yyyy-MM-dd</c>.</summary>
        public DateOnly? DateVisite { get; set; }

        /// <summary>Draft uniquement. CDF ou USD.</summary>
        [StringLength(3, MinimumLength = 3)]
        public string? CodeDevise { get; set; }

        /// <summary>Ouverture des ventes (UTC). Null = dès publication / inchangé selon statut.</summary>
        public DateTime? SalesOpenAtUtc { get; set; }

        /// <summary>Fermeture des ventes (UTC). Null = fin du jour DateVisite.</summary>
        public DateTime? SalesCloseAtUtc { get; set; }

        /// <summary>Mise à jour inventaire GlobalQuota (Draft ; Published si aucune vente active).</summary>
        public SiteTouristiqueCreateJourneeGlobalQuotaDto? GlobalQuota { get; set; }

        /// <summary>Mise à jour inventaire ClassQuota (Draft ; Published si aucune vente active).</summary>
        public List<SiteTouristiqueCreateJourneeClassQuotaDto>? ClassQuotas { get; set; }
    }
}
