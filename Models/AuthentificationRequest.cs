using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models
{
    public class AuthentificationRequest
    {
        [Required(ErrorMessage = "L'email ou le téléphone est requis")]
        public string? EmailOuTelephone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        public string? MotDePasse { get; set; } = string.Empty;

        // ✨ Informations du device pour les notifications push (optionnelles)
        [MaxLength(500)]
        public string? FcmToken { get; set; } // Token Firebase Cloud Messaging

        [MaxLength(50)]
        public string? DeviceType { get; set; } // Android, iOS, Web

        [MaxLength(100)]
        public string? DeviceModel { get; set; } // Ex: iPhone 12, Samsung Galaxy S21

        [MaxLength(50)]
        public string? OsVersion { get; set; } // Ex: Android 12, iOS 15.2
    }
}