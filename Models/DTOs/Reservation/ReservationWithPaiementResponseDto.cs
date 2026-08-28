using System.Collections.Generic;
using System.Text.Json.Serialization;
using CongoTravel.Models.Enums;
using CommonBilletResponseDto = CongoTravel.Models.DTOs.BilletResponseDto;

namespace CongoTravel.Models.DTOs.Reservation
{
    /// <summary>
    /// DTO de réponse pour la création d'une réservation avec paiement
    /// </summary>
    public class ReservationWithPaiementResponseDto
    {
        /// <summary>
        /// Réservation créée
        /// </summary>
        public ReservationResponseDto Reservation { get; set; } = new();

        /// <summary>
        /// Paiement effectué
        /// </summary>
        public PaiementResponseDto Paiement { get; set; } = new();

        /// <summary>
        /// Billet émis (si paiement complet)
        /// </summary>
        public CommonBilletResponseDto? Billet { get; set; }

        /// <summary>
        /// Tous les billets émis (un par passager en workflow V2).
        /// </summary>
        public List<CommonBilletResponseDto> Billets { get; set; } = new();

        /// <summary>
        /// ID de la transaction pour traçabilité
        /// </summary>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        /// Statut global de l'opération
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TransactionStatut Statut { get; set; }

        /// <summary>
        /// Message d'information sur l'opération
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Date de création de la transaction
        /// </summary>
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        // --- Métadonnées FlexPay (null pour POST guichet cash) ---

        /// <summary>Commande en attente (hold sièges) avant callback FlexPay.</summary>
        public Guid? IdCommandeReservationEnAttente { get; set; }

        /// <summary>Numéro de commande FlexPay (polling / verifier).</summary>
        public string? OrderNumberFlexPay { get; set; }

        /// <summary>Référence marchand FlexPay.</summary>
        public string? ReferenceFlexPay { get; set; }

        public decimal? MontantVoyage { get; set; }

        public string? CodeDeviseVoyage { get; set; }

        public decimal? MontantFlexPay { get; set; }

        public string? CodeDevisePaiement { get; set; }

        public decimal? TauxApplique { get; set; }

        /// <summary>Expiration des holds sièges (UTC).</summary>
        public DateTime? HoldExpireAt { get; set; }

        /// <summary>URL de redirection carte bancaire.</summary>
        public string? PaymentUrl { get; set; }

        /// <summary>Initiation acceptée par l'API FlexPay.</summary>
        public bool? FlexPayAccepted { get; set; }
    }

    /// <summary>
    /// DTO de réponse pour le paiement
    /// </summary>
    public class PaiementResponseDto
    {
        /// <summary>
        /// ID du paiement
        /// </summary>
        public int IdPaiement { get; set; }

        /// <summary>
        /// Montant à payer
        /// </summary>
        public decimal MontantAPaye { get; set; }

        /// <summary>
        /// Montant payé
        /// </summary>
        public decimal MontantPaye { get; set; }

        /// <summary>
        /// Reste à payer
        /// </summary>
        public decimal? ResteAPaye { get; set; }

        /// <summary>
        /// Méthode de paiement
        /// </summary>
        public string? MethodePaiement { get; set; }

        /// <summary>
        /// Référence de transaction
        /// </summary>
        public string? ReferenceTransaction { get; set; }

        /// <summary>
        /// Statut du paiement
        /// </summary>
        public bool Statut { get; set; }

        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Date d'émission du billet (si applicable)
        /// </summary>
        public DateTime? DateEmissionBillet { get; set; }

        /// <summary>
        /// ID du billet émis (si applicable)
        /// </summary>
        public int? IdBilletEmis { get; set; }

        /// <summary>
        /// ID de la réservation associée
        /// </summary>
        public int? IdReservation { get; set; }

        /// <summary>Agrégat aller-retour (null = paiement single-leg).</summary>
        public int? IdReservationAllerRetour { get; set; }

        /// <summary>
        /// ID de la société
        /// </summary>
        public int IdSociete { get; set; }

        /// <summary>Site associée au paiement (optionnel).</summary>
        public int? IdSite { get; set; }

        /// <summary>
        /// Indique si le paiement est complet
        /// </summary>
        public bool EstComplet { get; set; }

        /// <summary>
        /// Indique si le paiement est partiel
        /// </summary>
        public bool EstPartiel { get; set; }

        /// <summary>Canal d'origine (CLIENT, CAISSIER, etc.). Snapshot serveur.</summary>
        public string Origine { get; set; } = OrigineOperation.Default;

        /// <summary>Regroupement métier : CLIENT (auto-service) ou AGENT (staff). Dérivé de <see cref="Origine"/>.</summary>
        public string OrigineGroupe { get; set; } = OrigineOperationGroupe.INCONNU;
    }

    /// <summary>
    /// Statuts possibles pour la transaction
    /// </summary>
    public enum TransactionStatut
    {
        /// <summary>
        /// Transaction complétée avec succès
        /// </summary>
        Succes,

        /// <summary>
        /// Transaction complétée mais paiement partiel
        /// </summary>
        SuccesPaiementPartiel,

        /// <summary>
        /// Transaction échouée
        /// </summary>
        Echec,

        /// <summary>
        /// Transaction annulée
        /// </summary>
        Annule,

        /// <summary>
        /// Paiement électronique initié ; réservation créée après callback FlexPay uniquement.
        /// </summary>
        EnAttente
    }
}
