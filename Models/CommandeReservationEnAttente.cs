using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongoTravel.Models
{
    /// <summary>
    /// Commande transport en attente de confirmation FlexPay (payload métier JSON, pas de réservation).
    /// </summary>
    public class CommandeReservationEnAttente
    {
        [Key]
        public Guid IdCommandeReservationEnAttente { get; set; } = Guid.NewGuid();

        [Required]
        public int IdSociete { get; set; }

        public int? IdSite { get; set; }

        [Required]
        public int IdUtilisateur { get; set; }

        /// <summary>Origine capturée à l'initiation FlexPay (pas de session HTTP au callback).</summary>
        [Required]
        [MaxLength(20)]
        public string Origine { get; set; } = Enums.OrigineOperation.Default;

        [Required]
        [MaxLength(50)]
        public string MethodePaiement { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantVoyage { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDeviseVoyage { get; set; } = "CDF";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantFlexPay { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevisePaiement { get; set; } = "CDF";

        [Column(TypeName = "decimal(18,8)")]
        public decimal TauxVersDevisePaiement { get; set; } = 1m;

        [MaxLength(100)]
        public string? OrderNumberFlexPay { get; set; }

        [MaxLength(100)]
        public string? ReferenceFlexPay { get; set; }

        /// <summary>Snapshot JSON : réservation, passagers, etc.</summary>
        [Required]
        public string PayloadMetierJson { get; set; } = "{}";

        public int? IdPaiementEnAttente { get; set; }

        [Required]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime? DateExpiration { get; set; }
    }
}
