namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// Snapshot ConfigSociete pour agents offline.
    /// </summary>
    public class ConfigSocieteSyncDto
    {
        public int IdSociete { get; set; }
        public int? JoursAvanceMaxReservation { get; set; }
        public int DureeValiditeBilletJours { get; set; }
        public bool ReaffectationActive { get; set; }
        public int HeuresLimiteReaffectation { get; set; }
        public decimal PenaliteReaffectationPourcentage { get; set; }
        public int DureeHoldFlexPayMinutes { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
