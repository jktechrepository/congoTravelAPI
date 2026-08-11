using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueCreateJourneeGlobalQuotaDto
    {
        [Range(1, int.MaxValue)]
        public int CapaciteTotale { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PrixUnitaire { get; set; }
    }
}
