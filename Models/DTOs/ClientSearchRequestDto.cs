namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// DTO pour la recherche de clients avec filtres
    /// </summary>
    public class ClientSearchRequestDto
    {
        /// <summary>
        /// Terme de recherche multi-champs
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Inclure les clients inactifs (défaut = false)
        /// </summary>
        public bool IncludeInactive { get; set; } = false;
    }

    /// <summary>
    /// DTO pour la recherche paginée de clients avec filtres étendus
    /// </summary>
    public class ClientPagedSearchRequestDto : CongoTravel.Models.DTOs.Pagination.PagedRequest
    {
        /// <summary>
        /// Terme de recherche multi-champs
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Inclure les clients inactifs (défaut = false) - Rétro-compatibilité
        /// </summary>
        public bool IncludeInactive { get; set; } = false;

        /// <summary>
        /// Filtre sur le type de courant du client (optionnel)
        /// </summary>
        public int? IdTypeDeCourant { get; set; }

        /// <summary>
        /// Filtre sur le statut actif du client (optionnel, défaut = true si non spécifié)
        /// </summary>
        public bool? IsActif { get; set; }  // Supprimer la valeur par défaut

        /// <summary>
        /// Détermine si un filtre IsActif est explicitement fourni
        /// </summary>
        public bool HasIsActifFilter => IsActif.HasValue;

        /// <summary>
        /// Valeur du filtre IsActif (avec priorité sur IncludeInactive)
        /// </summary>
        public bool ActifFilterValue => IsActif ?? true;
    }
}
