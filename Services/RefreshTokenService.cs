using CongoTravel.Data;
using CongoTravel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace CongoTravel.Services
{
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Génère un nouveau refresh token pour un utilisateur
        /// </summary>
        Task<string> GenerateRefreshTokenAsync(int userId, string? deviceInfo = null, string? ipAddress = null);

        /// <summary>
        /// Valide un refresh token et retourne l'ID de l'utilisateur
        /// </summary>
        Task<int?> ValidateRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Révoque un refresh token spécifique
        /// </summary>
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Révoque tous les refresh tokens d'un utilisateur
        /// </summary>
        Task<bool> RevokeAllRefreshTokensAsync(int userId);

        /// <summary>
        /// Nettoie les refresh tokens expirés de la base de données
        /// </summary>
        Task<int> CleanupExpiredTokensAsync();
    }

    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly int _refreshTokenExpirationDays;

        public RefreshTokenService(CongoTravelDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            // Durée de vie du refresh token : 30 jours par défaut
            _refreshTokenExpirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");
        }

        /// <summary>
        /// Génère un token aléatoire sécurisé
        /// </summary>
        private string GenerateRandomToken()
        {
            var randomBytes = new byte[64]; // 512 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Hash un token pour le stockage sécurisé
        /// </summary>
        private string HashToken(string token)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
                return Convert.ToBase64String(hashBytes);
            }
        }

        public async Task<string> GenerateRefreshTokenAsync(int userId, string? deviceInfo = null, string? ipAddress = null)
        {
            // Générer un token aléatoire
            var token = GenerateRandomToken();
            var tokenHash = HashToken(token);

            // Créer l'entité RefreshToken
            var refreshToken = new RefreshToken
            {
                IdUtilisateur = userId,
                TokenHash = tokenHash,
                DateCreation = DateTime.UtcNow,
                DateExpiration = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Retourner le token en clair (il ne sera jamais stocké en clair)
            return token;
        }

        public async Task<int?> ValidateRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            var tokenHash = HashToken(refreshToken);

            // Chercher le token dans la base de données
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (storedToken == null)
                return null;

            // Vérifier si le token est actif (non révoqué et non expiré)
            if (!storedToken.EstActif)
                return null;

            // Vérifier si l'utilisateur existe toujours et est actif
            var utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.IdUtilisateur == storedToken.IdUtilisateur && u.Statut == true);

            if (utilisateur == null)
                return null;

            return storedToken.IdUtilisateur;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var tokenHash = HashToken(refreshToken);

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (storedToken == null || storedToken.EstRevoke)
                return false;

            storedToken.DateRevocation = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RevokeAllRefreshTokensAsync(int userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.IdUtilisateur == userId && rt.DateRevocation == null)
                .ToListAsync();

            if (!tokens.Any())
                return false;

            foreach (var token in tokens)
            {
                token.DateRevocation = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CleanupExpiredTokensAsync()
        {
            // Supprimer les tokens expirés depuis plus de 7 jours (pour garder un historique)
            var cutoffDate = DateTime.UtcNow.AddDays(-7);
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.DateExpiration < cutoffDate
                    || (rt.DateRevocation != null && rt.DateRevocation < cutoffDate))
                .ToListAsync();

            var count = expiredTokens.Count;
            if (count > 0)
            {
                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
            }

            return count;
        }
    }
}

