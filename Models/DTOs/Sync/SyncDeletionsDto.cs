namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// DTO pour les suppressions depuis la dernière synchronisation
    /// Permet au mobile de nettoyer son cache local
    /// </summary>
    public class SyncDeletionsDto
    {
        /// <summary>
        /// Token de snapshot utilisé pour garantir la cohérence
        /// </summary>
        public string Snapshot { get; set; } = string.Empty;

        /// <summary>
        /// Liste des IDs de clients supprimés (soft delete)
        /// </summary>
        public List<int> DeletedClientIds { get; set; } = new();

        /// <summary>
        /// Liste des IDs de factures client sorties du filtre (soldées, annulées, etc.)
        /// </summary>
        public List<int> RemovedClientFactureIds { get; set; } = new();

        /// <summary>
        /// Liste des IDs de paiements supprimés
        /// </summary>
        public List<int> DeletedPaymentIds { get; set; } = new();

        /// <summary>
        /// Watermark serveur pour la prochaine synchronisation delta
        /// </summary>
        public string? NextSince { get; set; }
    }
}
