using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Corps de <c>POST /api/events/sessions</c> (session <c>Draft</c>).</summary>
    public class EvenementCreateSessionRequestDto
    {
        [Required]
        [MaxLength(64)]
        public string CodeSession { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        public DateTime StartAtUtc { get; set; }

        public DateTime? EndAtUtc { get; set; }

        /// <summary>V1 Phase 2 : <c>GlobalQuota</c> uniquement côté service ; autres modes en phases ultérieures.</summary>
        [Required]
        public string InventoryMode { get; set; } = "GlobalQuota";

        /// <summary>Obligatoire si <c>InventoryMode == GlobalQuota</c>.</summary>
        public EvenementCreateSessionGlobalQuotaDto? GlobalQuota { get; set; }

        /// <summary>Obligatoire si <c>InventoryMode == ClassQuota</c>.</summary>
        public List<EvenementCreateSessionClassQuotaDto>? ClassQuotas { get; set; }

        /// <summary>Plan de salle par sections (mode <c>SeatNumbered</c>).</summary>
        public List<EvenementCreateSessionSectionDto>? Sections { get; set; }

        /// <summary>Sièges hors section (mode <c>SeatNumbered</c>).</summary>
        public List<EvenementCreateSessionSeatDto>? Seats { get; set; }
    }
}
