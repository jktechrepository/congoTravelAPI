namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// Projection voyage pour sync offline (snapshot du jour).
    /// </summary>
    public class VoyageSyncDto
    {
        public int IdVoyage { get; set; }
        public int IdSociete { get; set; }
        public int? IdSite { get; set; }
        public DateTime DateDepart { get; set; }
        public TimeSpan HeureDepart { get; set; }
        public int Prix { get; set; }
        public string CodeDevisePrix { get; set; } = "CDF";
        public string? VilleDepart { get; set; }
        public string? VilleArrivee { get; set; }
        public int IdVehicule { get; set; }
        public int? CapaciteSieges { get; set; }
        public bool Statut { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
