using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    /// <summary>Résultat d'une ligne d'inventaire réservée par une stratégie de hold.</summary>
    public class SiteTouristiqueHoldLineResult
    {
        public SiteTouristiqueReservationLineType LineType { get; set; }

        public int Quantite { get; set; }

        public decimal PrixUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public int? IdSiteTouristiqueClassQuota { get; set; }
    }

    /// <summary>Résultat agrégé d'une réservation de hold sur l'inventaire.</summary>
    public class SiteTouristiqueHoldStrategyResult
    {
        public IReadOnlyList<SiteTouristiqueHoldLineResult> Lines { get; init; } = Array.Empty<SiteTouristiqueHoldLineResult>();

        public decimal MontantSousTotal { get; init; }
    }

    public class SiteTouristiqueInventoryHoldRequest
    {
        public SiteTouristiqueJournee Journee { get; set; } = null!;

        public IReadOnlyList<SiteTouristiqueHoldItemRequestDto> Items { get; set; } = Array.Empty<SiteTouristiqueHoldItemRequestDto>();

        /// <summary>Mode GlobalQuota : prix unitaire (fourni par le service appelant).</summary>
        public decimal PrixUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public DateTime HoldExpiresAtUtc { get; set; }
    }
}
