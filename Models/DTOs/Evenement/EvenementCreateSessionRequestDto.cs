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

        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>Site opérationnel de la session (requis, doit appartenir à la société).</summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int IdSite { get; set; }

        [Required]
        public DateTime StartAtUtc { get; set; }

        public DateTime? EndAtUtc { get; set; }

        /// <summary>V1 Phase 2 : <c>GlobalQuota</c> uniquement côté service ; autres modes en phases ultérieures.</summary>
        [Required]
        public string InventoryMode { get; set; } = "GlobalQuota";

        /// <summary>Type éditorial de l'événement ; si omis, <c>Autres</c>.</summary>
        public string? TypeEvenement { get; set; }

        [MaxLength(255)]
        public string? NomOrganisateur { get; set; }

        [MaxLength(50)]
        public string? TelephoneOrganisateur { get; set; }

        [MaxLength(30)]
        public string? NumeroMobileMoneyOrganisateur { get; set; }

        public bool VenteEnLigneActive { get; set; } = true;

        public bool AutoReversementOrganisateur { get; set; } = true;

        [EmailAddress]
        [MaxLength(255)]
        public string? MailOrganisateur { get; set; }

        [MaxLength(1000)]
        public string? LogoOrganisateur { get; set; }

        [MaxLength(100)]
        public string? Ville { get; set; }

        [MaxLength(100)]
        public string? Commune { get; set; }

        [MaxLength(100)]
        public string? Quartier { get; set; }

        [MaxLength(200)]
        public string? Avenue { get; set; }

        [MaxLength(50)]
        public string? Numero { get; set; }

        /// <summary>Obligatoire si <c>InventoryMode == GlobalQuota</c>.</summary>
        public EvenementCreateSessionGlobalQuotaDto? GlobalQuota { get; set; }

        /// <summary>Obligatoire si <c>InventoryMode == ClassQuota</c>.</summary>
        public List<EvenementCreateSessionClassQuotaDto>? ClassQuotas { get; set; }

        /// <summary>Plan de salle par sections (mode <c>SeatNumbered</c>).</summary>
        public List<EvenementCreateSessionSectionDto>? Sections { get; set; }

        /// <summary>Sièges hors section (mode <c>SeatNumbered</c>).</summary>
        public List<EvenementCreateSessionSeatDto>? Seats { get; set; }

        /// <summary>
        /// LEGACY / déprécié — photos embarquées en photoBase64 (max 3).
        /// Préférer : créer la session sans photos, puis POST/PUT multipart
        /// <c>/api/events/sessions/{id}/photos</c>.
        /// </summary>
        public List<AddEvenementSessionPhotoDto>? Photos { get; set; }
    }
}
