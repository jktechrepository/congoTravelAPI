using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Représente une permission dans le système RBAC
    /// Une permission définit une action spécifique qu'un rôle peut effectuer
    /// Format: "Categorie.Action" (ex: "Societe.Create", "Paiement.Delete")
    /// </summary>
    public class Permission
    {
        [Key]
        public int IdPermission { get; set; }

        /// <summary>
        /// Nom unique de la permission (ex: "Societe.Create", "Paiement.Validate")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        /// <summary>
        /// Description détaillée de ce que permet cette permission
        /// </summary>
        [MaxLength(255)]
        public string? Description { get; set; }

        /// <summary>
        /// Catégorie/Ressource concernée (ex: "Societe", "Paiement", "Eleve", "Note")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Categorie { get; set; } = string.Empty;

        /// <summary>
        /// Action autorisée (ex: "Create", "Read", "Update", "Delete", "Validate", "ReadAll", "ReadOwn")
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Indique si cette permission est active
        /// </summary>
        public bool? Statut { get; set; } = true;

        /// <summary>
        /// Date de création de la permission
        /// </summary>
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // ═══════════════════════════════════════════════════════════════════
        // RELATIONS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Rôles ayant cette permission (relation N-N via RolePermission)
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}

