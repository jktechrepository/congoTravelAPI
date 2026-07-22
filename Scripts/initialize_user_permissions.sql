-- ============================================================================
-- Script d'initialisation des permissions personnalisées - CongoTravel API
-- Base de données: MariaDB / MySQL
-- Version: 1.0
-- Date: Décembre 2025
-- ============================================================================
-- 
-- INSTRUCTIONS:
-- 1. Assurez-vous d'avoir exécuté initialize_default_data.sql et initialize_permissions.sql
-- 2. Utilisez la base de données: USE CongoTravelDB;
-- 3. Ce script contient des EXEMPLES de permissions personnalisées
-- 4. Modifiez les exemples selon vos besoins avant d'exécuter
-- 
-- ⚠️  ATTENTION: Ce script est optionnel et contient des exemples
--    Les permissions personnalisées sont généralement gérées via l'API
-- 
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

-- ============================================================================
-- PERMISSIONS PERSONNALISÉES - EXEMPLES
-- ============================================================================
-- 
-- Les permissions personnalisées permettent de :
-- 1. Ajouter des permissions à un utilisateur (IsGranted = 1)
-- 2. Retirer des permissions à un utilisateur (IsGranted = 0)
-- 
-- Priorité : DENIED > GRANTED > ROLE
-- 
-- ============================================================================

-- Récupérer l'ID de l'utilisateur Super-Admin
SET @super_admin_user_id = (SELECT `IdUtilisateur` FROM `Utilisateurs` WHERE `Email` = 'superadmin@kenergie.cd' LIMIT 1);

-- ============================================================================
-- EXEMPLE 1 : Ajouter une permission spéciale à un utilisateur
-- ============================================================================
-- 
-- Scénario : Un Gerant a besoin temporairement de la permission Facture.Update
-- (normalement réservée aux Admins)
-- 
-- ⚠️  DÉCOMMENTEZ ET MODIFIEZ SELON VOS BESOINS
-- 
/*
SET @gerant_user_id = (SELECT `IdUtilisateur` FROM `Utilisateurs` WHERE `DefaultUsername` = 'nom_utilisateur_gerant' LIMIT 1);
SET @facture_update_permission_id = (SELECT `IdPermission` FROM `Permissions` WHERE `Nom` = 'Facture.Update' LIMIT 1);

INSERT IGNORE INTO `UserPermissions` (
    `IdUtilisateur`,
    `IdPermission`,
    `IsGranted`,
    `DateAttribution`,
    `DateExpiration`,
    `Commentaire`,
    `AttribueParIdUtilisateur`
)
VALUES (
    @gerant_user_id,
    @facture_update_permission_id,
    1, -- Permission accordée
    NOW(),
    DATE_ADD(NOW(), INTERVAL 30 DAY), -- Expire dans 30 jours (permission temporaire)
    'Permission temporaire pour projet spécial',
    @super_admin_user_id
);
*/

-- ============================================================================
-- EXEMPLE 2 : Retirer une permission à un utilisateur
-- ============================================================================
-- 
-- Scénario : Un Admin spécifique ne doit pas pouvoir supprimer des clients
-- (pour des raisons de sécurité ou de conformité)
-- 
-- ⚠️  DÉCOMMENTEZ ET MODIFIEZ SELON VOS BESOINS
-- 
/*
SET @admin_user_id = (SELECT `IdUtilisateur` FROM `Utilisateurs` WHERE `DefaultUsername` = 'nom_utilisateur_admin' LIMIT 1);
SET @client_delete_permission_id = (SELECT `IdPermission` FROM `Permissions` WHERE `Nom` = 'Client.Delete' LIMIT 1);

INSERT IGNORE INTO `UserPermissions` (
    `IdUtilisateur`,
    `IdPermission`,
    `IsGranted`,
    `DateAttribution`,
    `Commentaire`,
    `AttribueParIdUtilisateur`
)
VALUES (
    @admin_user_id,
    @client_delete_permission_id,
    0, -- Permission retirée (DENY)
    NOW(),
    'Restriction de sécurité - Ne peut pas supprimer des clients',
    @super_admin_user_id
);
*/

