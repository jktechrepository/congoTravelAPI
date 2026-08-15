namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Service de cache pour optimiser les performances
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Récupère une valeur du cache ou l'exécute et la met en cache
        /// </summary>
        Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

        /// <summary>
        /// Récupère une valeur du cache
        /// </summary>
        T? Get<T>(string key);

        /// <summary>
        /// Définit une valeur dans le cache
        /// </summary>
        void Set<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Supprime une entrée du cache
        /// </summary>
        void Remove(string key);

        /// <summary>
        /// Supprime toutes les entrées commençant par un préfixe
        /// </summary>
        void RemoveByPrefix(string prefix);

        /// <summary>
        /// Vide tout le cache
        /// </summary>
        void Clear();
    }
}

