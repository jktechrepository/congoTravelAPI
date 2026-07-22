using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongoTravel.Models
{
    /// <summary>
    /// Référentiel des devises autorisées dans le système.
    /// </summary>
    public class DeviseMonetaire
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDeviseMonetaire { get; set; }

        public int? IdSociete { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = "Franc congolais";

        [MaxLength(10)]
        public string? Symbole { get; set; }

        [Required]
        public bool Statut { get; set; } = true;

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public DateTime? DateModification { get; set; }

        public Societe? Societe { get; set; }
    }
}

