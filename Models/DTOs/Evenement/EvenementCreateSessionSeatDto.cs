using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Siège à créer dans le plan de salle (mode <c>SeatNumbered</c>).</summary>
    public class EvenementCreateSessionSeatDto
    {
        [Required]
        [MaxLength(50)]
        public string SeatCode { get; set; } = string.Empty;

        /// <summary>Classe tarifaire optionnelle (référence <c>IdEvenementClasse</c>).</summary>
        public int? IdEvenementClasse { get; set; }

        [Required]
        public decimal PrixUnitaire { get; set; }

        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";
    }
}
