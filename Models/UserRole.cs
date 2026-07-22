using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Table de liaison entre Utilisateur et Role (relation N-N)
    /// Permet à un utilisateur d'avoir plusieurs rôles simultanément
    /// </summary>
    public class UserRole
    {
        [Key]
        public int IdUserRole { get; set; }

        /// <summary>
        /// ID de l'utilisateur
        /// </summary>
        [Required]
        public int IdUtilisateur { get; set; }

        /// <summary>
        /// ID du rôle
        /// </summary>
        [Required]
        public int IdRole { get; set; }

        /// <summary>
        /// Indique si ce rôle est le rôle principal de l'utilisateur
        /// Un utilisateur ne peut avoir qu'un seul rôle principal à la fois
        /// </summary>
        public bool IsPrimary { get; set; } = false;

        /// <summary>
        /// Date d'attribution du rôle à l'utilisateur
        /// </summary>
        public DateTime DateAttribution { get; set; } = DateTime.Now;

        /// <summary>
        /// ID de l'utilisateur qui a attribué ce rôle (pour audit trail)
        /// </summary>
        public int? IdUtilisateurAttribution { get; set; }

        /// <summary>
        /// Statut du rôle (actif/inactif)
        /// Permet de désactiver un rôle sans le supprimer (soft delete)
        /// </summary>
        public bool? Statut { get; set; } = true;

        // ═══════════════════════════════════════════════════════════════════
        // RELATIONS (Navigation Properties)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Utilisateur concerné
        /// </summary>
        [ForeignKey(nameof(IdUtilisateur))]
        [JsonIgnore]
        [ValidateNever]
        public Utilisateur Utilisateur { get; set; } = null!;

        /// <summary>
        /// Rôle concerné
        /// </summary>
        [ForeignKey(nameof(IdRole))]
        [JsonIgnore]
        [ValidateNever]
        public Role Role { get; set; } = null!;
    }
}

