-- ============================================================================
-- Script de vérification et correction des assignations de permissions
-- Base de données: MariaDB / MySQL
-- Version: 1.0
-- Date: Décembre 2025
-- ============================================================================
-- 
-- INSTRUCTIONS:
-- 1. Assurez-vous d'avoir exécuté initialize_default_data.sql et initialize_permissions.sql
-- 2. Utilisez la base de données: USE CongoTravelDB;
-- 3. Ce script vérifie et corrige les assignations de permissions manquantes
-- 
-- ⚠️  ATTENTION: Ce script peut être exécuté plusieurs fois sans erreur
--    (il vérifie l'existence avant d'insérer)
-- 
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

-- ============================================================================
-- 1. VÉRIFICATION DE L'ÉTAT ACTUEL
-- ============================================================================

SELECT '════════════════════════════════════════════════════════════' as Separator;
SELECT '📊 RAPPORT DE VÉRIFICATION DES PERMISSIONS' as Title;
SELECT '════════════════════════════════════════════════════════════' as Separator;

-- Nombre total de permissions
SELECT 
    '📋 Nombre total de permissions:' as Info,
    COUNT(*) as Total
FROM `Permissions`
WHERE `Statut` = 1;

-- Nombre de permissions par catégorie
SELECT 
    '📂 Permissions par catégorie:' as Info;
SELECT 
    `Categorie`,
    COUNT(*) as Nombre
FROM `Permissions`
WHERE `Statut` = 1
GROUP BY `Categorie`
ORDER BY `Categorie`;

-- Rôles existants
SELECT 
    '👥 Rôles existants:' as Info;
SELECT 
    `IdRole`,
    `Nom`,
    `Niveau`,
    `Statut`
FROM `Roles`
ORDER BY `Niveau`;

-- Assignations de permissions par rôle
SELECT 
    '🔗 Assignations de permissions par rôle:' as Info;
SELECT 
    r.`Nom` as Role,
    COUNT(rp.`IdRolePermission`) as Permissions_Assignees
FROM `Roles` r
LEFT JOIN `RolePermissions` rp ON r.`IdRole` = rp.`IdRole`
GROUP BY r.`IdRole`, r.`Nom`
ORDER BY r.`Niveau`;

-- ============================================================================
-- 2. VÉRIFICATION DES ASSIGNATIONS MANQUANTES
-- ============================================================================

SELECT '════════════════════════════════════════════════════════════' as Separator;
SELECT '🔍 VÉRIFICATION DES ASSIGNATIONS MANQUANTES' as Title;
SELECT '════════════════════════════════════════════════════════════' as Separator;

-- Récupérer l'ID du rôle Super-Admin
SET @super_admin_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Super-Admin' LIMIT 1);
SET @admin_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Admin' LIMIT 1);
SET @gerant_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Gerant' LIMIT 1);
SET @financier_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Financier' LIMIT 1);
SET @caissier_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Caissier' LIMIT 1);
SET @technicien_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Technicien' LIMIT 1);

-- Vérifier les permissions manquantes pour Super-Admin
SELECT 
    '🔴 Super-Admin - Permissions manquantes:' as Info,
    COUNT(*) as Nombre_Manquantes
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND NOT EXISTS (
      SELECT 1 
      FROM `RolePermissions` rp 
      WHERE rp.`IdRole` = @super_admin_role_id 
        AND rp.`IdPermission` = p.`IdPermission`
  )
  AND @super_admin_role_id IS NOT NULL;

-- ============================================================================
-- 3. CORRECTION : ASSIGNER TOUTES LES PERMISSIONS AU SUPER-ADMIN
-- ============================================================================

SELECT '════════════════════════════════════════════════════════════' as Separator;
SELECT '🔧 CORRECTION DES ASSIGNATIONS MANQUANTES' as Title;
SELECT '════════════════════════════════════════════════════════════' as Separator;

