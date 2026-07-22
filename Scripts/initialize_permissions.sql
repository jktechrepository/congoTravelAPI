-- ============================================================================
-- Script d'initialisation des permissions RBAC - CongoTravel API
-- Base de données: MariaDB / MySQL
-- Version: 1.0
-- Date: Décembre 2025
-- ============================================================================
-- 
-- INSTRUCTIONS:
-- 1. Assurez-vous d'avoir exécuté initialize_default_data.sql d'abord
-- 2. Utilisez la base de données: USE CongoTravelDB;
-- 3. Exécutez ce script pour initialiser toutes les permissions
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
-- 1. CRÉER TOUTES LES PERMISSIONS (67 permissions)
-- ============================================================================

-- Vérifier si des permissions existent déjà
SET @permissions_exist = (SELECT COUNT(*) FROM `Permissions`);

-- Si des permissions existent déjà, on ne fait rien
-- Sinon, on insère toutes les permissions

-- ═══════════════════════════════════════════════════════════════════
-- SOCIETE - 5 permissions
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('Societe.Create', 'Societe', 'Create', 'Créer une école', 1, NOW()),
    ('Societe.Read', 'Societe', 'Read', 'Voir les informations d''une école', 1, NOW()),
    ('Societe.ReadAll', 'Societe', 'ReadAll', 'Voir toutes les écoles', 1, NOW()),
    ('Societe.Update', 'Societe', 'Update', 'Modifier une école', 1, NOW()),
    ('Societe.Delete', 'Societe', 'Delete', 'Supprimer une école', 1, NOW());

-- ═══════════════════════════════════════════════════════════════════
-- UTILISATEUR - 6 permissions
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('Utilisateur.Create', 'Utilisateur', 'Create', 'Créer un utilisateur', 1, NOW()),
    ('Utilisateur.Read', 'Utilisateur', 'Read', 'Voir un utilisateur', 1, NOW()),
    ('Utilisateur.ReadAll', 'Utilisateur', 'ReadAll', 'Voir tous les utilisateurs', 1, NOW()),
    ('Utilisateur.Update', 'Utilisateur', 'Update', 'Modifier un utilisateur', 1, NOW()),
    ('Utilisateur.Delete', 'Utilisateur', 'Delete', 'Supprimer un utilisateur', 1, NOW()),
    ('Utilisateur.ChangePassword', 'Utilisateur', 'ChangePassword', 'Changer le mot de passe d''un utilisateur', 1, NOW()),
    ('Utilisateur.DeactivateSelf', 'Utilisateur', 'DeactivateSelf', 'Désactiver son propre compte utilisateur', 1, NOW());

-- ═══════════════════════════════════════════════════════════════════
-- AGENT - 5 permissions
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('Agent.Create', 'Agent', 'Create', 'Créer un agent', 1, NOW()),
    ('Agent.Read', 'Agent', 'Read', 'Voir un agent', 1, NOW()),
    ('Agent.ReadAll', 'Agent', 'ReadAll', 'Voir tous les agents', 1, NOW()),
    ('Agent.Update', 'Agent', 'Update', 'Modifier un agent', 1, NOW()),
    ('Agent.Delete', 'Agent', 'Delete', 'Supprimer un agent', 1, NOW());

-- ═══════════════════════════════════════════════════════════════════
-- CLIENT - 5 permissions
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('Client.Create', 'Client', 'Create', 'Créer un client', 1, NOW()),
    ('Client.Read', 'Client', 'Read', 'Voir un client', 1, NOW()),
    ('Client.ReadAll', 'Client', 'ReadAll', 'Voir tous les clients', 1, NOW()),
    ('Client.Update', 'Client', 'Update', 'Modifier un client', 1, NOW()),
    ('Client.Delete', 'Client', 'Delete', 'Supprimer un client', 1, NOW());

-- ═══════════════════════════════════════════════════════════════════
-- CATEGORIE CLIENT - 5 permissions
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('CategorieClient.Create', 'CategorieClient', 'Create', 'Créer une catégorie de client', 1, NOW()),
    ('CategorieClient.Read', 'CategorieClient', 'Read', 'Voir une catégorie de client', 1, NOW()),
    ('CategorieClient.ReadAll', 'CategorieClient', 'ReadAll', 'Voir toutes les catégories de clients', 1, NOW()),
    ('CategorieClient.Update', 'CategorieClient', 'Update', 'Modifier une catégorie de client', 1, NOW()),
    ('CategorieClient.Delete', 'CategorieClient', 'Delete', 'Supprimer une catégorie de client', 1, NOW());

