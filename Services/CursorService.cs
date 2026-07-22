using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour la gestion des cursors sécurisés de pagination
    /// Crée et valide les cursors pour la pagination cursor-based
    /// </summary>
    public class CursorService : ICursorService
    {
        private readonly ILogger<CursorService> _logger;
        private readonly string _hmacKey;

        public CursorService(IConfiguration configuration, ILogger<CursorService> logger)
        {
            _logger = logger;
            // Clé HMAC depuis configuration ou valeur par défaut
            _hmacKey = configuration["Sync:CursorKey"] ?? "CongoTravel-Sync-Cursor-Key-2025-Secure";
        }

        /// <summary>
        /// Crée un cursor sécurisé pour une entité
        /// </summary>
        /// <typeparam name="T">Type de l'entité</typeparam>
        /// <param name="entity">Entité avec UpdatedAt et ID</param>
        /// <returns>Cursor encodé en base64 avec signature HMAC</returns>
        public string CreateCursor<T>(T entity) where T : class
        {
            try
            {
                // Extraire les valeurs de tri via réflexion
                var updatedAtValue = GetProperty<DateTime?>(entity, "UpdatedAt") ?? GetProperty<DateTime?>(entity, "DateModification");
                var id = GetProperty<int>(entity, "Id");

                // Formater les données avec culture invariante
                var data = $"{updatedAtValue:O}|{id}";
                var dataBytes = Encoding.UTF8.GetBytes(data);

                // Calculer la signature HMAC SHA256
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
                var signature = hmac.ComputeHash(dataBytes);

                // Combiner données + signature et encoder en base64
                var combinedBytes = dataBytes.Concat(signature).ToArray();
                return Convert.ToBase64String(combinedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du cursor pour l'entité {EntityType}", typeof(T).Name);
                throw new InvalidOperationException($"Impossible de créer le cursor pour {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Extrait et valide un cursor sécurisé
        /// </summary>
        /// <param name="cursor">Cursor encodé en base64</param>
        /// <returns>Tuple avec UpdatedAt et ID</returns>
        /// <exception cref="SecurityException">Si le cursor est altéré ou invalide</exception>
        public (DateTime updatedAt, int id) ParseCursor(string cursor)
        {
            try
            {
                if (string.IsNullOrEmpty(cursor))
                    throw new ArgumentException("Le cursor ne peut être vide");

                var combinedBytes = Convert.FromBase64String(cursor);
                
                // Vérifier la taille minimale (données + signature HMAC SHA256 = 32 bytes)
                if (combinedBytes.Length < 33)
                    throw new SecurityException("Cursor invalide: taille incorrecte");

                // Séparer les données de la signature
                var dataBytes = combinedBytes.Take(combinedBytes.Length - 32).ToArray();
                var signature = combinedBytes.Skip(combinedBytes.Length - 32).ToArray();

                // Valider la signature HMAC
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
                var expectedSignature = hmac.ComputeHash(dataBytes);

                if (!signature.SequenceEqual(expectedSignature))
                {
                    _logger.LogWarning("Tentative de cursor altéré détectée");
                    throw new SecurityException("Cursor altéré ou invalide");
                }

                // Extraire et parser les données
                var data = Encoding.UTF8.GetString(dataBytes);
                var parts = data.Split('|');

                if (parts.Length != 2)
                    throw new SecurityException("Cursor invalide: format incorrect");

                if (!DateTime.TryParseExact(parts[0], "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var updatedAt))
                    throw new SecurityException("Cursor invalide: date incorrecte");

                if (!int.TryParse(parts[1], out var id))
                    throw new SecurityException("Cursor invalide: ID incorrect");

                return (updatedAt, id);
            }
            catch (SecurityException)
            {
                // Relancer les exceptions de sécurité
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du parsing du cursor");
                throw new InvalidOperationException("Impossible de parser le cursor", ex);
            }
        }

        /// <summary>
        /// Extrait une propriété d'une entité par réflexion
        /// </summary>
        /// <typeparam name="T">Type de la propriété</typeparam>
        /// <param name="obj">Entité</param>
        /// <param name="propertyName">Nom de la propriété</param>
        /// <returns>Valeur de la propriété</returns>
        private T GetProperty<T>(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type {obj.GetType().Name}");
            
            return (T)property.GetValue(obj)!;
        }
    }

    /// <summary>
    /// Interface pour le service de cursor
    /// </summary>
    public interface ICursorService
    {
        /// <summary>
        /// Crée un cursor sécurisé pour une entité
        /// </summary>
        string CreateCursor<T>(T entity) where T : class;

        /// <summary>
        /// Extrait et valide un cursor sécurisé
        /// </summary>
        (DateTime updatedAt, int id) ParseCursor(string cursor);
    }
}
