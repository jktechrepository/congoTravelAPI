using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Configuration marchand FlexPay pour un site (1 site = 1 marchand).
    /// </summary>
    public class InfoPaiementSociete
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdInfoPaiementSociete { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdSite { get; set; }

        [Required]
        [MaxLength(100)]
        public string CodeMarchand { get; set; } = string.Empty;

        /// <summary>Token FlexPay (Bearer) — ne jamais exposer en clair via l'API.</summary>
        [Required]
        [MaxLength(500)]
        public string ApiToken { get; set; } = string.Empty;

        public bool ActifMobileMoney { get; set; } = true;

        public bool ActifCarteBancaire { get; set; } = true;

        public bool Statut { get; set; } = true;

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        public Site? Site { get; set; }

        [JsonIgnore]
        public Societe? Societe { get; set; }
    }
}
