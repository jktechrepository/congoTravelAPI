namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Disponibilité mode <c>GlobalQuota</c> (Mode C).</summary>
    public class EvenementGlobalQuotaAvailabilityDto
    {
        public int CapaciteTotale { get; set; }

        public int QuantiteHold { get; set; }

        public int QuantiteVendue { get; set; }

        /// <summary>Places encore réservables : <c>CapaciteTotale - QuantiteHold - QuantiteVendue</c>.</summary>
        public int QuantiteDisponible { get; set; }

        public decimal PrixUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";
    }
}
