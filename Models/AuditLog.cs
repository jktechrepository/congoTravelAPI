using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongoTravel.Models
{
    /// <summary>
    /// Table d'audit pour tracer TOUTES les modifications dans le système
    /// Capture CREATE, UPDATE, DELETE pour toutes les entités critiques
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// ID unique de l'entrée d'audit (BIGINT pour supporter des millions d'enregistrements)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long IdAudit { get; set; }

        /// <summary>
        /// Nom de la table/entité modifiée (ex: "Paiement", "Note", "Inscription")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// ID de l'enregistrement modifié dans sa table
        /// </summary>
        [Required]
        public int RecordId { get; set; }

        /// <summary>
        /// Type d'action : CREATE, UPDATE, DELETE
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// ID de l'utilisateur qui a effectué l'action
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Nom complet de l'utilisateur (pour référence rapide sans JOIN)
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Rôle de l'utilisateur au moment de l'action
        /// </summary>
        [MaxLength(50)]
        public string? UserRole { get; set; }

        /// <summary>
        /// Société de l'utilisateur (pour filtrage par société)
        /// </summary>
        public int? IdSociete { get; set; }

        /// <summary>
        /// Date et heure exactes de l'action
        /// </summary>
        [Required]
        public DateTime DateAction { get; set; } = DateTime.Now;

        /// <summary>
        /// Valeurs AVANT modification (JSON)
        /// NULL pour les CREATE
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? OldValues { get; set; }

        /// <summary>
        /// Valeurs APRÈS modification (JSON)
        /// NULL pour les DELETE
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? NewValues { get; set; }

        /// <summary>
        /// Liste des champs modifiés séparés par virgule
        /// Ex: "Montant,StatutPaiement,Commentaire"
        /// </summary>
        [MaxLength(500)]
        public string? ChangedFields { get; set; }

        /// <summary>
        /// Adresse IP du client
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User-Agent (navigateur ou app mobile)
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Commentaire ou raison de la modification (optionnel)
        /// Peut être fourni par l'utilisateur
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? Commentaire { get; set; }

        /// <summary>
        /// Méthode HTTP utilisée (POST, PUT, DELETE)
        /// </summary>
        [MaxLength(10)]
        public string? HttpMethod { get; set; }

        /// <summary>
        /// Endpoint appelé (ex: "/api/Paiement/123")
        /// </summary>
        [MaxLength(500)]
        public string? Endpoint { get; set; }

        /// <summary>
        /// Durée de l'opération en millisecondes
        /// </summary>
        public int? DurationMs { get; set; }

        /// <summary>
        /// Succès ou échec de l'opération
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Message d'erreur si l'opération a échoué
        /// </summary>
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        // ============================================
        // INDEXES (définis dans OnModelCreating)
        // ============================================
        // - Index sur (TableName, RecordId) → Recherche par entité
        // - Index sur UserId → Recherche par utilisateur
        // - Index sur DateAction → Recherche temporelle
        // - Index sur IdSociete → Filtrage par société
    }
}

