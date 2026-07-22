using CongoTravel.Services.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace CongoTravel.Services
{
    /// <summary>
    /// Implémentation du service de cache In-Memory
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;
        private readonly HashSet<string> _cacheKeys = new();
        private readonly object _lock = new();

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Récupère du cache ou exécute et met en cache
        /// </summary>
        public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T? cachedValue))
            {
                _logger.LogDebug($"✅ Cache HIT : {key}");
                return cachedValue;
            }

            _logger.LogDebug($"❌ Cache MISS : {key} - Exécution de la requête");

            var value = await factory();

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };

            _cache.Set(key, value, cacheOptions);

            lock (_lock)
            {
                _cacheKeys.Add(key);
            }

            return value;
        }

        /// <summary>
        /// Récupère une valeur du cache
        /// </summary>
        public T? Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug($"✅ Cache GET : {key}");
                return value;
            }

            _logger.LogDebug($"❌ Cache GET MISS : {key}");
            return default;
        }

        /// <summary>
        /// Définit une valeur dans le cache
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
            };

            _cache.Set(key, value, cacheOptions);

            lock (_lock)
            {
                _cacheKeys.Add(key);
            }

            _logger.LogDebug($"✅ Cache SET : {key}");
        }

        /// <summary>
        /// Supprime une entrée du cache
        /// </summary>
        public void Remove(string key)
        {
            _cache.Remove(key);

            lock (_lock)
            {
                _cacheKeys.Remove(key);
            }

            _logger.LogDebug($"🗑️ Cache REMOVE : {key}");
        }

        /// <summary>
        /// Supprime toutes les entrées commençant par un préfixe
        /// </summary>
        public void RemoveByPrefix(string prefix)
        {
            List<string> keysToRemove;

            lock (_lock)
            {
                keysToRemove = _cacheKeys.Where(k => k.StartsWith(prefix)).ToList();
            }

            foreach (var key in keysToRemove)
            {
                Remove(key);
            }

            _logger.LogInformation($"🗑️ Cache REMOVE BY PREFIX : {prefix} - {keysToRemove.Count} entrées supprimées");
        }

        /// <summary>
        /// Vide tout le cache
        /// </summary>
        public void Clear()
        {
            List<string> allKeys;

            lock (_lock)
            {
                allKeys = _cacheKeys.ToList();
                _cacheKeys.Clear();
            }

            foreach (var key in allKeys)
            {
                _cache.Remove(key);
            }

            _logger.LogWarning($"🗑️ Cache CLEAR : {allKeys.Count} entrées supprimées");
        }
    }
}

