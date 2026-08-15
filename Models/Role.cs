using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Représente un rôle dans le système RBAC
    /// Un rôle définit un ensemble de permissions pour un groupe d'utilisateurs
    /// </summary>
    public class Role
    {
        [Key]
        public int IdRole { get; set; }

        /// <summary>
        /// Nom du rôle (ex: "Super-Admin", "Gerant", "Caissier", "Technicien")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Nom { get; set; } = string.Empty;

        /// <summary>
        /// Description détaillée du rôle et de ses responsabilités
        /// </summary>
        [MaxLength(255)]
        public string? Description { get; set; }

        /// <summary>
        /// Niveau hiérarchique du rôle (1 = Super-Admin, 10 = niveau le plus bas)
        /// Permet de gérer la hiérarchie et l'héritage de permissions
        /// Plus le niveau est bas, plus le rôle a de pouvoir
        /// </summary>
        public int? Niveau { get; set; } = 5;

        /// <summary>
        /// Indique si ce rôle est actif
        /// </summary>
        public bool? Statut { get; set; } = true;

        /// <summary>
        /// Date de création du rôle
        /// </summary>
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // ═══════════════════════════════════════════════════════════════════
        // RELATIONS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Utilisateurs ayant ce rôle
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Utilisateur> Utilisateurs { get; set; } = new List<Utilisateur>();

        /// <summary>
        /// Permissions associées à ce rôle (relation N-N via RolePermission)
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
