using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Modèle pour stocker les refresh tokens JWT
    /// Permet de rafraîchir l'access token sans ré-authentification
    /// </summary>
    public class RefreshToken
    {
        [Key]
        public int IdRefreshToken { get; set; }

        /// <summary>
        /// ID de l'utilisateur propriétaire du token
        /// </summary>
        [Required]
        [ForeignKey(nameof(Utilisateur))]
        public int IdUtilisateur { get; set; }

        /// <summary>
        /// Token hashé (pour sécurité, on ne stocke jamais le token en clair)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// Date de création du token
        /// </summary>
        [Required]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date d'expiration du token
        /// </summary>
        [Required]
        public DateTime DateExpiration { get; set; }

        /// <summary>
        /// Date de révocation (si le token a été révoqué)
        /// </summary>
        public DateTime? DateRevocation { get; set; }

        /// <summary>
        /// Indique si le token a été révoqué
        /// </summary>
        public bool EstRevoke => DateRevocation.HasValue;

        /// <summary>
        /// Indique si le token est expiré
        /// </summary>
        public bool EstExpire => DateTime.UtcNow > DateExpiration;

        /// <summary>
        /// Indique si le token est actif (non révoqué et non expiré)
        /// </summary>
        public bool EstActif => !EstRevoke && !EstExpire;

        /// <summary>
        /// Informations sur l'appareil qui a créé le token (optionnel)
        /// </summary>
        [MaxLength(200)]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Adresse IP qui a créé le token (optionnel, pour audit)
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        // ═══════════════════════════════════════════════════════════════════
        // RELATIONS (Navigation Properties)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Utilisateur propriétaire du token
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public Utilisateur Utilisateur { get; set; } = null!;
    }
}

