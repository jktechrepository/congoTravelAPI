using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models
{
    /// <summary>
    /// DTO pour les requêtes de notifications push
    /// </summary>
    public class NotificationPushRequest
    {
        [Required(ErrorMessage = "Le titre est requis")]
        [MaxLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
        public string Titre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le contenu est requis")]
        [MaxLength(1000, ErrorMessage = "Le contenu ne peut pas dépasser 1000 caractères")]
        public string Corps { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? TypeNotification { get; set; } = "INFO";

        [MaxLength(100)]
        public string? LienAction { get; set; }

        [MaxLength(50)]
        public string? Icone { get; set; }

        public Dictionary<string, string>? Donnees { get; set; }
    }

    /// <summary>
    /// DTO pour les requêtes d'emails
    /// </summary>
    public class NotificationEmailRequest
    {
        [Required(ErrorMessage = "L'adresse email du destinataire est requise.")]
        [EmailAddress(ErrorMessage = "Format d'adresse email invalide.")]
        public string DestinataireEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom du destinataire est requis.")]
        [MaxLength(255, ErrorMessage = "Le nom ne peut pas dépasser 255 caractères.")]
        public string DestinataireNom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le sujet de l'email est requis.")]
        [MaxLength(200, ErrorMessage = "Le sujet ne peut pas dépasser 200 caractères.")]
        public string Sujet { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le corps HTML de l'email est requis.")]
        public string CorpsHtml { get; set; } = string.Empty;

        public string? CorpsTexteBrut { get; set; }
    }

    /// <summary>
    /// DTO pour les requêtes SignalR
    /// </summary>
    public class NotificationSignalRRequest
    {
        [Required(ErrorMessage = "Le titre de la notification est requis.")]
        [MaxLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères.")]
        public string Titre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le contenu de la notification est requis.")]
        [MaxLength(1000, ErrorMessage = "Le contenu ne peut pas dépasser 1000 caractères.")]
        public string Contenu { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Le type de notification ne peut pas dépasser 50 caractères.")]
        public string TypeNotification { get; set; } = "INFO";

        public string? LienAction { get; set; }

        public string? Icone { get; set; }

        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// DTO pour l'enregistrement d'un appareil utilisateur
    /// </summary>
    public class RegisterDeviceRequest
    {
        [Required(ErrorMessage = "L'ID de l'utilisateur est requis.")]
        public int IdUtilisateur { get; set; }

        [Required(ErrorMessage = "Le token FCM est requis.")]
        [MaxLength(500, ErrorMessage = "Le token FCM ne peut pas dépasser 500 caractères.")]
        public string FcmToken { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Le type d'appareil ne peut pas dépasser 100 caractères.")]
        public string? DeviceType { get; set; }

        [MaxLength(100, ErrorMessage = "Le modèle d'appareil ne peut pas dépasser 100 caractères.")]
        public string? DeviceModel { get; set; }

        [MaxLength(50, ErrorMessage = "La version OS ne peut pas dépasser 50 caractères.")]
        public string? OsVersion { get; set; }
    }

    /// <summary>
    /// DTO pour les notifications avancées
    /// </summary>
    public class NotificationAvanceeRequest
    {
        [Required(ErrorMessage = "Le token FCM est requis.")]
        [MaxLength(500, ErrorMessage = "Le token FCM ne peut pas dépasser 500 caractères.")]
        public string FcmToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le titre de la notification est requis.")]
        [MaxLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères.")]
        public string Titre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le corps de la notification est requis.")]
        [MaxLength(1000, ErrorMessage = "Le corps ne peut pas dépasser 1000 caractères.")]
        public string Corps { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "L'URL de l'image ne peut pas dépasser 500 caractères.")]
        public string? ImageUrl { get; set; }

        [MaxLength(200, ErrorMessage = "L'action de clic ne peut pas dépasser 200 caractères.")]
        public string? ClickAction { get; set; }

        public Dictionary<string, string>? Donnees { get; set; }

        [MaxLength(50, ErrorMessage = "Le son ne peut pas dépasser 50 caractères.")]
        public string? Sound { get; set; }

        [MaxLength(10, ErrorMessage = "Le badge ne peut pas dépasser 10 caractères.")]
        public string? Badge { get; set; }
    }
}