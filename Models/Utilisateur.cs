using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CongoTravel.Models
{
    public class Utilisateur
    {
        [Key]
        public int IdUtilisateur { get; set; }
        public Guid? ReferenceUtilisateur { get; set; }
        [Required]
        [MaxLength(200)]
        public string? NomComplet { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? Telephone { get; set; }
       // [ValidateNever]
       // public IFormFile? Image { get; set; }
        public string? PhotoUrl { get; set; }
        public string? LieuNaissance { get; set; }
        public DateTime? DateNaissance { get; set; }
        public string? Genre { get; set; }

        [Required]
        public string? MotDePasseHash { get; set; }
        public string? DefaultUsername { get; set; } // Nom d'utilisateur par défaut généré automatiquement
        public bool DoitChangerMotDePasse { get; set; } = false; // Force le changement de mot de passe à la première connexion
        public bool? Statut { get; set; } = true;
        public int? IdRole { get; set; } // ✅ Rendu nullable pour le système multi-rôles (rétrocompatibilité)
        public int? IdSociete { get; set; }
        
        /// <summary>
        /// Adresse de résidence de l'utilisateur
        /// </summary>
        [MaxLength(500)]
        public string? AdresseResidence { get; set; }

        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;
        [JsonIgnore]
        public bool IsConnecte { get; set; } = false; // Par defaut, l'utilisateur n'est pas connecte
        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Role? Role { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<Notification>? NotificationsEnvoyees { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<Notification>? NotificationsRecues { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<PasswordResetToken>? PasswordResetTokens { get; set; }

        // Relations avec Agent (nullable)
        public int? IdAgent { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdAgent")]
        public Agent? Agent { get; set; }

        // Relations avec Client (nullable)
        public int? IdClient { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdClient")]
        public Client? Client { get; set; }

        /// <summary>Provider d'auth externe (ex. <c>Google</c>) ; null = compte local.</summary>
        [MaxLength(32)]
        public string? AuthProvider { get; set; }

        /// <summary>Identifiant stable du provider (Google <c>sub</c>).</summary>
        [MaxLength(128)]
        public string? ExternalSubjectId { get; set; }

        /// <summary>Email vérifié par le provider OAuth (claim Google).</summary>
        public bool? EmailVerified { get; set; }

        /// <summary>Site d’affectation opérationnelle (optionnel, même société).</summary>
        public int? IdSite { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdSite")]
        public Site? Site { get; set; }

        // ═══════════════════════════════════════════════════════════════════
        // ✅ MULTI-RÔLES : Relation N-N avec Role via UserRole
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Collection des rôles de l'utilisateur (relation N-N)
        /// Permet à un utilisateur d'avoir plusieurs rôles simultanément
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        /// <summary>
        /// Propriété calculée : Rôles actifs de l'utilisateur
        /// </summary>
        [NotMapped]
        public IEnumerable<Role> Roles => UserRoles
            .Where(ur => ur.Statut == true)
            .Select(ur => ur.Role);

        /// <summary>
        /// Propriété calculée : Rôle principal de l'utilisateur
        /// Retourne le rôle marqué comme principal, ou le rôle avec le niveau le plus élevé
        /// </summary>
        [NotMapped]
        public Role? PrimaryRole => UserRoles
            .Where(ur => ur.Statut == true && ur.IsPrimary)
            .Select(ur => ur.Role)
            .FirstOrDefault() 
            ?? UserRoles
                .Where(ur => ur.Statut == true)
                .OrderBy(ur => ur.Role.Niveau ?? 999)
                .Select(ur => ur.Role)
                .FirstOrDefault();

    }
}
