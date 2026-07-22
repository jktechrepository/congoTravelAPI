using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Classe de réponse paginée standardisée
    /// </summary>
    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public PagedResponse(List<T> data, int currentPage, int pageSize, int totalItems)
        {
            Data = data;
            CurrentPage = currentPage;
            PageSize = pageSize;
            TotalItems = totalItems;
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        }
    }

    /// <summary>
    /// Paramètres de pagination standardisés
    /// </summary>
    public class PaginationParams
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 20; // Par défaut : 20 éléments

        public int Page { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : (value < 1 ? 20 : value);
        }
    }

    /// <summary>
    /// Extensions pour faciliter la pagination
    /// </summary>
    public static class PaginationExtensions
    {
        /// <summary>
        /// Applique la pagination à une requête IQueryable
        /// </summary>
        public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, PaginationParams pagination)
        {
            return query
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize);
        }

        /// <summary>
        /// Crée une réponse paginée à partir d'une requête
        /// </summary>
        public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
            this IQueryable<T> query,
            PaginationParams pagination)
        {
            var totalItems = await Task.Run(() => query.Count());
            var data = await Task.Run(() => 
                query.ApplyPagination(pagination).ToList()
            );

            return new PagedResponse<T>(data, pagination.Page, pagination.PageSize, totalItems);
        }

        /// <summary>
        /// Crée une réponse paginée à partir d'une liste déjà chargée
        /// </summary>
        public static PagedResponse<T> ToPagedResponse<T>(
            this List<T> list,
            PaginationParams pagination)
        {
            var totalItems = list.Count;
            var data = list
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            return new PagedResponse<T>(data, pagination.Page, pagination.PageSize, totalItems);
        }

        /// <summary>
        /// Ajoute les headers de pagination à la réponse HTTP
        /// </summary>
        public static void AddPaginationHeaders<T>(this ControllerBase controller, PagedResponse<T> response)
        {
            controller.Response.Headers.Append("X-Pagination-CurrentPage", response.CurrentPage.ToString());
            controller.Response.Headers.Append("X-Pagination-PageSize", response.PageSize.ToString());
            controller.Response.Headers.Append("X-Pagination-TotalItems", response.TotalItems.ToString());
            controller.Response.Headers.Append("X-Pagination-TotalPages", response.TotalPages.ToString());
            controller.Response.Headers.Append("X-Pagination-HasPreviousPage", response.HasPreviousPage.ToString());
            controller.Response.Headers.Append("X-Pagination-HasNextPage", response.HasNextPage.ToString());
        }
    }
}

