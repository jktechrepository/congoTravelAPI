namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// Projection réservation pour sync offline delta.
    /// </summary>
    public class ReservationSyncDto
    {
        public int IdReservation { get; set; }
        public int IdSociete { get; set; }
        public int IdVoyage { get; set; }
        public int IdClient { get; set; }
        public int? IdSite { get; set; }
        public int NombreDePlace { get; set; }
        public string StatutReservation { get; set; } = string.Empty;
        public DateTime DateReservation { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
