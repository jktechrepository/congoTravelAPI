using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    /// <summary>Résultat d'une ligne d'inventaire réservée par une stratégie de hold.</summary>
    public class EvenementHoldLineResult
    {
        public EvenementReservationLineType LineType { get; set; }

        public int Quantite { get; set; }

        public decimal PrixUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public int? IdEvenementSessionClassQuota { get; set; }

        public int? IdEvenementSessionSeat { get; set; }
    }

    /// <summary>Résultat agrégé d'une réservation de hold sur l'inventaire.</summary>
    public class EvenementHoldStrategyResult
    {
        public IReadOnlyList<EvenementHoldLineResult> Lines { get; init; } = Array.Empty<EvenementHoldLineResult>();

        public decimal MontantSousTotal { get; init; }
    }

    public class EvenementInventoryHoldRequest
    {
        public EvenementSession Session { get; set; } = null!;

        public IReadOnlyList<EvenementHoldItemRequestDto> Items { get; set; } = Array.Empty<EvenementHoldItemRequestDto>();

        /// <summary>Mode C : prix unitaire (fourni par le service appelant jusqu'à persistance inventaire).</summary>
        public decimal PrixUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        /// <summary>Mode A : expiration du hold à appliquer sur les sièges.</summary>
        public DateTime HoldExpiresAtUtc { get; set; }
    }
}
