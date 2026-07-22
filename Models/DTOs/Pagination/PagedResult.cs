namespace CongoTravel.Models.DTOs.Pagination
{
    /// <summary>
    /// Résultat paginé d'une requête
    /// </summary>
    public class PagedResult<T>
    {
        /// <summary>
        /// Données de la page actuelle
        /// </summary>
        public IEnumerable<T> Data { get; set; } = new List<T>();

        /// <summary>
        /// Nombre total d'éléments
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Numéro de la page actuelle
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Taille de la page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Nombre total de pages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>
        /// Indique s'il y a une page précédente
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Indique s'il y a une page suivante
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedResult(IEnumerable<T> data, int totalCount, int pageNumber, int pageSize)
        {
            Data = data;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}

