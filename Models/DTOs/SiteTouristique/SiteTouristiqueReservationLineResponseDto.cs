namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueReservationLineResponseDto
    {
        public int IdSiteTouristiqueReservationLine { get; set; }

        public string LineType { get; set; } = string.Empty;

        public int Quantite { get; set; }

        public decimal PrixUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public int? IdSiteTouristiqueClassQuota { get; set; }
    }
}
