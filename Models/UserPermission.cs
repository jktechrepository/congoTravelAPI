using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Permissions personnalisées par utilisateur (optionnel)
    /// Permet d'ajouter ou retirer des permissions individuelles en plus de celles du rôle
    /// </summary>
    public class UserPermission
    {
        [Key]
        public int IdUserPermission { get; set; }

        /// <summary>
        /// ID de l'utilisateur
        /// </summary>
        [Required]
        public int IdUtilisateur { get; set; }

        /// <summary>
        /// ID de la permission
        /// </summary>
        [Required]
        public int IdPermission { get; set; }

        /// <summary>
        /// true = Permission ajoutée (GrantPermission)
        /// false = Permission retirée (DenyPermission)
        /// 
        /// Exemples :
        /// - IsGranted = true : Ajouter "Paiement.Validate" à un Gerant (qui ne l'a pas normalement)
        /// - IsGranted = false : Retirer "Eleve.Delete" à un Admin spécifique
        /// </summary>
        [Required]
        public bool IsGranted { get; set; }

        /// <summary>
        /// Date d'attribution de cette permission personnalisée
        /// </summary>
        public DateTime DateAttribution { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date d'expiration (optionnel) pour permissions temporaires
        /// </summary>
        public DateTime? DateExpiration { get; set; }

        /// <summary>
        /// Commentaire/Raison de cette permission personnalisée
        /// Ex: "Promotion temporaire", "Besoin spécifique", etc.
        /// </summary>
        [MaxLength(500)]
        public string? Commentaire { get; set; }

        /// <summary>
        /// Qui a attribué cette permission personnalisée
        /// </summary>
        public int? AttribueParIdUtilisateur { get; set; }

        // Navigation properties
        [JsonIgnore]
        [ForeignKey(nameof(IdUtilisateur))]
        public Utilisateur? Utilisateur { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(IdPermission))]
        public Permission? Permission { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(AttribueParIdUtilisateur))]
        public Utilisateur? AttribuePar { get; set; }

        /// <summary>
        /// Vérifie si la permission personnalisée est encore valide (non expirée)
        /// </summary>
        public bool IsValid()
        {
            if (DateExpiration == null)
                return true;

            return DateTime.UtcNow <= DateExpiration.Value;
        }
    }
}

