using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CongoTravel.Helpers;
using CongoTravel.Models.Enums;

namespace CongoTravel.Models
{
    /// <summary>
    /// Reversement FlexPay PayOut vers le NumeroMobileMoney d'un site.
    /// </summary>
    public class ReversementSite
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdReversementSite { get; set; }

        public int? IdPaiement { get; set; }

        public int? IdReservation { get; set; }

        [MaxLength(30)]
        public string Origine { get; set; } = ReversementSiteOrigines.Manuel;

        [Required]
        public int IdSite { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdUtilisateur { get; set; }

        [Required]
        [MaxLength(30)]
        public string NumeroMobileMoney { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }

        [Required]
        [MaxLength(10)]
        public string CodeDevise { get; set; } = "CDF";

        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? OrderNumber { get; set; }

        [MaxLength(100)]
        public string? ProviderReference { get; set; }

        [MaxLength(100)]
        public string? CodeMarchand { get; set; }

        public StatutReversementSite Statut { get; set; } = StatutReversementSite.EnAttente;

        [MaxLength(10)]
        public string? CodeFlexPay { get; set; }

        [MaxLength(500)]
        public string? MessageFlexPay { get; set; }

        [MaxLength(50)]
        public string? Channel { get; set; }

        [MaxLength(500)]
        public string? Motif { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime? DateCallback { get; set; }
    }
}
