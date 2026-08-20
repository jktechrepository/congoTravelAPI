using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueCommandeSnapshotLineDto
    {
        public SiteTouristiqueReservationLineType LineType { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public int? IdSiteTouristiqueClassQuota { get; set; }
    }

    /// <summary>Payload métier sérialisé pour finaliser la réservation au callback.</summary>
    public class SiteTouristiqueCommandeSnapshotDto
    {
        public SiteTouristiqueReservationWithPaiementRequestDto Request { get; set; } = new();
        public List<SiteTouristiqueCommandeSnapshotLineDto> Lines { get; set; } = new();
        public decimal MontantSousTotal { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public string ReferenceReservation { get; set; } = string.Empty;
        public string? CustomerRef { get; set; }
    }
}
