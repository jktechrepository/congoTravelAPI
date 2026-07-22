namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Réponse <c>GET /api/events/sessions/{idSession}/availability</c> (champs selon le mode).</summary>
    public class EvenementAvailabilityResponseDto
    {
        public int IdEvenementSession { get; set; }

        public string InventoryMode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        /// <summary>Renseigné uniquement si <c>InventoryMode == GlobalQuota</c>.</summary>
        public EvenementGlobalQuotaAvailabilityDto? GlobalQuota { get; set; }

        /// <summary>Renseigné uniquement si <c>InventoryMode == ClassQuota</c>.</summary>
        public List<EvenementClassQuotaAvailabilityDto>? ClassQuotas { get; set; }

        /// <summary>Renseigné uniquement si <c>InventoryMode == SeatNumbered</c>.</summary>
        public List<EvenementSeatAvailabilityDto>? Seats { get; set; }
    }
}
