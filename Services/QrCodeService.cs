using CongoTravel.Data;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service de génération de QR Codes uniques pour les billets
    /// </summary>
    public class QrCodeService : IQrCodeService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<QrCodeService> _logger;

        public QrCodeService(CongoTravelDbContext context, ILogger<QrCodeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Génère un QR Code unique pour un billet
        /// Format: RT-XXX-YYYYMMDDHHMMSS-NNNN
        /// Exemple: RT-001-20260423210945-1234
        /// </summary>
        public async Task<string> GenerateUniqueQrCodeAsync(int idSociete, int? idReservation = null)
        {
            const int maxAttempts = 10;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                var qrCode = GenerateQrCodeFormat(idSociete);
                
                if (!await QrCodeExistsAsync(qrCode))
                {
                    _logger.LogInformation("QR Code unique généré: {QrCode} pour société {IdSociete}, réservation {IdReservation}", 
                        qrCode, idSociete, idReservation);
                    return qrCode;
                }

                attempts++;
                _logger.LogWarning("Collision QR Code détectée: {QrCode} (tentative {Attempt}/{MaxAttempts})", 
                    qrCode, attempts, maxAttempts);
                
                // Petit délai pour éviter les collisions
                await Task.Delay(10);
            }

            throw new InvalidOperationException($"Impossible de générer un QR Code unique après {maxAttempts} tentatives");
        }

        /// <summary>
        /// Vérifie si un QR Code existe déjà dans la base de données
        /// </summary>
        public async Task<bool> QrCodeExistsAsync(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
                return true;

            return await _context.Billets
                .AnyAsync(b => b.QrCode == qrCode);
        }

        /// <summary>
        /// Génère un QR Code avec un format personnalisé
        /// </summary>
        public async Task<string> GenerateCustomQrCodeAsync(string format, object parameters)
        {
            // Implémentation basique pour les formats personnalisés
            // Peut être étendue selon les besoins spécifiques
            var qrCode = format
                .Replace("{societe}", "001")
                .Replace("{date}", DateTime.UtcNow.ToString("yyyyMMddHHmmss"))
                .Replace("{random}", new Random().Next(1000, 9999).ToString());

            // S'assurer que le QR Code est unique
            int attempts = 0;
            while (await QrCodeExistsAsync(qrCode) && attempts < 10)
            {
                qrCode = qrCode.Replace($"{new Random().Next(1000, 9999)}", $"{new Random().Next(1000, 9999)}");
                attempts++;
            }

            if (attempts >= 10)
            {
                throw new InvalidOperationException("Impossible de générer un QR Code personnalisé unique");
            }

            return qrCode;
        }

        /// <summary>
        /// Génère un QR Code au format standard RT-XXX-YYYYMMDDHHMMSS-NNNN
        /// </summary>
        private string GenerateQrCodeFormat(int idSociete)
        {
            var now = DateTime.UtcNow;
            var random = new Random();
            
            return $"RT-{idSociete:D3}-{now:yyyyMMddHHmmss}-{random.Next(1000, 9999):D4}";
        }

        /// <summary>
        /// Valide le format d'un QR Code
        /// </summary>
        public bool IsValidQrCodeFormat(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
                return false;

            // Format attendu: RT-XXX-YYYYMMDDHHMMSS-NNNN
            return System.Text.RegularExpressions.Regex.IsMatch(qrCode, 
                @"^RT-\d{3}-\d{14}-\d{4}$");
        }

        /// <summary>
        /// Extrait l'ID de la société à partir d'un QR Code
        /// </summary>
        public int? ExtractSocieteIdFromQrCode(string qrCode)
        {
            if (!IsValidQrCodeFormat(qrCode))
                return null;

            var parts = qrCode.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var societeId))
            {
                return societeId;
            }

            return null;
        }
    }
}
