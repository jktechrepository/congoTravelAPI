using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// DTO pour la création d'un nouvel utilisateur (Admin uniquement)
    /// </summary>
    public class CreateUtilisateurDto
    {
        // ═══════════════════════════════════════════════════════════
        // INFORMATIONS PERSONNELLES (Obligatoires)
        // ═══════════════════════════════════════════════════════════
        
        [Required(ErrorMessage = "Le nom complet est obligatoire")]
        [StringLength(200, ErrorMessage = "Le nom complet ne peut pas dépasser 200 caractères")]
        public string NomComplet { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(256, ErrorMessage = "L'email ne peut pas dépasser 256 caractères")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir entre 6 et 100 caractères")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$", 
            ErrorMessage = "Le mot de passe doit contenir au moins 1 majuscule, 1 minuscule, 1 chiffre et 1 caractère spécial")]
        public string MotDePasse { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        public string? Telephone { get; set; }
        
        // ═══════════════════════════════════════════════════════════
        // INFORMATIONS COMPLÉMENTAIRES (Optionnelles)
        // ═══════════════════════════════════════════════════════════
        
        //[Url(ErrorMessage = "Format d'URL invalide")]
       // [StringLength(500, ErrorMessage = "L'URL de la photo ne peut pas dépasser 500 caractères")]
        public string? PhotoUrl { get; set; }
        
        [StringLength(100, ErrorMessage = "Le lieu de naissance ne peut pas dépasser 100 caractères")]
        public string? LieuNaissance { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? DateNaissance { get; set; }
        
        [RegularExpression("^(M|F|Autre)$", ErrorMessage = "Le genre doit être 'M', 'F' ou 'Autre'")]
        public string? Genre { get; set; }
        
        // ═══════════════════════════════════════════════════════════
        // INFORMATIONS ADMINISTRATIVES (Gérées par l'Admin)
        // ═══════════════════════════════════════════════════════════
        
        [Required(ErrorMessage = "Le rôle est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du rôle doit être valide")]
        public int IdRole { get; set; }
        
        [Required(ErrorMessage = "La société est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")]
        public int IdSociete { get; set; }

        /// <summary>Site d’affectation (optionnel, doit appartenir à <see cref="IdSociete"/>).</summary>
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du site doit être valide")]
        public int? IdSite { get; set; }
        
        public bool? Statut { get; set; } = true; // Actif par défaut
        
        // ═══════════════════════════════════════════════════════════
        // CHAMPS AUTO-GÉNÉRÉS (Non inclus - Gérés par le système)
        // ═══════════════════════════════════════════════════════════
        // ✅ ReferenceUtilisateur  → Généré automatiquement (Guid)
        // ✅ DefaultUsername       → Généré automatiquement (prenom.nom)
        // ✅ DateCreation          → DateTime.UtcNow
        // ✅ IsConnecte            → false par défaut
        // ✅ FcmToken              → null (ajouté à la connexion)
    }
}