-- ═══════════════════════════════════════════════════════════════════
-- FACTURE - 5 permissions
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('Facture.Create', 'Facture', 'Create', 'Créer une facture', 1, NOW()),
    ('Facture.Read', 'Facture', 'Read', 'Voir une facture', 1, NOW()),
    ('Facture.ReadAll', 'Facture', 'ReadAll', 'Voir toutes les factures', 1, NOW()),
    ('Facture.Update', 'Facture', 'Update', 'Modifier une facture', 1, NOW()),
    ('Facture.Delete', 'Facture', 'Delete', 'Supprimer une facture', 1, NOW());

-- ═══════════════════════════════════════════════════════════════════
-- ROLE - 5 permissions
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('Role.Create', 'Role', 'Create', 'Créer un rôle', 1, NOW()),
    ('Role.Read', 'Role', 'Read', 'Voir un rôle', 1, NOW()),
    ('Role.ReadAll', 'Role', 'ReadAll', 'Voir tous les rôles', 1, NOW()),
    ('Role.Update', 'Role', 'Update', 'Modifier un rôle', 1, NOW()),
    ('Role.Delete', 'Role', 'Delete', 'Supprimer un rôle', 1, NOW());

-- ═══════════════════════════════════════════════════════════════════
-- PERMISSION - 7 permissions
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('Permission.Create', 'Permission', 'Create', 'Créer une permission', 1, NOW()),
    ('Permission.Read', 'Permission', 'Read', 'Voir une permission', 1, NOW()),
    ('Permission.ReadAll', 'Permission', 'ReadAll', 'Voir toutes les permissions', 1, NOW()),
    ('Permission.Update', 'Permission', 'Update', 'Modifier une permission', 1, NOW()),
    ('Permission.Delete', 'Permission', 'Delete', 'Supprimer une permission', 1, NOW()),
    ('Permission.Assign', 'Permission', 'Assign', 'Assigner une permission à un rôle', 1, NOW()),
    ('Permission.Revoke', 'Permission', 'Revoke', 'Retirer une permission d''un rôle', 1, NOW());


-- ============================================================================
-- 2. ASSIGNER LES PERMISSIONS AUX RÔLES
-- ============================================================================

-- Récupérer les IDs des rôles
SET @super_admin_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Super-Admin' LIMIT 1);
SET @admin_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Admin' LIMIT 1);
SET @gerant_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Gerant' LIMIT 1);
SET @financier_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Financier' LIMIT 1);
SET @caissier_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Caissier' LIMIT 1);
SET @client_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Client' LIMIT 1);
SET @technicien_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Technicien' LIMIT 1);

-- ═══════════════════════════════════════════════════════════════════
-- 🔴 SUPER-ADMIN : TOUTES LES PERMISSIONS (Root User - Aucune restriction)
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @super_admin_role_id, `IdPermission`, NOW()
FROM `Permissions`
WHERE @super_admin_role_id IS NOT NULL;

