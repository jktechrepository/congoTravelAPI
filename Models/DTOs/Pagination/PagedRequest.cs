namespace CongoTravel.Models.DTOs.Pagination
{
    /// <summary>
    /// Paramètres de pagination pour les requêtes
    /// </summary>
    public class PagedRequest
    {
        private int _pageSize = 20;
        private const int MaxPageSize = 100000;

        /// <summary>
        /// Numéro de la page (commence à 1)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Taille de la page (par défaut 20, maximum 100)
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 20 : value);
        }

        /// <summary>
        /// Terme de recherche optionnel
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Propriété par laquelle trier
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Tri décroissant si true, croissant si false
        /// </summary>
        public bool SortDescending { get; set; } = false;

        /// <summary>
        /// Filtre multi-tenant optionnel (IdSociete).
        /// </summary>
        public int? IdSociete { get; set; }
    }
}

