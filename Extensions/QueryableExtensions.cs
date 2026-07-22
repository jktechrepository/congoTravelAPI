using Microsoft.EntityFrameworkCore;
using CongoTravel.Models.DTOs.Pagination;
using System.Linq.Expressions;

namespace CongoTravel.Extensions
{
    /// <summary>
    /// Extensions pour faciliter la pagination sur IQueryable
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Convertit un IQueryable en PagedResult (pagination offset-based)
        /// Usage: var result = await query.ToPagedAsync(request);
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedAsync<T>(
            this IQueryable<T> query,
            PagedRequest request,
            CancellationToken cancellationToken = default)
        {
            // Compter le total AVANT le Skip/Take pour éviter de charger toutes les données
            var totalRecords = await query.CountAsync(cancellationToken);

            // Appliquer Skip et Take
            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>(data, totalRecords, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// Convertit un IQueryable en PagedResult avec tri dynamique
        /// Usage: var result = await query.ToPagedAsync(request, e => e.NomComplet);
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedAsync<T, TKey>(
            this IQueryable<T> query,
            PagedRequest request,
            Expression<Func<T, TKey>> defaultSortExpression,
            CancellationToken cancellationToken = default)
        {
            // Appliquer le tri par défaut
            var sortedQuery = request.SortDescending
                ? query.OrderByDescending(defaultSortExpression)
                : query.OrderBy(defaultSortExpression);

            return await sortedQuery.ToPagedAsync(request, cancellationToken);
        }

        /// <summary>
        /// Convertit un IQueryable en CursorPaginatedResult (pagination cursor-based)
        /// Usage: var result = await query.ToCursorPagedAsync(request, e => e.IdEleve);
        /// </summary>
        /// <typeparam name="T">Type de l'entité</typeparam>
        /// <typeparam name="TCursor">Type du curseur (int, long, DateTime, Guid, etc.)</typeparam>
        public static async Task<CursorPaginatedResult<T>> ToCursorPagedAsync<T, TCursor>(
            this IQueryable<T> query,
            CursorPaginationRequest request,
            Expression<Func<T, TCursor>> cursorSelector,
            CancellationToken cancellationToken = default)
            where TCursor : IComparable<TCursor>
        {
            // Si un curseur est fourni, filtrer les éléments après ce curseur
            if (!string.IsNullOrEmpty(request.Cursor))
            {
                try
                {
                    // Convertir le curseur string vers le type TCursor
                    var cursorValue = (TCursor)Convert.ChangeType(request.Cursor, typeof(TCursor));
                    
                    // Créer une expression pour filtrer : cursorField > cursorValue
                    var parameter = Expression.Parameter(typeof(T), "e");
                    var property = Expression.Invoke(cursorSelector, parameter);
                    var constant = Expression.Constant(cursorValue, typeof(TCursor));
                    var comparison = Expression.GreaterThan(property, constant);
                    var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
                    
                    query = query.Where(lambda);
                }
                catch
                {
                    // Si le curseur est invalide, ignorer le filtre
                }
            }

            // Trier par le curseur (ordre croissant pour cursor-based)
            query = query.OrderBy(cursorSelector);

            // Charger limit + 1 pour savoir s'il y a une page suivante
            var data = await query
                .Take(request.PageSize + 1)
                .ToListAsync(cancellationToken);

            // Déterminer s'il y a plus de données
            var hasMore = data.Count > request.PageSize;
            
            // Retirer l'élément supplémentaire si présent
            if (hasMore)
            {
                data = data.Take(request.PageSize).ToList();
            }

            // Calculer le prochain curseur
            string? nextCursor = null;
            if (hasMore && data.Any())
            {
                var lastItem = data.Last();
                var compiledSelector = cursorSelector.Compile();
                var lastCursorValue = compiledSelector(lastItem);
                nextCursor = lastCursorValue?.ToString();
            }

            return new CursorPaginatedResult<T>(data, nextCursor, hasMore);
        }

        /// <summary>
        /// Applique un tri dynamique basé sur le nom de propriété (string)
        /// Usage: query = query.ApplySort("NomComplet", descending: true);
        /// </summary>
        public static IQueryable<T> ApplySort<T>(
            this IQueryable<T> query,
            string? sortBy,
            bool descending = false)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query;

            // Utiliser la réflexion pour trouver la propriété
            var parameter = Expression.Parameter(typeof(T), "e");
            
            try
            {
                var property = typeof(T).GetProperty(sortBy);
                if (property == null)
                    return query; // Propriété invalide, retourner query inchangée

                var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                var orderByExpression = Expression.Lambda(propertyAccess, parameter);

                var methodName = descending ? "OrderByDescending" : "OrderBy";
                var resultExpression = Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new Type[] { typeof(T), property.PropertyType },
                    query.Expression,
                    Expression.Quote(orderByExpression));

                return query.Provider.CreateQuery<T>(resultExpression);
            }
            catch
            {
                // En cas d'erreur, retourner la query inchangée
                return query;
            }
        }

        /// <summary>
        /// Applique un filtre de recherche textuelle sur plusieurs propriétés
        /// Usage: query = query.ApplySearch(searchTerm, e => e.NomComplet, e => e.Matricule);
        /// </summary>
        public static IQueryable<T> ApplySearch<T>(
            this IQueryable<T> query,
            string? searchTerm,
            params Expression<Func<T, string>>[] properties)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || !properties.Any())
                return query;

            searchTerm = searchTerm.ToLower().Trim();

            var parameter = Expression.Parameter(typeof(T), "e");
            Expression? combinedExpression = null;

            foreach (var property in properties)
            {
                // Créer une expression : property.ToLower().Contains(searchTerm)
                var propertyAccess = Expression.Invoke(property, parameter);
                var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                
                var toLowerCall = Expression.Call(propertyAccess, toLowerMethod);
                var containsCall = Expression.Call(
                    toLowerCall,
                    containsMethod,
                    Expression.Constant(searchTerm));

                // Combiner avec OR
                combinedExpression = combinedExpression == null
                    ? containsCall
                    : Expression.OrElse(combinedExpression, containsCall);
            }

            if (combinedExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                query = query.Where(lambda);
            }

            return query;
        }
    }
}