-- Super-Admin : TOUTES les permissions
INSERT IGNORE INTO `RolePermissions` (
    `IdRole`,
    `IdPermission`,
    `DateAttribution`
)
SELECT 
    @super_admin_role_id,
    p.`IdPermission`,
    NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @super_admin_role_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 
      FROM `RolePermissions` rp 
      WHERE rp.`IdRole` = @super_admin_role_id 
        AND rp.`IdPermission` = p.`IdPermission`
  );

SELECT CONCAT('✅ Super-Admin: ', ROW_COUNT(), ' permission(s) assignée(s)') as Resultat;

-- ============================================================================
-- 4. ASSIGNATION DES PERMISSIONS AUX AUTRES RÔLES
-- ============================================================================

-- Admin : Gestion complète de son école (sauf création/suppression d'écoles)
INSERT IGNORE INTO `RolePermissions` (
    `IdRole`,
    `IdPermission`,
    `DateAttribution`
)
SELECT 
    @admin_role_id,
    p.`IdPermission`,
    NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @admin_role_id IS NOT NULL
  AND (
      -- Écoles : Lecture et modification uniquement (pas création/suppression)
      (p.`Categorie` = 'Societe' AND p.`Action` IN ('Read', 'ReadAll', 'Update')) OR
      -- Gestion complète
      p.`Categorie` IN ('Utilisateur', 'Agent', 'Client', 'CategorieClient', 'Facture')
  )
  AND NOT EXISTS (
      SELECT 1 
      FROM `RolePermissions` rp 
      WHERE rp.`IdRole` = @admin_role_id 
        AND rp.`IdPermission` = p.`IdPermission`
  );

SELECT CONCAT('✅ Admin: ', ROW_COUNT(), ' permission(s) assignée(s)') as Resultat;

-- Gerant : Mêmes permissions que Admin, sauf modification/suppression de factures
INSERT IGNORE INTO `RolePermissions` (
    `IdRole`,
    `IdPermission`,
    `DateAttribution`
)
SELECT 
    @gerant_role_id,
    p.`IdPermission`,
    NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @gerant_role_id IS NOT NULL
  AND (
      -- Écoles : Lecture et modification uniquement
      (p.`Categorie` = 'Societe' AND p.`Action` IN ('Read', 'ReadAll', 'Update')) OR
      -- Gestion complète
      p.`Categorie` IN ('Utilisateur', 'Agent', 'Client', 'CategorieClient') OR
      -- Factures : Création et lecture uniquement (PAS modification ni suppression)
      (p.`Categorie` = 'Facture' AND p.`Action` NOT IN ('Update', 'Delete'))
  )
  AND NOT EXISTS (
      SELECT 1 
      FROM `RolePermissions` rp 
      WHERE rp.`IdRole` = @gerant_role_id 
        AND rp.`IdPermission` = p.`IdPermission`
  );

SELECT CONCAT('✅ Gerant: ', ROW_COUNT(), ' permission(s) assignée(s)') as Resultat;

-- Financier : Gestion financière (Factures en lecture/création uniquement)
INSERT IGNORE INTO `RolePermissions` (
    `IdRole`,
    `IdPermission`,
    `DateAttribution`
)
SELECT 
    @financier_role_id,
    p.`IdPermission`,
    NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @financier_role_id IS NOT NULL
  AND (
      -- Factures : Création et lecture uniquement (PAS modification ni suppression)
      (p.`Categorie` = 'Facture' AND p.`Action` NOT IN ('Update', 'Delete')) OR
      -- Clients : Lecture seule
      (p.`Categorie` = 'Client' AND p.`Action` IN ('Read', 'ReadAll')) OR
      -- Catégorie Clients : Lecture seule
      (p.`Categorie` = 'CategorieClient' AND p.`Action` IN ('Read', 'ReadAll'))
  )
  AND NOT EXISTS (
      SELECT 1 
      FROM `RolePermissions` rp 
      WHERE rp.`IdRole` = @financier_role_id 
        AND rp.`IdPermission` = p.`IdPermission`
  );

SELECT CONCAT('✅ Financier: ', ROW_COUNT(), ' permission(s) assignée(s)') as Resultat;

-- Caissier : Gestion des paiements et transactions
INSERT IGNORE INTO `RolePermissions` (
    `IdRole`,
    `IdPermission`,
    `DateAttribution`
)
SELECT 
    @caissier_role_id,
    p.`IdPermission`,
    NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @caissier_role_id IS NOT NULL
  AND (
      -- Factures : Lecture et modification (pour valider les paiements)
      (p.`Categorie` = 'Facture' AND p.`Action` IN ('Read', 'ReadAll', 'Update')) OR
      -- Clients : Lecture seule
      (p.`Categorie` = 'Client' AND p.`Action` IN ('Read', 'ReadAll')) OR
      -- Catégorie Clients : Lecture seule
      (p.`Categorie` = 'CategorieClient' AND p.`Action` IN ('Read', 'ReadAll'))
  )
  AND NOT EXISTS (
      SELECT 1 
      FROM `RolePermissions` rp 
      WHERE rp.`IdRole` = @caissier_role_id 
        AND rp.`IdPermission` = p.`IdPermission`
  );

SELECT CONCAT('✅ Caissier: ', ROW_COUNT(), ' permission(s) assignée(s)') as Resultat;

-- Technicien : Maintenance et support technique (lecture seule)
INSERT IGNORE INTO `RolePermissions` (
    `IdRole`,
    `IdPermission`,
    `DateAttribution`
)
SELECT 
    @technicien_role_id,
    p.`IdPermission`,
    NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @technicien_role_id IS NOT NULL
  AND (
      -- Lecture seule sur les équipements et systèmes
      (p.`Categorie` = 'Agent' AND p.`Action` IN ('Read', 'ReadAll')) OR
      (p.`Categorie` = 'Societe' AND p.`Action` IN ('Read', 'ReadAll')) OR
      (p.`Categorie` = 'Utilisateur' AND p.`Action` IN ('Read', 'ReadAll'))
  )
  AND NOT EXISTS (
      SELECT 1 
      FROM `RolePermissions` rp 
      WHERE rp.`IdRole` = @technicien_role_id 
        AND rp.`IdPermission` = p.`IdPermission`
  );

SELECT CONCAT('✅ Technicien: ', ROW_COUNT(), ' permission(s) assignée(s)') as Resultat;

-- ============================================================================
-- 5. VÉRIFICATION DES ASSOCIATIONS USERROLES
-- ============================================================================

SELECT '════════════════════════════════════════════════════════════' as Separator;
SELECT '👤 VÉRIFICATION DES ASSOCIATIONS USERROLES' as Title;
SELECT '════════════════════════════════════════════════════════════' as Separator;

-- Utilisateurs sans association UserRole
SELECT 
    '⚠️ Utilisateurs sans association UserRole:' as Info;
SELECT 
    u.`IdUtilisateur`,
    u.`NomComplet`,
    u.`Email`,
    u.`DefaultUsername`,
    r.`Nom` as Role_Attribue,
    CASE 
        WHEN ur.`IdUserRole` IS NULL THEN '❌ MANQUANT'
        ELSE '✅ OK'
    END as Statut_UserRole
FROM `Utilisateurs` u
LEFT JOIN `Roles` r ON u.`IdRole` = r.`IdRole`
LEFT JOIN `UserRoles` ur ON u.`IdUtilisateur` = ur.`IdUtilisateur` AND r.`IdRole` = ur.`IdRole`
WHERE u.`Statut` = 1
  AND ur.`IdUserRole` IS NULL
  AND r.`IdRole` IS NOT NULL;

-- Corriger les associations UserRole manquantes
INSERT IGNORE INTO `UserRoles` (
    `IdUtilisateur`,
    `IdRole`,
    `IsPrimary`,
    `DateAttribution`,
    `Statut`
)
SELECT 
    u.`IdUtilisateur`,
    u.`IdRole`,
    1, -- Rôle principal
    NOW(),
    1 -- Statut actif
FROM `Utilisateurs` u
INNER JOIN `Roles` r ON u.`IdRole` = r.`IdRole`
WHERE u.`Statut` = 1
  AND u.`IdRole` IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 
      FROM `UserRoles` ur 
      WHERE ur.`IdUtilisateur` = u.`IdUtilisateur 
        AND ur.`IdRole` = u.`IdRole`
  );

SELECT CONCAT('✅ UserRoles: ', ROW_COUNT(), ' association(s) créée(s)') as Resultat;

-- ============================================================================
-- 6. RAPPORT FINAL
-- ============================================================================

SELECT '════════════════════════════════════════════════════════════' as Separator;
SELECT '📊 RAPPORT FINAL' as Title;
SELECT '════════════════════════════════════════════════════════════' as Separator;

-- Assignations finales par rôle
SELECT 
    '🔗 Assignations finales de permissions par rôle:' as Info;
SELECT 
    r.`Nom` as Role,
    COUNT(rp.`IdRolePermission`) as Permissions_Assignees,
    (SELECT COUNT(*) FROM `Permissions` WHERE `Statut` = 1) as Permissions_Total,
    CASE 
        WHEN r.`Nom` = 'Super-Admin' THEN 
            CASE 
                WHEN COUNT(rp.`IdRolePermission`) = (SELECT COUNT(*) FROM `Permissions` WHERE `Statut` = 1) 
                THEN '✅ COMPLET'
                ELSE '⚠️ INCOMPLET'
            END
        ELSE '✅ OK'
    END as Statut
FROM `Roles` r
LEFT JOIN `RolePermissions` rp ON r.`IdRole` = rp.`IdRole`
WHERE r.`Statut` = 1
GROUP BY r.`IdRole`, r.`Nom`
ORDER BY r.`Niveau`;

-- Utilisateurs avec leurs rôles et permissions
SELECT 
    '👥 Utilisateurs avec leurs rôles et permissions:' as Info;
SELECT 
    u.`IdUtilisateur`,
    u.`NomComplet`,
    u.`Email`,
    r.`Nom` as Role,
    COUNT(rp.`IdRolePermission`) as Permissions_Disponibles,
    CASE 
        WHEN ur.`IdUserRole` IS NOT NULL THEN '✅ UserRole OK'
        ELSE '❌ UserRole MANQUANT'
    END as Statut_UserRole
FROM `Utilisateurs` u
LEFT JOIN `Roles` r ON u.`IdRole` = r.`IdRole`
LEFT JOIN `UserRoles` ur ON u.`IdUtilisateur` = ur.`IdUtilisateur` AND r.`IdRole` = ur.`IdRole`
LEFT JOIN `RolePermissions` rp ON r.`IdRole` = rp.`IdRole`
WHERE u.`Statut` = 1
GROUP BY u.`IdUtilisateur`, u.`NomComplet`, u.`Email`, r.`Nom`, ur.`IdUserRole`
ORDER BY u.`IdUtilisateur`;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 1;
COMMIT;

SELECT '════════════════════════════════════════════════════════════' as Separator;
SELECT '✅ VÉRIFICATION ET CORRECTION TERMINÉES' as Title;
SELECT '════════════════════════════════════════════════════════════' as Separator;
SELECT '📝 Prochaines étapes:' as Info;
SELECT '   1. Vérifiez les rapports ci-dessus' as Step;
SELECT '   2. Testez l''authentification pour vérifier les permissions' as Step;
SELECT '   3. Si des problèmes persistent, contactez le support' as Step;

-- ============================================================================
-- NOTES IMPORTANTES:
-- ============================================================================
-- 1. Ce script vérifie et corrige automatiquement les assignations manquantes
-- 2. Super-Admin doit avoir TOUTES les permissions (aucune restriction)
-- 3. Les autres rôles ont des permissions limitées selon leur fonction
-- 4. Les associations UserRoles sont créées automatiquement si manquantes
-- 5. Ce script peut être exécuté plusieurs fois sans erreur (INSERT IGNORE)
--
-- ============================================================================