-- ═══════════════════════════════════════════════════════════════════
-- 🔵 ADMIN : Gestion complète de son école (sauf création/suppression d'écoles)
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @admin_role_id, `IdPermission`, NOW()
FROM `Permissions`
WHERE @admin_role_id IS NOT NULL
  AND (
    -- Écoles : Lecture et modification uniquement (pas création/suppression)
    (`Categorie` = 'Societe' AND `Action` IN ('Read', 'ReadAll', 'Update'))
    OR
    -- Gestion complète de son école
    `Categorie` IN ('Utilisateur', 'Agent', 'Client', 'CategorieClient', 'Facture')
  );

-- ═══════════════════════════════════════════════════════════════════
-- 🟢 GERANT : Mêmes permissions que Admin, sauf modification/suppression de factures
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @gerant_role_id, `IdPermission`, NOW()
FROM `Permissions`
WHERE @gerant_role_id IS NOT NULL
  AND (
    -- Écoles : Lecture et modification uniquement (pas création/suppression)
    (`Categorie` = 'Societe' AND `Action` IN ('Read', 'ReadAll', 'Update'))
    OR
    -- Gestion complète de son école
    `Categorie` IN ('Utilisateur', 'Agent', 'Client', 'CategorieClient')
    OR
    -- Factures : Création et lecture uniquement (PAS modification ni suppression)
    (`Categorie` = 'Facture' AND `Action` NOT IN ('Update', 'Delete'))
  );

-- ═══════════════════════════════════════════════════════════════════
-- 🟡 CAISSIER : Gestion des factures et transactions
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @caissier_role_id, `IdPermission`, NOW()
FROM `Permissions`
WHERE @caissier_role_id IS NOT NULL
  AND (
    -- Factures : Création et lecture uniquement (PAS modification ni suppression)
    (`Categorie` = 'Facture' AND `Action` NOT IN ('Update', 'Delete'))
    OR
    -- Clients : Lecture seule (pour vérifier les factures)
    (`Categorie` = 'Client' AND `Action` IN ('Read', 'ReadAll'))
    OR
    -- Catégorie Clients : Lecture seule
    (`Categorie` = 'CategorieClient' AND `Action` IN ('Read', 'ReadAll'))
  );

-- ═══════════════════════════════════════════════════════════════════
-- 🟠 FINANCIER : Gestion financière (Factures)
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @financier_role_id, `IdPermission`, NOW()
FROM `Permissions`
WHERE @financier_role_id IS NOT NULL
  AND (
    -- Factures : Création et lecture uniquement (PAS modification ni suppression)
    (`Categorie` = 'Facture' AND `Action` NOT IN ('Update', 'Delete'))
    OR
    -- Clients : Lecture seule (pour vérifier les factures)
    (`Categorie` = 'Client' AND `Action` IN ('Read', 'ReadAll'))
    OR
    -- Catégorie Clients : Lecture seule
    (`Categorie` = 'CategorieClient' AND `Action` IN ('Read', 'ReadAll'))
  );

-- ═══════════════════════════════════════════════════════════════════
-- 🔵 CLIENT : CongoTravel transport (profil, réservations, billets)
-- ═══════════════════════════════════════════════════════════════════
DELETE rp FROM `RolePermissions` rp
INNER JOIN `Permissions` p ON p.`IdPermission` = rp.`IdPermission`
WHERE rp.`IdRole` = @client_role_id
  AND @client_role_id IS NOT NULL
  AND p.`Categorie` IN ('Facture', 'CategorieClient');

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @client_role_id, `IdPermission`, NOW()
FROM `Permissions`
WHERE @client_role_id IS NOT NULL
  AND (
    (`Categorie` = 'Client' AND `Action` IN ('Read', 'ReadAll'))
    OR (`Categorie` = 'PlainteClient' AND `Action` IN ('Create', 'Read', 'ReadAll'))
    OR (`Categorie` = 'ClientDashboard' AND `Action` = 'ReadAll')
    OR (`Categorie` = 'Reservation' AND `Action` IN ('Create', 'Read', 'ReadAll'))
    OR (`Categorie` = 'Paiement' AND `Action` IN ('Read', 'ReadAll'))
    OR (`Categorie` = 'Billet' AND `Action` IN ('Read', 'ReadAll'))
    OR (`Categorie` = 'Voyage' AND `Action` IN ('Read', 'ReadAll'))
    OR (`Categorie` = 'Destination' AND `Action` IN ('Read', 'ReadAll'))
  );

-- ═══════════════════════════════════════════════════════════════════
-- 🟣 TECHNICIEN : Maintenance et support technique
-- ═══════════════════════════════════════════════════════════════════
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @technicien_role_id, `IdPermission`, NOW()
FROM `Permissions`
WHERE @technicien_role_id IS NOT NULL
  AND (
    -- Lecture seule sur les équipements et systèmes
    (`Categorie` = 'Agent' AND `Action` IN ('Read', 'ReadAll'))
    OR
    (`Categorie` = 'Societe' AND `Action` IN ('Read', 'ReadAll'))
    OR
    (`Categorie` = 'Utilisateur' AND `Action` IN ('Read', 'ReadAll'))
  );

-- Auto-désactivation compte : tous les rôles actifs
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, NOW()
FROM `Roles` r
INNER JOIN `Permissions` p ON p.`Nom` = 'Utilisateur.DeactivateSelf' AND p.`Statut` = 1
WHERE r.`Statut` = 1;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 1;
COMMIT;

-- ============================================================================
-- VÉRIFICATION DES DONNÉES CRÉÉES
-- ============================================================================

SELECT '✅ Permissions créées:' as Message;
SELECT COUNT(*) as NombrePermissions FROM `Permissions`;

SELECT '✅ Permissions par catégorie:' as Message;
SELECT `Categorie`, COUNT(*) as Nombre 
FROM `Permissions` 
GROUP BY `Categorie` 
ORDER BY `Categorie`;

SELECT '✅ Permissions assignées aux rôles:' as Message;
SELECT 
    r.`Nom` as Role,
    COUNT(rp.`IdRolePermission`) as NombrePermissions
FROM `Roles` r
LEFT JOIN `RolePermissions` rp ON r.`IdRole` = rp.`IdRole`
GROUP BY r.`IdRole`, r.`Nom`
ORDER BY r.`Niveau`;

-- ============================================================================
-- NOTES IMPORTANTES:
-- ============================================================================
-- 1. Ce script crée 38 permissions au total
-- 2. Les permissions sont assignées aux rôles selon leur niveau hiérarchique
-- 3. Super-Admin a TOUTES les permissions (aucune restriction)
-- 4. Les autres rôles ont des permissions limitées selon leur fonction
-- 5. Ce script peut être exécuté plusieurs fois sans erreur (INSERT IGNORE)
--
-- ============================================================================

