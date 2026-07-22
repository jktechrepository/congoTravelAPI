namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// Projection billet pour sync offline delta.
    /// </summary>
    public class BilletSyncDto
    {
        public int IdBillet { get; set; }
        public int IdSociete { get; set; }
        public int? IdReservation { get; set; }
        public int? IdReservationPassenger { get; set; }
        public int? IdSiege { get; set; }
        public string QrCode { get; set; } = string.Empty;
        public bool IsUsed { get; set; }
        public DateTime DateGeneration { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
