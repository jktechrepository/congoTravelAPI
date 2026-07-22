using System.Threading.Tasks;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le service de génération de QR Codes uniques
    /// </summary>
    public interface IQrCodeService
    {
        /// <summary>
        /// Génère un QR Code unique pour un billet
        /// </summary>
        /// <param name="idSociete">Identifiant de la société</param>
        /// <param name="idReservation">Identifiant de la réservation (optionnel)</param>
        /// <returns>QR Code unique au format RT-XXX-YYYYMMDDHHMMSS-NNNN</returns>
        Task<string> GenerateUniqueQrCodeAsync(int idSociete, int? idReservation = null);

        /// <summary>
        /// Vérifie si un QR Code existe déjà dans la base de données
        /// </summary>
        /// <param name="qrCode">QR Code à vérifier</param>
        /// <returns>True si le QR Code existe, false sinon</returns>
        Task<bool> QrCodeExistsAsync(string qrCode);

        /// <summary>
        /// Génère un QR Code avec un format personnalisé
        /// </summary>
        /// <param name="format">Format personnalisé (ex: "RT-{societe}-{date}-{random}")</param>
        /// <param name="parameters">Paramètres pour le format</param>
        /// <returns>QR Code formaté</returns>
        Task<string> GenerateCustomQrCodeAsync(string format, object parameters);
    }
}
