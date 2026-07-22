using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour la gestion du stockage de fichiers (stockage local)
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileStorageService> _logger;
        
        // Constantes pour les devoirs à domicile
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] ALLOWED_EXTENSIONS = { ".pdf", ".jpg", ".jpeg", ".png" };
        private static readonly string[] ALLOWED_MIME_TYPES = { 
            "application/pdf", 
            "image/jpeg", 
            "image/png" 
        };
        
        public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }
        
        public async Task<FileUploadResult> UploadFileAsync(IFormFile file, string subfolder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Le fichier est vide ou null", nameof(file));
            }
            
            // Validation du type de fichier
            if (!IsValidFileType(file.FileName))
            {
                throw new InvalidOperationException($"Type de fichier non autorisé. Formats acceptés : PDF, JPG, PNG.");
            }
            
            // Validation de la taille
            if (!IsValidFileSize(file.Length))
            {
                throw new InvalidOperationException($"Fichier trop volumineux. Taille maximum : {MAX_FILE_SIZE / (1024 * 1024)} MB");
            }
            
            // Générer un nom de fichier unique
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            
            // Créer le dossier de destination s'il n'existe pas
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", subfolder);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
                _logger.LogInformation($"Dossier créé : {uploadPath}");
            }
            
            // Chemin complet du fichier
            var filePath = Path.Combine(uploadPath, uniqueFileName);
            
            // Sauvegarder le fichier
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            _logger.LogInformation($"Fichier uploadé avec succès : {filePath} ({file.Length} bytes)");
            
            // Retourner le résultat
            return new FileUploadResult
            {
                FileName = uniqueFileName,
                OriginalFileName = file.FileName,
                FilePath = Path.Combine("uploads", subfolder, uniqueFileName).Replace("\\", "/"), // Chemin relatif avec /
                FileSize = file.Length,
                TypeMIME = GetContentType(file.FileName)
            };
        }
        
        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                // Construire le chemin complet
                var fullPath = Path.Combine(_environment.WebRootPath, filePath);
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"Fichier supprimé : {fullPath}");
                    return await Task.FromResult(true);
                }
                
                _logger.LogWarning($"Fichier introuvable pour suppression : {fullPath}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression du fichier : {filePath}");
                return false;
            }
        }
        
        public Task<FileStream> GetFileStreamAsync(string filePath)
        {
            // Construire le chemin complet
            var fullPath = Path.Combine(_environment.WebRootPath, filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Fichier introuvable : {filePath}");
            }
            
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(fileStream);
        }
        
        public bool IsValidFileType(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;
            
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return ALLOWED_EXTENSIONS.Contains(extension);
        }
        
        public bool IsValidFileSize(long fileSize)
        {
            return fileSize > 0 && fileSize <= MAX_FILE_SIZE;
        }
        
        public string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}

