using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Remboursement d'un paiement avec snapshots multi-devise.
    /// </summary>
    public class Remboursement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRemboursement { get; set; }

        [Required]
        public int IdPaiement { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdUtilisateur { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDeviseRemboursement { get; set; } = "CDF";

        [Required]
        [MaxLength(3)]
        public string CodeDevisePrincipale { get; set; } = "CDF";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantRembourse { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal TauxVersDevisePrincipale { get; set; } = 1m;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantRembourseDevisePrincipale { get; set; }

        [Required]
        public DateTime DateRemboursement { get; set; } = DateTime.UtcNow;

        [MaxLength(250)]
        public string? Motif { get; set; }

        public bool Statut { get; set; } = true;

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        [ForeignKey("IdPaiement")]
        public Paiement? Paiement { get; set; }
    }
}

