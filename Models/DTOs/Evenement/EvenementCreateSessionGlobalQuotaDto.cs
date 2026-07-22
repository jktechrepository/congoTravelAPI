using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Inventaire global pour une session Mode C (création back-office).</summary>
    public class EvenementCreateSessionGlobalQuotaDto
    {
        [Range(1, int.MaxValue)]
        public int CapaciteTotale { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CodeDevise { get; set; } = "CDF";
    }
}
