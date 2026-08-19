using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Models.Evenement
{
    public class EvenementSession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEvenementSession { get; set; }

        [Required]
        public int IdSociete { get; set; }

        /// <summary>
        /// Site opérationnel (lieu / guichet / bénéficiaire PayOut futur).
        /// Nullable pour sessions legacy ; requis à la création Draft.
        /// </summary>
        public int? IdSite { get; set; }

        [Required]
        [MaxLength(64)]
        public string CodeSession { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartAtUtc { get; set; }

        public DateTime? EndAtUtc { get; set; }

        [Required]
        public EvenementInventoryMode InventoryMode { get; set; }

        [Required]
        public EvenementSessionStatus Status { get; set; } = EvenementSessionStatus.Draft;

        [Required]
        public EvenementSessionType TypeEvenement { get; set; } = EvenementSessionType.Autres;

        [MaxLength(255)]
        public string? NomOrganisateur { get; set; }

        [MaxLength(50)]
        public string? TelephoneOrganisateur { get; set; }

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

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Site? Site { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public EvenementSessionGlobalQuota? GlobalQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementSessionSection> Sections { get; set; } = new List<EvenementSessionSection>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementSessionClassQuota> ClassQuotas { get; set; } = new List<EvenementSessionClassQuota>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementSessionSeat> Seats { get; set; } = new List<EvenementSessionSeat>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementReservation> Reservations { get; set; } = new List<EvenementReservation>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementSessionPhoto> Photos { get; set; } = new List<EvenementSessionPhoto>();
    }
}
