namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// Données platées pour le template FastReport <c>Reports/Billet_A4.frx</c>
    /// (noms alignés sur les objets du rapport).
    /// </summary>
    public class BilletReportModel
    {
        public int IdBillet { get; set; }

        /// <summary>FRX: NomClient</summary>
        public string NomClient { get; set; } = string.Empty;

        /// <summary>FRX: code_reservation</summary>
        public string CodeReservation { get; set; } = string.Empty;

        /// <summary>FRX: site (Issue Officer)</summary>
        public string Site { get; set; } = string.Empty;

        /// <summary>FRX: Text1</summary>
        public string DetailsMessage { get; set; } = string.Empty;

        /// <summary>FRX: phone_number</summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>FRX: nom_passager</summary>
        public string NomPassager { get; set; } = string.Empty;

        /// <summary>FRX: email_passager</summary>
        public string EmailPassager { get; set; } = string.Empty;

        /// <summary>FRX: siege</summary>
        public string Siege { get; set; } = string.Empty;

        /// <summary>FRX: reference_billet</summary>
        public string ReferenceBillet { get; set; } = string.Empty;

        /// <summary>FRX: date_voyage</summary>
        public string DateVoyage { get; set; } = string.Empty;

        /// <summary>FRX: avion</summary>
        public string Avion { get; set; } = string.Empty;

        /// <summary>FRX: provenance</summary>
        public string Provenance { get; set; } = string.Empty;

        /// <summary>FRX: heure_depart</summary>
        public string HeureDepart { get; set; } = string.Empty;

        /// <summary>FRX: destination</summary>
        public string Destination { get; set; } = string.Empty;

        /// <summary>FRX: heure_arrive</summary>
        public string HeureArrive { get; set; } = string.Empty;

        /// <summary>FRX: cabin</summary>
        public string Cabin { get; set; } = string.Empty;

        /// <summary>FRX: classe_siege</summary>
        public string ClasseSiege { get; set; } = string.Empty;

        /// <summary>FRX: kilos_bagage</summary>
        public string KilosBagage { get; set; } = string.Empty;

        /// <summary>Nom société (message d'intro).</summary>
        public string NomSociete { get; set; } = string.Empty;
    }
}
