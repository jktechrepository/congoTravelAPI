namespace CongoTravel.Models.DTOs
{
    /// <summary>Réponse du contrôle d’un billet (usage, réservation, voyage, fenêtre d’embarquement).</summary>
    public class BilletCheckResponseDto
    {
        /// <summary>Identifiant billet si trouvé en base ; null sinon.</summary>
        public int? IdBillet { get; set; }

        /// <summary><c>true</c> = déjà embarqué, <c>false</c> = non utilisé, <c>null</c> = billet inconnu.</summary>
        public bool? IsUsed { get; set; }

        /// <summary>
        /// Synthèse : NonReconnu | DejaUtilise | EmbarquementDejaEnregistre | ReservationInactive | ReservationInvalide |
        /// VoyageIndisponible | HorsFenetreEmbarquement | ValideSansReservation | Valide.
        /// </summary>
        public string Statut { get; set; } = string.Empty;

        /// <summary>Message affichable pour l’utilisateur.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Indique si l’embarquement peut être enclenché (scan final) selon les règles actuelles.</summary>
        public bool EmbarquementAutorise { get; set; }

        /// <summary>Statut brut de la réservation si chargée (ex. CONFIRMEE, EN_ATTENTE).</summary>
        public string? StatutReservation { get; set; }

        /// <summary>Identifiant réservation liée au billet, si présent.</summary>
        public int? IdReservation { get; set; }

        /// <summary>Date de départ du voyage (jour), si connue.</summary>
        public DateTime? DateDepartVoyage { get; set; }

        /// <summary>Heure de départ du voyage, si connue (sérialisation JSON standard).</summary>
        public TimeSpan? HeureDepartVoyage { get; set; }

        /// <summary>Nom affiché à l’embarquement : passager réel (NomComplet), pas l’acheteur.</summary>
        public string? NomClient { get; set; }

        /// <summary>Téléphone affiché à l’embarquement : passager réel, pas l’acheteur.</summary>
        public string? TelephoneClient { get; set; }
    }
}
