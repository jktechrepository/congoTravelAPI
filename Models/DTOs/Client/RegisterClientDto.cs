using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Client
{
    /// <summary>
    /// DTO pour l'enregistrement public d'un client
    /// Optimisé pour l'auto-inscription avec validations spécifiques
    /// </summary>
    public class RegisterClientDto
    {
        /// <summary>
        /// Nom complet du client
        /// </summary>
        [Required(ErrorMessage = "Le nom est obligatoire")]
        [MinLength(2, ErrorMessage = "Le nom doit contenir au moins 2 caractères")]
        [MaxLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
        [RegularExpression(@"^[a-zA-ZàâäéèêëïîôöùûüÿçÀÂÄÉÈÊËÏÎÔÖÙÛÜŸÇ\s'-]+$", 
            ErrorMessage = "Le nom contient des caractères non valides")]
        public string NomClient { get; set; } = string.Empty;

        
        /// <summary>Email optionnel ; si renseigné, doit être unique et au format valide.</summary>
        [MaxLength(256, ErrorMessage = "L'email ne peut pas dépasser 256 caractères")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", 
            ErrorMessage = "Le format de l'email est invalide")]
        public string? EmailClient { get; set; }

        /// <summary>
        /// Téléphone du client (obligatoire pour contact)
        /// </summary>
        [Required(ErrorMessage = "Le téléphone est obligatoire")]
        [Phone(ErrorMessage = "Le format du téléphone est invalide")]
        [MaxLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]{8,20}$", 
            ErrorMessage = "Le format du téléphone est invalide")]
        public string Telephone { get; set; } = string.Empty;

        /// <summary>
        /// Adresse complète du client (optionnelle)
        /// </summary>
        [MaxLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
        public string? AdresseClient { get; set; }

        /// <summary>
        /// Genre du client (M, F, Autre)
        /// </summary>
        [MaxLength(10, ErrorMessage = "Le genre ne peut pas dépasser 10 caractères")]
        [RegularExpression(@"^(M|F|Autre)$", ErrorMessage = "Le genre doit être M, F ou Autre")]
        public string? GenreClient { get; set; }

        /// <summary>
        /// Province du client
        /// </summary>
        [MaxLength(100, ErrorMessage = "La province ne peut pas dépasser 100 caractères")]
        [RegularExpression(@"^[a-zA-ZàâäéèêëïîôöùûüÿçÀÂÄÉÈÊËÏÎÔÖÙÛÜŸÇ\s'-]+$", 
            ErrorMessage = "La province contient des caractères non valides")]
        public string? Province { get; set; }

        /// <summary>
        /// Ville du client
        /// </summary>
        [MaxLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
        [RegularExpression(@"^[a-zA-ZàâäéèêëïîôöùûüÿçÀÂÄÉÈÊËÏÎÔÖÙÛÜŸÇ\s'-]+$", 
            ErrorMessage = "La ville contient des caractères non valides")]
        public string? Ville { get; set; }

        /// <summary>
        /// Commune du client
        /// </summary>
        [MaxLength(100, ErrorMessage = "La commune ne peut pas dépasser 100 caractères")]
        [RegularExpression(@"^[a-zA-ZàâäéèêëïîôöùûüÿçÀÂÄÉÈÊËÏÎÔÖÙÛÜŸÇ\s'-]+$", 
            ErrorMessage = "La commune contient des caractères non valides")]
        public string? Commune { get; set; }

        /// <summary>
        /// Avenue du client
        /// </summary>
        [MaxLength(200, ErrorMessage = "L'avenue ne peut pas dépasser 200 caractères")]
        public string? Avenue { get; set; }

        /// <summary>
        /// Numéro de l'adresse du client
        /// </summary>
        [MaxLength(50, ErrorMessage = "Le numéro ne peut pas dépasser 50 caractères")]
        [RegularExpression(@"^[0-9A-Za-z\s\-\/]+$", 
            ErrorMessage = "Le numéro contient des caractères non valides")]
        public string? Numero { get; set; }

        /// <summary>
        /// Terms and conditions acceptance
        /// </summary>
        [Required(ErrorMessage = "L'acceptation des conditions est obligatoire")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Vous devez accepter les conditions d'utilisation")]
        public bool AcceptTerms { get; set; }

        /// <summary>
        /// Newsletter subscription (optional)
        /// </summary>
        public bool SubscribeNewsletter { get; set; } = false;

        /// <summary>
        /// Marketing consent (optional)
        /// </summary>
        public bool MarketingConsent { get; set; } = false;
    }

    /// <summary>
    /// DTO pour la réponse après inscription
    /// </summary>
    public class ClientRegistrationResponseDto
    {
        public int IdClient { get; set; }
        public string NomClient { get; set; } = string.Empty;
        public string? EmailClient { get; set; }
        public string Telephone { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public bool IsActif { get; set; }
        public bool Statut { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? WelcomeMessage { get; set; }

        /// <summary>True si un email a été fourni et doit être confirmé via le lien reçu.</summary>
        public bool EmailVerificationRequired { get; set; }

        /// <summary>True si un email de vérification a bien été émis (SMTP OK).</summary>
        public bool EmailVerificationSent { get; set; }
    }

    /// <summary>Body pour confirmer un email via le token du lien.</summary>
    public class VerifyEmailRequestDto
    {
        [Required(ErrorMessage = "Le token est obligatoire")]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>Body pour renvoyer un email de vérification.</summary>
    public class ResendEmailVerificationRequestDto
    {
        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "L'email doit être valide")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour la validation d'email unique
    /// </summary>
    public class CheckEmailAvailabilityDto
    {
        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "L'email doit être valide")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour la réponse de disponibilité email
    /// </summary>
    public class EmailAvailabilityResponseDto
    {
        public string Email { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
