namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// DTO pour une page de synchronisation paginée
    /// Structure uniforme pour tous les endpoints de sync paginés
    /// </summary>
    /// <typeparam name="T">Type des éléments de la page</typeparam>
    public class SyncPageDto<T>
    {
        /// <summary>
        /// Token de snapshot utilisé pour garantir la cohérence
        /// </summary>
        public string Snapshot { get; set; } = string.Empty;

        /// <summary>
        /// Liste des éléments de la page courante
        /// </summary>
        public List<T> Items { get; set; } = new();

        /// <summary>
        /// Token de pagination pour la page suivante (opaque, signé)
        /// </summary>
        public string? NextCursor { get; set; }

        /// <summary>
        /// Indique s'il y a une page suivante
        /// </summary>
        public bool HasMore { get; set; }

        /// <summary>
        /// Watermark serveur pour la prochaine synchronisation delta
        /// </summary>
        public string? NextSince { get; set; }
    }
}
