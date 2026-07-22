using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models
{
    /// <summary>
    /// Modèle de requête pour les opérations de refresh token
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// Le refresh token à utiliser ou à révoquer
        /// </summary>
        [Required]
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Informations sur l'appareil (optionnel, pour le refresh)
        /// </summary>
        public string? DeviceInfo { get; set; }
    }
}

