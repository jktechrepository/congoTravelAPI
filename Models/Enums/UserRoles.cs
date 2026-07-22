namespace CongoTravel.Models.Enums
{
    /// <summary>
    /// Définit tous les rôles utilisateur du système avec leurs niveaux hiérarchiques
    /// Facilite l'utilisation des rôles avec IntelliSense et évite les erreurs de frappe
    /// </summary>
    public static class UserRoles
    {
        // ═══════════════════════════════════════════════════════════════════
        // 🔴 NIVEAU 1 : SYSTÈME (Accès complet à tout)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Super-Administrateur système - Accès complet à toutes les écoles et fonctionnalités
        /// </summary>
        public const string SUPER_ADMIN = "Super-Admin";

        // ═══════════════════════════════════════════════════════════════════
        // 🟠 NIVEAU 2 : DIRECTION ÉCOLE (Gestion complète d'une école)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gérant d'école - Gestion complète de son école
        /// </summary>
        public const string GERANT = "Gerant";

        /// <summary>
        /// Sous-Directeur - Assiste le directeur avec permissions réduites
        /// </summary>
        public const string SOUS_DIRECTEUR = "Sous-Directeur";

        // ═══════════════════════════════════════════════════════════════════
        // 🟡 NIVEAU 3 : ADMINISTRATION (Gestion administrative)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Secrétaire - Gestion des inscriptions, élèves, documents
        /// </summary>
        public const string SECRETAIRE = "Secrétaire";

        /// <summary>
        /// Administrateur d'école - Gestion opérationnelle globale
        /// </summary>
        public const string ADMIN = "Admin";

        /// <summary>
        /// Financier - Gestion financière, paiements, frais
        /// </summary>
        public const string FINANCIER = "Financier";

        // ═══════════════════════════════════════════════════════════════════
        // 🟢 NIVEAU 4 : PÉDAGOGIE (Enseignement et éducation)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Caissier - Gestion des paiements et transactions
        /// </summary>
        public const string CAISSIER = "Caissier";

        /// <summary>
        /// Préfet - Discipline, gestion des présences
        /// </summary>
        public const string PREFET = "Préfet";

        // ═══════════════════════════════════════════════════════════════════
        // 🔵 NIVEAU 5 : UTILISATEURS EXTERNES (Consultation)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Technicien - Maintenance et support technique
        /// </summary>
        public const string TECHNICIEN = "Technicien";


        /// <summary>
        /// Bailleur/Sponsor - Consultation des rapports financiers
        /// </summary>
        public const string BAILLEUR = "Bailleur";

        /// <summary>
        /// Client - Passager / réservations et paiements personnels
        /// </summary>
        public const string CLIENT = "Client";

        // ═══════════════════════════════════════════════════════════════════
        // 🟣 NIVEAU 6 : SUPPORT (Maintenance et support)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Agent de support - Assistance technique
        /// </summary>
        public const string AGENT_SUPPORT = "Agent Support";

        /// <summary>
        /// Autre personnel - Rôle générique pour personnel non catégorisé
        /// </summary>
        public const string AUTRE_PERSONNEL = "Autre Personnel";

        // ═══════════════════════════════════════════════════════════════════
        // GROUPES DE RÔLES (pour faciliter les vérifications)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tous les rôles ayant des droits d'administration
        /// </summary>
        public static string[] AdminRoles => new[]
        {
            SUPER_ADMIN,
            ADMIN,
            GERANT,
            SOUS_DIRECTEUR
        };

        /// <summary>
        /// Tous les rôles du personnel de l'école
        /// </summary>
        public static string[] StaffRoles => new[]
        {
            SUPER_ADMIN,
            GERANT,
            SOUS_DIRECTEUR,
            SECRETAIRE,
            FINANCIER,
            CAISSIER,
            PREFET
        };

        /// <summary>
        /// Rôles ayant accès à la gestion financière
        /// </summary>
        public static string[] FinanceRoles => new[]
        {
            SUPER_ADMIN,
            GERANT,
            FINANCIER
        };

        /// <summary>
        /// Rôles ayant accès à la gestion pédagogique
        /// </summary>
        public static string[] PedagogieRoles => new[]
        {
            SUPER_ADMIN,
            GERANT,
            SOUS_DIRECTEUR,
            CAISSIER,
            PREFET
        };

        /// <summary>
        /// Rôles ayant accès à la gestion des élèves
        /// </summary>
        public static string[] GestionElevesRoles => new[]
        {
            SUPER_ADMIN,
            GERANT,
            SOUS_DIRECTEUR,
            SECRETAIRE
        };

        /// <summary>
        /// Rôles externes (non-personnel)
        /// </summary>
        public static string[] ExternalRoles => new[]
        {
            CLIENT,
            TECHNICIEN,
            BAILLEUR
        };

        // ═══════════════════════════════════════════════════════════════════
        // MÉTHODES UTILITAIRES
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Vérifie si un rôle est un rôle d'administration
        /// </summary>
        public static bool IsAdminRole(string role)
        {
            return AdminRoles.Contains(role);
        }

        /// <summary>
        /// Vérifie si un rôle est un rôle de personnel
        /// </summary>
        public static bool IsStaffRole(string role)
        {
            return StaffRoles.Contains(role);
        }

        /// <summary>
        /// Vérifie si un rôle a accès à la gestion financière
        /// </summary>
        public static bool HasFinanceAccess(string role)
        {
            return FinanceRoles.Contains(role);
        }

        /// <summary>
        /// Vérifie si un rôle a accès à la gestion pédagogique
        /// </summary>
        public static bool HasPedagogieAccess(string role)
        {
            return PedagogieRoles.Contains(role);
        }

        /// <summary>
        /// Vérifie si un rôle peut gérer les élèves
        /// </summary>
        public static bool CanManageEleves(string role)
        {
            return GestionElevesRoles.Contains(role);
        }

        /// <summary>
        /// Vérifie si un rôle est un rôle externe (non-personnel)
        /// </summary>
        public static bool IsExternalRole(string role)
        {
            return ExternalRoles.Contains(role);
        }

        /// <summary>
        /// Retourne tous les rôles disponibles
        /// </summary>
        public static string[] GetAllRoles()
        {
            return new[]
            {
                SUPER_ADMIN,
                GERANT,
                SOUS_DIRECTEUR,
                SECRETAIRE,
                FINANCIER,
                CAISSIER,
                PREFET,
                TECHNICIEN,
                BAILLEUR,
                CLIENT,
                AGENT_SUPPORT,
                AUTRE_PERSONNEL
            };
        }

        /// <summary>
        /// Retourne le niveau hiérarchique d'un rôle (1 = plus haut niveau)
        /// </summary>
        public static int GetRoleLevel(string role)
        {
            return role switch
            {
                SUPER_ADMIN => 1,
                GERANT => 2,
                SOUS_DIRECTEUR => 2,
                SECRETAIRE => 3,
                FINANCIER => 3,
                CAISSIER => 4,
                PREFET => 4,
                TECHNICIEN => 5,
                BAILLEUR => 5,
                CLIENT => 5,
                AGENT_SUPPORT => 4,
                AUTRE_PERSONNEL => 5,
                _ => 10 // Rôle inconnu = niveau le plus bas
            };
        }

        /// <summary>
        /// Vérifie si le premier rôle a un niveau supérieur au second
        /// </summary>
        public static bool IsHigherLevel(string role1, string role2)
        {
            return GetRoleLevel(role1) < GetRoleLevel(role2);
        }
    }
}

