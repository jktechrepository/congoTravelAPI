using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Taux de change manuel par société.
    /// </summary>
    public class TauxChange
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdTauxChange { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDeviseSource { get; set; } = "CDF";

        [Required]
        [MaxLength(3)]
        public string CodeDeviseCible { get; set; } = "CDF";

        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal Taux { get; set; }

        [Required]
        public DateTime DateEffet { get; set; } = DateTime.UtcNow;

        [Required]
        public bool Statut { get; set; } = true;

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ForeignKey("IdSociete")]
        public Societe? Societe { get; set; }
    }
}

