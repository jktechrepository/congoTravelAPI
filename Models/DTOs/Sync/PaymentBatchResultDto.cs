namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// DTO pour la réponse d'un batch de paiements
    /// Contient les résultats détaillés par paiement et un résumé global
    /// </summary>
    public class PaymentBatchResultDto
    {
        /// <summary>
        /// Liste des résultats pour chaque paiement traité
        /// </summary>
        public List<PaymentResultDto> Results { get; set; } = new();

        /// <summary>
        /// Résumé du traitement du batch
        /// </summary>
        public PaymentSummaryDto Summary { get; set; } = new();
    }

    /// <summary>
    /// DTO pour le résultat d'un paiement individuel
    /// </summary>
    public class PaymentResultDto
    {
        /// <summary>
        /// Identifiant unique de la demande client (pour le suivi)
        /// </summary>
        public string ClientRequestId { get; set; } = string.Empty;

        /// <summary>
        /// Statut du traitement: created, duplicate, rejected, error
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant du paiement créé (si succès)
        /// </summary>
        public int? IdPaiement { get; set; }

        /// <summary>
        /// Nouveau montant dû après ce paiement (si succès)
        /// </summary>
        public decimal? NewMontantDu { get; set; }

        /// <summary>
        /// Message descriptif du résultat
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Code d'erreur (si rejet ou erreur)
        /// </summary>
        public string? ErrorCode { get; set; }
    }

    /// <summary>
    /// DTO pour le résumé du traitement d'un batch de paiements
    /// </summary>
    public class PaymentSummaryDto
    {
        /// <summary>
        /// Nombre total de paiements dans la requête
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Nombre de paiements créés avec succès
        /// </summary>
        public int Created { get; set; }

        /// <summary>
        /// Nombre de paiements en double (idempotence)
        /// </summary>
        public int Duplicates { get; set; }

        /// <summary>
        /// Nombre de paiements rejetés (validation métier)
        /// </summary>
        public int Rejected { get; set; }

        /// <summary>
        /// Nombre d'erreurs techniques
        /// </summary>
        public int Errors { get; set; }
    }
}
