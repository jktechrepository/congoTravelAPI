using Microsoft.AspNetCore.Http;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour la gestion du stockage de fichiers (local ou S3).
    /// </summary>
    public interface IFileStorageService
    {
        Task<FileUploadResult> UploadFileAsync(IFormFile file, string subfolder);

        /// <summary>
        /// Upload d'octets bruts (photos CongoTravel, etc.).
        /// <paramref name="relativeKey"/> = chemin relatif sous le préfixe média (ex. vehicules/12/1-guid.jpg).
        /// </summary>
        Task<FileUploadResult> UploadBytesAsync(
            byte[] content,
            string relativeKey,
            string contentType,
            string? originalFileName = null,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteFileAsync(string filePath);

        Task<FileStream> GetFileStreamAsync(string filePath);

        /// <summary>Télécharge le contenu binaire (clé S3 ou chemin relatif local).</summary>
        Task<byte[]> GetFileBytesAsync(string filePath, CancellationToken cancellationToken = default);

        bool IsValidFileType(string fileName);

        bool IsValidFileSize(long fileSize);

        string GetContentType(string fileName);
    }

    public class FileUploadResult
    {
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        /// <summary>Clé S3 complète ou chemin relatif local (à persister en StorageKey).</summary>
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string TypeMIME { get; set; } = string.Empty;
    }
}
