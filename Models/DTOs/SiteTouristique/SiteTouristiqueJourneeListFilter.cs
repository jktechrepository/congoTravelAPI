using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueJourneeListFilter
    {
        public SiteTouristiqueStatus? Status { get; set; }
        public SiteTouristiqueInventoryMode? InventoryMode { get; set; }
        public int? IdSiteTouristique { get; set; }
        public int? IdSociete { get; set; }
        public DateOnly? DateVisite { get; set; }
        public DateOnly? DateVisiteFrom { get; set; }
        public DateOnly? DateVisiteTo { get; set; }
    }
}
