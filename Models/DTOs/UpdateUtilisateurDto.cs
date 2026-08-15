using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// DTO pour la modification des informations personnelles d'un utilisateur
    /// Contient uniquement les champs modifiables par l'utilisateur lui-même
    /// </summary>
    public class UpdateUtilisateurDto
    {
        [Required(ErrorMessage = "L'ID utilisateur est obligatoire")]
        public int IdUtilisateur { get; set; }
        
        // ═══════════════════════════════════════════════════════════
        // INFORMATIONS PERSONNELLES (Modifiables par l'utilisateur)
        // ═══════════════════════════════════════════════════════════
        
        [Required(ErrorMessage = "Le nom complet est obligatoire")]
        [StringLength(200, ErrorMessage = "Le nom complet ne peut pas dépasser 200 caractères")]
        public string? NomComplet { get; set; }
        
        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(256, ErrorMessage = "L'email ne peut pas dépasser 256 caractères")]
        public string? Email { get; set; }
        
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        public string? Telephone { get; set; }
        
       // [Url(ErrorMessage = "Format d'URL invalide")]
       // [StringLength(500, ErrorMessage = "L'URL de la photo ne peut pas dépasser 500 caractères")]
        public string? PhotoUrl { get; set; }
        
        [StringLength(100, ErrorMessage = "Le lieu de naissance ne peut pas dépasser 100 caractères")]
        public string? LieuNaissance { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? DateNaissance { get; set; }
        
        [RegularExpression("^(M|F|Autre)$", ErrorMessage = "Le genre doit être 'M', 'F' ou 'Autre'")]
        public string? Genre { get; set; }
        
        // ═══════════════════════════════════════════════════════════
        // CHAMPS SENSIBLES (NON INCLUS - Protection de sécurité)
        // ═══════════════════════════════════════════════════════════
        // ❌ MotDePasseHash         → Utiliser POST /api/Utilisateur/changer_mot_de_passe
        // ❌ IdRole                 → Réservé aux admins (voir UpdateUtilisateurAdminDto)
        // ❌ IdSociete              → Réservé aux Super-Admins uniquement
        // ❌ Statut                 → Réservé aux admins (voir UpdateUtilisateurAdminDto)
        // ❌ IdAgent                → Géré automatiquement par le système
        // ❌ ReferenceUtilisateur   → Immuable (identifiant unique)
        // ❌ DateCreation           → Immuable (audit)
        // ❌ IsConnecte             → Géré automatiquement
        // ❌ FcmToken               → Géré via authentification
    }
}

