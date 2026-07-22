namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour la gestion du stockage de fichiers
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Upload un fichier sur le serveur
        /// </summary>
        Task<FileUploadResult> UploadFileAsync(IFormFile file, string subfolder);
        
        /// <summary>
        /// Supprime un fichier du serveur
        /// </summary>
        Task<bool> DeleteFileAsync(string filePath);
        
        /// <summary>
        /// Récupère le stream d'un fichier
        /// </summary>
        Task<FileStream> GetFileStreamAsync(string filePath);
        
        /// <summary>
        /// Vérifie si le type de fichier est valide
        /// </summary>
        bool IsValidFileType(string fileName);
        
        /// <summary>
        /// Vérifie si la taille du fichier est valide
        /// </summary>
        bool IsValidFileSize(long fileSize);
        
        /// <summary>
        /// Obtient le type MIME d'un fichier
        /// </summary>
        string GetContentType(string fileName);
    }
    
    /// <summary>
    /// Résultat d'un upload de fichier
    /// </summary>
    public class FileUploadResult
    {
        public string FileName { get; set; } = string.Empty; // Nom unique généré
        public string OriginalFileName { get; set; } = string.Empty; // Nom original
        public string FilePath { get; set; } = string.Empty; // Chemin relatif
        public long FileSize { get; set; }
        public string TypeMIME { get; set; } = string.Empty;
    }
}

