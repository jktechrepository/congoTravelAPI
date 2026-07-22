namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// Chemins REST du workflow réservation V2 (multi-passagers, sièges, billets).
    /// À inclure dans le bootstrap sync pour orienter les apps mobiles hors bundle delta réservations.
    /// </summary>
    public class ReservationWorkflowV2ApiHintsDto
    {
        /// <summary>Version logique du contrat décrit par ces chemins.</summary>
        public int SchemaVersion { get; set; } = 2;

        /// <summary>POST création réservation + paiement (route canonique).</summary>
        public string PostReservationWithPaiementPath { get; set; } = "/api/Reservation/reservation_with_paiement";

        /// <summary>POST même corps que la route canonique (alias Phase D).</summary>
        public string PostReservationWithPaiementAliasPath { get; set; } = "/api/Reservation/with-passengers-and-paiement";

        /// <summary>POST initiation FlexPay (MOBILE_MONEY / CARTE_BANCAIRE) — pas de réservation avant callback.</summary>
        public string PostReservationWithPaiementElectroniquePath { get; set; } = "/api/Reservation/reservation_with_paiement_electronique";

        public string GetVoyageDestinationsTemplate { get; set; } = "/api/Voyage/{id}/destinations";

        public string GetVoyageSiegesDisponiblesTemplate { get; set; } = "/api/Voyage/{id}/sieges-disponibles";

        public string GetVoyageSiegesIndisponiblesTemplate { get; set; } = "/api/Voyage/{id}/sieges-indisponibles";

        public string GetReservationPassagersTemplate { get; set; } = "/api/Reservation/{id}/passagers";

        public string GetReservationBilletsTemplate { get; set; } = "/api/Reservation/{id}/billets";
    }
}
