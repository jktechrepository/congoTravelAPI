using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueLieuListFilter
    {
        public SiteTouristiqueStatus? Status { get; set; }
        public int? IdSociete { get; set; }
    }
}
