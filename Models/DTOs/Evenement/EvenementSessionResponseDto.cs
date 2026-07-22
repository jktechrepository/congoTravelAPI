namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Détail session événement (GET, publish, etc.).</summary>
    public class EvenementSessionResponseDto
    {
        public int IdEvenementSession { get; set; }

        public int IdSociete { get; set; }

        public string CodeSession { get; set; } = string.Empty;

        public string Libelle { get; set; } = string.Empty;

        public DateTime StartAtUtc { get; set; }

        public DateTime? EndAtUtc { get; set; }

        public string InventoryMode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }

        public EvenementGlobalQuotaAvailabilityDto? GlobalQuota { get; set; }

        public List<EvenementClassQuotaAvailabilityDto> ClassQuotas { get; set; } = new();

        public List<EvenementSeatAvailabilityDto> Seats { get; set; } = new();
    }
}
