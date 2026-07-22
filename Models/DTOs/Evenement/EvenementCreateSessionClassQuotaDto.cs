using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Quota par classe lors de la création d'une session <c>ClassQuota</c>.</summary>
    public class EvenementCreateSessionClassQuotaDto
    {
        [Required]
        public int IdEvenementClasse { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int CapaciteTotale { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";
    }
}
