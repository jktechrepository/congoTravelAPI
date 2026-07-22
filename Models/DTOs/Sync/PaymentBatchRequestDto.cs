using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// DTO pour une requête batch de paiements offline
    /// Permet l'upload multiple avec idempotence
    /// </summary>
    public class PaymentBatchRequestDto
    {
        /// <summary>
        /// Liste des paiements à traiter
        /// </summary>
        [Required(ErrorMessage = "La liste des paiements est requise")]
        public List<PaymentRequestDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO pour un paiement individuel dans un batch
    /// Contient tous les champs nécessaires pour l'idempotence
    /// </summary>
    public class PaymentRequestDto
    {
        /// <summary>
        /// Identifiant unique de la demande client (UUID) pour idempotence
        /// </summary>
        [Required(ErrorMessage = "Le ClientRequestId est requis")]
        [StringLength(36, ErrorMessage = "Le ClientRequestId doit faire 36 caractères maximum")]
        public string ClientRequestId { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant du client
        /// </summary>
        [Required(ErrorMessage = "L'IdClient est requis")]
        public int IdClient { get; set; }

        /// <summary>
        /// Identifiant de la facture client (pour arriérés)
        /// </summary>
        public int? IdClientFacture { get; set; }

        /// <summary>
        /// Identifiant de la facture (pour factures système)
        /// </summary>
        public int? IdFacture { get; set; }

        /// <summary>
        /// Montant payé par le client
        /// </summary>
        [Required(ErrorMessage = "Le MontantPaye est requis")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal MontantPaye { get; set; }

        /// <summary>
        /// Date du paiement (UTC)
        /// </summary>
        [Required(ErrorMessage = "La DatePaiementUtc est requise")]
        public DateTime DatePaiementUtc { get; set; }

        /// <summary>
        /// Méthode de paiement (Espèces, Mobile Money, Virement, etc.)
        /// </summary>
        [Required(ErrorMessage = "La MethodePaiement est requise")]
        [StringLength(50, ErrorMessage = "La méthode de paiement doit faire 50 caractères maximum")]
        public string MethodePaiement { get; set; } = string.Empty;

        /// <summary>
        /// Référence de la transaction (numéro de transaction, référence virement, etc.)
        /// </summary>
        [StringLength(100, ErrorMessage = "La référence de transaction doit faire 100 caractères maximum")]
        public string? ReferenceTransaction { get; set; }

        /// <summary>
        /// Commentaire ou note sur le paiement
        /// </summary>
        [StringLength(500, ErrorMessage = "Le commentaire doit faire 500 caractères maximum")]
        public string? Commentaire { get; set; }

        /// <summary>
        /// Identifiant du device mobile
        /// </summary>
        [StringLength(50, ErrorMessage = "L'identifiant du device doit faire 50 caractères maximum")]
        public string? DeviceId { get; set; }

        /// <summary>
        /// Identifiant de l'agent qui a effectué le paiement
        /// </summary>
        public int? AgentId { get; set; }
    }
}
