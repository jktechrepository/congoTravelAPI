using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Ligne d’inventaire figée dans <c>EvenementCommandeEnAttente.PayloadMetierJson</c>.</summary>
    public class EvenementCommandeSnapshotLineDto
    {
        public EvenementReservationLineType LineType { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public int? IdEvenementSessionClassQuota { get; set; }
        public int? IdEvenementSessionSeat { get; set; }
    }

    /// <summary>Payload métier sérialisé pour finaliser la réservation au callback.</summary>
    public class EvenementCommandeSnapshotDto
    {
        public EvenementReservationWithPaiementRequestDto Request { get; set; } = new();
        public List<EvenementCommandeSnapshotLineDto> Lines { get; set; } = new();
        public decimal MontantSousTotal { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public string ReferenceReservation { get; set; } = string.Empty;
        public string? CustomerRef { get; set; }
    }
}
