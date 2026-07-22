using Microsoft.AspNetCore.Hosting;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour le scan antivirus des fichiers
    /// Phase 1 : Validation basique (extension, headers, taille)
    /// Phase 2 : Intégration avec un scanner antivirus réel (ClamAV, Windows Defender, etc.)
    /// </summary>
    public class AntivirusService : IAntivirusService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AntivirusService> _logger;
        private readonly IFileStorageService _fileStorageService;
        
        // Signatures de fichiers valides (magic bytes)
        private static readonly byte[] PDF_SIGNATURE = { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        private static readonly byte[] JPEG_SIGNATURE = { 0xFF, 0xD8, 0xFF }; // JPEG commence par FF D8 FF
        private static readonly byte[] PNG_SIGNATURE = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG signature complète
        
        public AntivirusService(
            IWebHostEnvironment environment, 
            ILogger<AntivirusService> logger,
            IFileStorageService fileStorageService)
        {
            _environment = environment;
            _logger = logger;
            _fileStorageService = fileStorageService;
        }
        
        public async Task<bool> ScanFileAsync(string filePath)
        {
            try
            {
                // Utiliser IFileStorageService pour obtenir le stream (fonctionne avec local et S3)
                using (var fileStream = await _fileStorageService.GetFileStreamAsync(filePath))
                {
                    return await ScanStreamAsync(fileStream, Path.GetFileName(filePath));
                }
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning($"Fichier introuvable pour scan : {filePath}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du scan du fichier : {filePath}");
                return false;
            }
        }
        
        public async Task<bool> ScanStreamAsync(Stream fileStream, string fileName)
        {
            try
            {
                // Vérifier l'extension
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                
                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning($"Extension non autorisée : {extension} pour {fileName}");
                    return false;
                }
                
                // Vérifier la signature du fichier (magic bytes)
                // Lire les premiers bytes selon le type de fichier
                var bufferSize = extension == ".png" ? 8 : 4; // PNG nécessite 8 bytes, autres 4 bytes
                var buffer = new byte[bufferSize];
                var bytesRead = await fileStream.ReadAsync(buffer, 0, bufferSize);
                
                if (bytesRead < bufferSize)
                {
                    _logger.LogWarning($"Fichier trop petit ou corrompu : {fileName}");
                    return false;
                }
                
                // Vérifier la signature selon le type de fichier
                bool isValidSignature = false;
                
                if (extension == ".pdf")
                {
                    // Vérifier signature PDF (4 premiers bytes)
                    isValidSignature = buffer.Take(4).SequenceEqual(PDF_SIGNATURE);
                }
                else if (extension == ".jpg" || extension == ".jpeg")
                {
                    // Vérifier signature JPEG (3 premiers bytes : FF D8 FF)
                    isValidSignature = buffer.Take(3).SequenceEqual(JPEG_SIGNATURE);
                }
                else if (extension == ".png")
                {
                    // Vérifier signature PNG (8 bytes complets)
                    isValidSignature = buffer.SequenceEqual(PNG_SIGNATURE);
                }
                
                if (!isValidSignature)
                {
                    _logger.LogWarning($"Signature invalide pour {fileName}. Signature détectée : {BitConverter.ToString(buffer)}");
                    return false;
                }
                
                // Vérifications supplémentaires (optionnel)
                // Note: La validation de taille minimale a été supprimée pour permettre les fichiers de toute taille
                
                // Phase 1 : Validation basique réussie
                _logger.LogInformation($"Scan antivirus réussi pour {fileName} (Type: {extension})");
                return true;
                
                // TODO Phase 2 : Intégrer un scanner antivirus réel
                // - ClamAV (Linux)
                // - Windows Defender (Windows)
                // - Service cloud (VirusTotal API, etc.)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du scan du stream : {fileName}");
                return false;
            }
        }
    }
}


