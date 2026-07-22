namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// DTO pour le bootstrap de synchronisation
    /// Fournit les données initiales pour démarrer la synchronisation
    /// </summary>
    public class SyncBootstrapDto
    {
        /// <summary>
        /// Watermark pour synchronisations futures
        /// </summary>
        public string Watermark { get; set; } = string.Empty;

        /// <summary>
        /// Liste des clients pour synchronisation initiale
        /// </summary>
        public List<ClientSyncDto> Clients { get; set; } = new();

        /// <summary>
        /// Liste des arriérés pour synchronisation initiale
        /// </summary>
        public List<ArrearSyncDto> Arrears { get; set; } = new();

        /// <summary>
        /// Chemins API réservation V2 (multi-passagers / sièges).
        /// </summary>
        public ReservationWorkflowV2ApiHintsDto ReservationWorkflowV2 { get; set; } = new();

        /// <summary>Voyages du jour (snapshot read-only).</summary>
        public List<VoyageSyncDto> Voyages { get; set; } = new();

        /// <summary>Configuration société (snapshot).</summary>
        public ConfigSocieteSyncDto? ConfigSociete { get; set; }
    }

    /// <summary>
    /// DTO pour les métadonnées de bootstrap (alternative)
    /// </summary>
    public class SyncBootstrapMetadataDto
    {
        /// <summary>
        /// Heure serveur actuelle (UTC)
        /// </summary>
        public DateTime ServerTimeUtc { get; set; }

        /// <summary>
        /// Token de snapshot pour garantir la cohérence pendant toute la session de sync
        /// </summary>
        public string Snapshot { get; set; } = string.Empty;

        /// <summary>
        /// Watermark serveur opaque à utiliser comme 'since' pour les delta sync futurs
        /// </summary>
        public string ServerWatermark { get; set; } = string.Empty;

        /// <summary>
        /// Taille de page recommandée pour les requêtes paginées
        /// </summary>
        public int RecommendedPageSize { get; set; } = 1000;

        /// <summary>
        /// Taille maximale de page autorisée
        /// </summary>
        public int MaxPageSize { get; set; } = 5000;

        /// <summary>
        /// Indique si le serveur supporte la synchronisation delta
        /// </summary>
        public bool SupportsDelta { get; set; } = true;

        /// <summary>
        /// Informations sur les volumétries estimées
        /// </summary>
        public DatasetInfoDto Datasets { get; set; } = new();
    }

    /// <summary>
    /// Informations sur un dataset de synchronisation
    /// </summary>
    public class DatasetInfoDto
    {
        /// <summary>
        /// Nombre estimé d'enregistrements dans ce dataset
        /// </summary>
        public long EstimatedCount { get; set; }
    }
}
