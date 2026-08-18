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
        public bool ReservationIsActif { get; set; }
        public bool ActiviteTransport { get; set; }
        public bool ActiviteEvenement { get; set; }
        public bool ActiviteSiteTouristique { get; set; }
        public bool ActiviteRestaurant { get; set; }
        public int HeuresLimiteReaffectation { get; set; }
        public decimal PenaliteReaffectationPourcentage { get; set; }
        public int DureeHoldFlexPayMinutes { get; set; }
        /// <summary>Poids de bagage offert (kg) ; 0 = aucun.</summary>
        public decimal PoidsBagageParKiloOffert { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
