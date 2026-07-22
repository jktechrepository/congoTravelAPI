using System.Text.Json.Serialization;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// DTO pour la réponse contenant les informations d'un paiement
    /// </summary>
    public class PaiementResponseDto
    {
        /// <summary>
        /// Identifiant unique du paiement
        /// </summary>
        public int IdPaiement { get; set; }

        /// <summary>
        /// Montant total à payer pour la réservation
        /// </summary>
        public decimal MontantAPaye { get; set; }

        /// <summary>
        /// Montant déjà payé
        /// </summary>
        public decimal? MontantPaye { get; set; }

        /// <summary>
        /// Montant restant à payer
        /// </summary>
        public decimal? ResteAPaye { get; set; }

        /// <summary>
        /// Montant restant à payer calculé
        /// </summary>
        public decimal ResteAPayeCalcule { get; set; }

        /// <summary>
        /// Méthode de paiement utilisée
        /// </summary>
        public string? MethodePaiement { get; set; }

        /// <summary>
        /// Référence unique de la transaction
        /// </summary>
        public string? ReferenceTransaction { get; set; }

        /// <summary>
        /// Statut du paiement
        /// </summary>
        public bool Statut { get; set; }

        /// <summary>
        /// Date de création du paiement
        /// </summary>
        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Date de dernière modification du paiement
        /// </summary>
        public DateTime? DateModification { get; set; }

        /// <summary>
        /// Indique si le paiement est complètement payé
        /// </summary>
        public bool EstComplet { get; set; }

        /// <summary>
        /// Indique si le paiement est partiellement payé
        /// </summary>
        public bool EstPartiel { get; set; }

        /// <summary>
        /// Identifiant de l'utilisateur qui a effectué le paiement
        /// </summary>
        public int IdUtilisateur { get; set; }

        /// <summary>
        /// Nom complet de l'utilisateur qui a effectué le paiement
        /// </summary>
        public string? NomUtilisateur { get; set; }

        /// <summary>
        /// Identifiant de la réservation concernée
        /// </summary>
        public int? IdReservation { get; set; }

        /// <summary>
        /// Code de la réservation (si disponible)
        /// </summary>
        public string? CodeReservation { get; set; }

        /// <summary>
        /// Identifiant de la société
        /// </summary>
        public int IdSociete { get; set; }

        /// <summary>
        /// Nom de la société
        /// </summary>
        public string? NomSociete { get; set; }

        /// <summary>Client de la réservation liée (si applicable).</summary>
        public int? IdClient { get; set; }

        /// <summary>Nom du client de la réservation liée (si applicable).</summary>
        public string? NomClient { get; set; }

        /// <summary>Canal d'origine (CLIENT, CAISSIER, etc.). Snapshot serveur.</summary>
        public string Origine { get; set; } = Models.Enums.OrigineOperation.Default;

        /// <summary>Regroupement métier : CLIENT (auto-service) ou AGENT (staff). Dérivé de <see cref="Origine"/>.</summary>
        public string OrigineGroupe { get; set; } = Models.Enums.OrigineOperationGroupe.INCONNU;
    }
}
