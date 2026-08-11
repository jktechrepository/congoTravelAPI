namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>Réponse <c>201 Created</c> après création d'un hold.</summary>
    public class SiteTouristiqueHoldResponseDto
    {
        public int IdSiteTouristiqueReservation { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public string Status { get; set; } = "HOLD";

        public DateTime ExpiresAtUtc { get; set; }

        public decimal AmountPreview { get; set; }

        public string CodeDevise { get; set; } = "CDF";
    }
}
