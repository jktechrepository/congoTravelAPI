using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour la gestion du stockage de fichiers avec AWS S3
    /// </summary>
    public class S3FileStorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<S3FileStorageService> _logger;
        private readonly IConfiguration _configuration;
        
        // Constantes pour les devoirs à domicile
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] ALLOWED_EXTENSIONS = { ".pdf", ".jpg", ".jpeg", ".png" };
        private static readonly string[] ALLOWED_MIME_TYPES = { 
            "application/pdf", 
            "image/jpeg", 
            "image/png" 
        };
        
        private readonly string _bucketName;
        private readonly string _region;
        
        public S3FileStorageService(
            IAmazonS3 s3Client,
            ILogger<S3FileStorageService> logger,
            IConfiguration configuration)
        {
            _s3Client = s3Client;
            _logger = logger;
            _configuration = configuration;
            _bucketName = _configuration["AWS:S3:BucketName"] ?? throw new ArgumentNullException("AWS:S3:BucketName");
            _region = _configuration["AWS:S3:Region"] ?? "us-east-1";
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
            
            // Construire la clé S3 (chemin dans le bucket)
            var s3Key = $"{subfolder}/{uniqueFileName}";
            
            try
            {
                // Upload vers S3
                using (var fileStream = file.OpenReadStream())
                {
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = fileStream,
                        Key = s3Key,
                        BucketName = _bucketName,
                        ContentType = GetContentType(file.FileName),
                        CannedACL = S3CannedACL.Private // Fichiers privés par défaut
                    };
                    
                    var transferUtility = new TransferUtility(_s3Client);
                    await transferUtility.UploadAsync(uploadRequest);
                }
                
                _logger.LogInformation($"Fichier uploadé avec succès vers S3 : {_bucketName}/{s3Key} ({file.Length} bytes)");
                
                // Retourner le résultat
                // FilePath contient la clé S3 pour référence future
                return new FileUploadResult
                {
                    FileName = uniqueFileName,
                    OriginalFileName = file.FileName,
                    FilePath = s3Key, // Clé S3 au lieu du chemin local
                    FileSize = file.Length,
                    TypeMIME = GetContentType(file.FileName)
                };
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, $"Erreur AWS S3 lors de l'upload : {ex.Message}");
                throw new InvalidOperationException($"Erreur lors de l'upload vers S3 : {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur inattendue lors de l'upload vers S3");
                throw;
            }
        }
        
        public async Task<FileUploadResult> UploadBytesAsync(
            byte[] content,
            string relativeKey,
            string contentType,
            string? originalFileName = null,
            CancellationToken cancellationToken = default)
        {
            if (content == null || content.Length == 0)
                throw new ArgumentException("Le contenu binaire est vide ou null", nameof(content));

            if (string.IsNullOrWhiteSpace(relativeKey))
                throw new ArgumentException("La clé relative est obligatoire", nameof(relativeKey));

            var s3Key = relativeKey.Trim().TrimStart('/');
            var fileName = Path.GetFileName(s3Key);
            var resolvedContentType = string.IsNullOrWhiteSpace(contentType)
                ? GetContentType(fileName)
                : contentType;

            try
            {
                using var memoryStream = new MemoryStream(content, writable: false);
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = memoryStream,
                    Key = s3Key,
                    BucketName = _bucketName,
                    ContentType = resolvedContentType,
                    CannedACL = S3CannedACL.Private
                };

                var transferUtility = new TransferUtility(_s3Client);
                await transferUtility.UploadAsync(uploadRequest, cancellationToken);

                _logger.LogInformation(
                    "Octets uploadés vers S3 : {Bucket}/{Key} ({Size} bytes)",
                    _bucketName, s3Key, content.Length);

                return new FileUploadResult
                {
                    FileName = fileName,
                    OriginalFileName = originalFileName ?? fileName,
                    FilePath = s3Key,
                    FileSize = content.Length,
                    TypeMIME = resolvedContentType
                };
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Erreur AWS S3 lors de l'upload bytes : {Key}", s3Key);
                throw new InvalidOperationException($"Erreur lors de l'upload vers S3 : {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                // filePath est la clé S3
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = filePath
                };
                
                var response = await _s3Client.DeleteObjectAsync(deleteRequest);
                
                _logger.LogInformation($"Fichier supprimé de S3 : {_bucketName}/{filePath}");
                return true;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, $"Erreur AWS S3 lors de la suppression : {filePath}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur inattendue lors de la suppression depuis S3 : {filePath}");
                return false;
            }
        }

        public async Task<byte[]> GetFileBytesAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var getRequest = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = filePath
                };

                using var response = await _s3Client.GetObjectAsync(getRequest, cancellationToken);
                using var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
                _logger.LogInformation("Octets téléchargés depuis S3 : {Bucket}/{Key}", _bucketName, filePath);
                return memoryStream.ToArray();
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Erreur AWS S3 lors du téléchargement bytes : {Key}", filePath);
                throw new FileNotFoundException($"Fichier introuvable dans S3 : {filePath}", ex);
            }
        }
        
        public async Task<FileStream> GetFileStreamAsync(string filePath)
        {
            try
            {
                // filePath est la clé S3
                var getRequest = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = filePath
                };
                
                var response = await _s3Client.GetObjectAsync(getRequest);
                
                // Télécharger le contenu dans un MemoryStream puis créer un FileStream temporaire
                using (var memoryStream = new MemoryStream())
                {
                    await response.ResponseStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    
                    // Créer un fichier temporaire et y écrire le contenu
                    var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(filePath));
                    using (var writeStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await memoryStream.CopyToAsync(writeStream);
                    }
                    
                    // Retourner un FileStream en lecture seule
                    var readStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                    _logger.LogInformation($"Fichier téléchargé depuis S3 : {_bucketName}/{filePath}");
                    return readStream;
                }
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, $"Erreur AWS S3 lors du téléchargement : {filePath}");
                throw new FileNotFoundException($"Fichier introuvable dans S3 : {filePath}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur inattendue lors du téléchargement depuis S3 : {filePath}");
                throw;
            }
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

