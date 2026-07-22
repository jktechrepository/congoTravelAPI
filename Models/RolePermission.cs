using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Table de liaison entre Roles et Permissions (relation N-N)
    /// Permet d'assigner plusieurs permissions à un rôle et vice-versa
    /// </summary>
    public class RolePermission
    {
        [Key]
        public int IdRolePermission { get; set; }

        /// <summary>
        /// ID du rôle
        /// </summary>
        [Required]
        public int IdRole { get; set; }

        /// <summary>
        /// ID de la permission
        /// </summary>
        [Required]
        public int IdPermission { get; set; }

        /// <summary>
        /// Date d'attribution de la permission au rôle
        /// </summary>
        public DateTime DateAttribution { get; set; } = DateTime.Now;

        /// <summary>
        /// ID de l'utilisateur qui a attribué cette permission (optionnel, pour audit)
        /// </summary>
        public int? IdUtilisateurAttribution { get; set; }

        // ═══════════════════════════════════════════════════════════════════
        // RELATIONS (Navigation Properties)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Rôle concerné
        /// </summary>
        [ForeignKey(nameof(IdRole))]
        [JsonIgnore]
        [ValidateNever]
        public Role Role { get; set; } = null!;

        /// <summary>
        /// Permission concernée
        /// </summary>
        [ForeignKey(nameof(IdPermission))]
        [JsonIgnore]
        [ValidateNever]
        public Permission Permission { get; set; } = null!;
    }
}

