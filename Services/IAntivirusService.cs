namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour le scan antivirus des fichiers
    /// </summary>
    public interface IAntivirusService
    {
        /// <summary>
        /// Scanne un fichier pour détecter les virus
        /// </summary>
        /// <param name="filePath">Chemin complet du fichier à scanner</param>
        /// <returns>True si le fichier est sûr, False si un virus est détecté</returns>
        Task<bool> ScanFileAsync(string filePath);
        
        /// <summary>
        /// Scanne un fichier depuis un stream
        /// </summary>
        /// <param name="fileStream">Stream du fichier à scanner</param>
        /// <param name="fileName">Nom du fichier</param>
        /// <returns>True si le fichier est sûr, False si un virus est détecté</returns>
        Task<bool> ScanStreamAsync(Stream fileStream, string fileName);
    }
}

