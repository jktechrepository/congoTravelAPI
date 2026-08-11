namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueJourneeListItemDto
    {
        public int IdSiteTouristiqueJournee { get; set; }
        public int IdSociete { get; set; }
        public string? NomSociete { get; set; }
        public int IdSiteTouristique { get; set; }
        public string? CodeLieu { get; set; }
        public string? NomLieu { get; set; }
        public int? IdSite { get; set; }
        public string? NomSite { get; set; }
        public DateOnly DateVisite { get; set; }
        public string InventoryMode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CodeDevise { get; set; } = "CDF";
        public DateTime? SalesOpenAtUtc { get; set; }
        public DateTime? SalesCloseAtUtc { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
        public decimal? PrixMin { get; set; }
        public decimal? PrixMax { get; set; }
    }
}
