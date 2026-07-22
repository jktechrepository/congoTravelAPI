using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// DTO pour la modification des informations d'un utilisateur par un Admin
    /// Hérite de UpdateUtilisateurDto et ajoute des champs administratifs
    /// </summary>
    public class UpdateUtilisateurAdminDto : UpdateUtilisateurDto
    {
        // ═══════════════════════════════════════════════════════════
        // CHAMPS SUPPLÉMENTAIRES (Réservés aux Admins)
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Modification du rôle (Admin uniquement)
        /// Un Admin ne peut pas créer un Super-Admin (sauf si lui-même est Super-Admin)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du rôle doit être valide")]
        public int? IdRole { get; set; }
        
        /// <summary>
        /// Activation/Désactivation du compte (Admin uniquement)
        /// </summary>
        public bool? Statut { get; set; }

        /// <summary>Assigner un site (doit appartenir à la société de l'utilisateur).</summary>
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du site doit être valide")]
        public int? IdSite { get; set; }

        /// <summary>Si true, retire l'affectation site (<see cref="IdSite"/> ignoré).</summary>
        public bool DesassocierSite { get; set; }
        
        // ═══════════════════════════════════════════════════════════
        // CHAMPS TOUJOURS PROTÉGÉS (Même pour Admin)
        // ═══════════════════════════════════════════════════════════
        // ❌ IdSociete              → Réservé aux Super-Admins uniquement
        //                             (Un Admin ne peut pas transférer un user vers une autre école)
        // ❌ MotDePasseHash         → Utiliser endpoint dédié
        // ❌ ReferenceUtilisateur   → Immuable
        // ❌ DateCreation           → Immuable (audit)
    }
}

