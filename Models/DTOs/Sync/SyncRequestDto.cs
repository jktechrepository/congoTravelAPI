using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// DTO pour les requêtes de synchronisation
    /// Paramètres communs pour tous les endpoints de sync paginés
    /// </summary>
    public class SyncRequestDto
    {
        /// <summary>
        /// Token de pagination opaque (base64) pour la page suivante
        /// </summary>
        [StringLength(1000)]
        public string? Cursor { get; set; }

        /// <summary>
        /// Taille de la page (défaut: 1000, max: 5000)
        /// </summary>
        [Range(1, 5000)]
        public int PageSize { get; set; } = 1000;

        /// <summary>
        /// Token de snapshot pour garantir la cohérence pendant toute la session
        /// </summary>
        [StringLength(1000)]
        public string? Snapshot { get; set; }

        /// <summary>
        /// Watermark serveur opaque pour la synchronisation delta
        /// </summary>
        [StringLength(1000)]
        public string? Since { get; set; }
    }

    /// <summary>
    /// DTO pour les requêtes de synchronisation des arriérés
    /// Hérite de SyncRequestDto et ajoute des filtres spécifiques
    /// </summary>
    public class SyncArrearsRequestDto : SyncRequestDto
    {
        /// <summary>
        /// Filtre pour n'inclure que les impayés (montantDu > 0)
        /// </summary>
        public bool OnlyOutstanding { get; set; } = true;
    }

    /// <summary>
    /// DTO pour les requêtes de suppressions
    /// </summary>
    public class SyncDeletionsRequestDto
    {
        /// <summary>
        /// Watermark serveur obligatoire pour les suppressions
        /// </summary>
        [Required(ErrorMessage = "Le paramètre 'since' est requis")]
        [StringLength(1000)]
        public string Since { get; set; } = string.Empty;

        /// <summary>
        /// Token de snapshot optionnel pour cohérence
        /// </summary>
        [StringLength(1000)]
        public string? Snapshot { get; set; }
    }
}
