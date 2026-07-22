using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour la gestion des watermarks sécurisés
    /// Crée et valide les watermarks pour la synchronisation delta
    /// </summary>
    public class WatermarkService : IWatermarkService
    {
        private readonly ILogger<WatermarkService> _logger;
        private readonly string _hmacKey;

        public WatermarkService(IConfiguration configuration, ILogger<WatermarkService> logger)
        {
            _logger = logger;
            // Clé HMAC depuis configuration ou valeur par défaut
            _hmacKey = configuration["Sync:WatermarkKey"] ?? "CongoTravel-Sync-Watermark-Key-2025-Secure";
        }

        /// <summary>
        /// Crée un watermark sécurisé basé sur la date de dernière modification et l'ID
        /// </summary>
        /// <param name="lastModified">Date de dernière modification</param>
        /// <param name="lastId">Dernier ID traité</param>
        /// <returns>Watermark encodé en base64 avec signature HMAC</returns>
        public string CreateWatermark(DateTime lastModified, int lastId)
        {
            try
            {
                // Formater les données avec culture invariante pour éviter les problèmes de fuseau horaire
                var data = $"{lastModified:O}|{lastId}";
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
                _logger.LogError(ex, "Erreur lors de la création du watermark");
                throw new InvalidOperationException("Impossible de créer le watermark", ex);
            }
        }

        /// <summary>
        /// Extrait et valide un watermark sécurisé
        /// </summary>
        /// <param name="watermark">Watermark encodé en base64</param>
        /// <returns>Tuple avec date de dernière modification et dernier ID</returns>
        /// <exception cref="SecurityException">Si le watermark est altéré ou invalide</exception>
        public (DateTime lastModified, int lastId) ParseWatermark(string watermark)
        {
            try
            {
                if (string.IsNullOrEmpty(watermark))
                    throw new ArgumentException("Le watermark ne peut être vide");

                var combinedBytes = Convert.FromBase64String(watermark);
                
                // Vérifier la taille minimale (données + signature HMAC SHA256 = 32 bytes)
                if (combinedBytes.Length < 33)
                    throw new SecurityException("Watermark invalide: taille incorrecte");

                // Séparer les données de la signature (32 derniers bytes pour HMAC SHA256)
                var dataBytes = combinedBytes.Take(combinedBytes.Length - 32).ToArray();
                var signature = combinedBytes.Skip(combinedBytes.Length - 32).ToArray();

                // Valider la signature HMAC
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
                var expectedSignature = hmac.ComputeHash(dataBytes);

                if (!signature.SequenceEqual(expectedSignature))
                {
                    _logger.LogWarning("Tentative de watermark altéré détectée");
                    throw new SecurityException("Watermark altéré ou invalide");
                }

                // Extraire et parser les données
                var data = Encoding.UTF8.GetString(dataBytes);
                var parts = data.Split('|');

                if (parts.Length != 2)
                    throw new SecurityException("Watermark invalide: format incorrect");

                if (!DateTime.TryParseExact(parts[0], "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastModified))
                    throw new SecurityException("Watermark invalide: date incorrecte");

                if (!int.TryParse(parts[1], out var lastId))
                    throw new SecurityException("Watermark invalide: ID incorrect");

                return (lastModified, lastId);
            }
            catch (SecurityException)
            {
                // Relancer les exceptions de sécurité
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du parsing du watermark");
                throw new InvalidOperationException("Impossible de parser le watermark", ex);
            }
        }

        /// <summary>
        /// Crée un watermark initial pour la première synchronisation
        /// </summary>
        /// <returns>Watermark avec date actuelle et ID = 0</returns>
        public string CreateInitialWatermark()
        {
            return CreateWatermark(DateTime.UtcNow, 0);
        }
    }

    /// <summary>
    /// Interface pour le service de watermark
    /// </summary>
    public interface IWatermarkService
    {
        /// <summary>
        /// Crée un watermark sécurisé
        /// </summary>
        string CreateWatermark(DateTime lastModified, int lastId);

        /// <summary>
        /// Extrait et valide un watermark sécurisé
        /// </summary>
        (DateTime lastModified, int lastId) ParseWatermark(string watermark);

        /// <summary>
        /// Crée un watermark initial
        /// </summary>
        string CreateInitialWatermark();
    }
}