-- ============================================================================
-- EXEMPLE 3 : Permissions personnalisées pour un Caissier
-- ============================================================================
-- 
-- Scénario : Un Caissier expérimenté peut valider des factures
-- (normalement réservé aux Financiers)
-- 
-- ⚠️  DÉCOMMENTEZ ET MODIFIEZ SELON VOS BESOINS
-- 
/*
SET @caissier_user_id = (SELECT `IdUtilisateur` FROM `Utilisateurs` WHERE `DefaultUsername` = 'nom_utilisateur_caissier' LIMIT 1);
SET @facture_update_permission_id = (SELECT `IdPermission` FROM `Permissions` WHERE `Nom` = 'Facture.Update' LIMIT 1);

INSERT IGNORE INTO `UserPermissions` (
    `IdUtilisateur`,
    `IdPermission`,
    `IsGranted`,
    `DateAttribution`,
    `Commentaire`,
    `AttribueParIdUtilisateur`
)
VALUES (
    @caissier_user_id,
    @facture_update_permission_id,
    1, -- Permission accordée
    NOW(),
    'Caissier expérimenté - Peut modifier des factures',
    @super_admin_user_id
);
*/

-- ============================================================================
-- EXEMPLE 4 : Permissions temporaires pour un projet
-- ============================================================================
-- 
-- Scénario : Un Technicien a besoin temporairement de permissions Admin
-- pour un projet spécifique
-- 
-- ⚠️  DÉCOMMENTEZ ET MODIFIEZ SELON VOS BESOINS
-- 
/*
SET @technicien_user_id = (SELECT `IdUtilisateur` FROM `Utilisateurs` WHERE `DefaultUsername` = 'nom_utilisateur_technicien' LIMIT 1);

-- Ajouter plusieurs permissions temporaires
INSERT IGNORE INTO `UserPermissions` (
    `IdUtilisateur`,
    `IdPermission`,
    `IsGranted`,
    `DateAttribution`,
    `DateExpiration`,
    `Commentaire`,
    `AttribueParIdUtilisateur`
)
SELECT 
    @technicien_user_id,
    `IdPermission`,
    1,
    NOW(),
    DATE_ADD(NOW(), INTERVAL 90 DAY), -- Expire dans 90 jours
    'Permissions temporaires pour projet spécial',
    @super_admin_user_id
FROM `Permissions`
WHERE `Nom` IN ('Client.Create', 'Client.Update', 'Facture.ReadAll')
  AND @technicien_user_id IS NOT NULL;
*/

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 1;
COMMIT;

-- ============================================================================
-- VÉRIFICATION DES PERMISSIONS PERSONNALISÉES
-- ============================================================================

SELECT '✅ Permissions personnalisées créées:' as Message;
SELECT 
    up.`IdUserPermission`,
    u.`NomComplet` as Utilisateur,
    u.`Email`,
    p.`Nom` as Permission,
    CASE 
        WHEN up.`IsGranted` = 1 THEN '✅ Accordée'
        ELSE '🚫 Retirée'
    END as Statut,
    up.`DateAttribution`,
    up.`DateExpiration`,
    up.`Commentaire`,
    attribue_par.`NomComplet` as AttribuePar
FROM `UserPermissions` up
INNER JOIN `Utilisateurs` u ON up.`IdUtilisateur` = u.`IdUtilisateur`
INNER JOIN `Permissions` p ON up.`IdPermission` = p.`IdPermission`
LEFT JOIN `Utilisateurs` attribue_par ON up.`AttribueParIdUtilisateur` = attribue_par.`IdUtilisateur`
ORDER BY up.`DateAttribution` DESC;

-- ============================================================================
-- NOTES IMPORTANTES:
-- ============================================================================
-- 1. Ce script contient des EXEMPLES commentés
-- 2. Décommentez et modifiez les exemples selon vos besoins
-- 3. Les permissions personnalisées ont priorité sur les permissions du rôle
-- 4. IsGranted = 1 : Permission ajoutée (GRANT)
-- 5. IsGranted = 0 : Permission retirée (DENY)
-- 6. DateExpiration : Permet de créer des permissions temporaires
-- 7. Pour gérer les permissions personnalisées via l'API, utilisez :
--    - POST /api/Permission/grant-user-permission
--    - POST /api/Permission/deny-user-permission
--    - DELETE /api/Permission/remove-user-permission-override
--
-- ============================================================================

