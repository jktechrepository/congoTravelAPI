namespace CongoTravel.Models.DTOs.Voyage
{
    public class ReporterVoyageResultDto
    {
        public int IdVoyage { get; set; }
        public DateTime AncienneDateDepart { get; set; }
        public TimeSpan AncienneHeureDepart { get; set; }
        public DateTime NouvelleDateDepart { get; set; }
        public TimeSpan NouvelleHeureDepart { get; set; }
        public int NombreReservationsImpactees { get; set; }
        public int NombreBilletsRecalcules { get; set; }
        public int NotificationsEnvoyees { get; set; }
        public int NotificationsEchecs { get; set; }
        public List<string> Avertissements { get; set; } = new();
    }
}
