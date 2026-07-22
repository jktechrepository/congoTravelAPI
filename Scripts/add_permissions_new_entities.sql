-- ============================================================================
-- Script d'ajout des permissions pour les nouvelles entités
-- Date : 2025-12-14
-- Description : Ajoute les permissions pour PlainteClient, CommunicationCampaign et PanneSignalement
-- ============================================================================
-- IMPORTANT : Ce script est IDEMPOTENT - peut être exécuté plusieurs fois sans erreur
-- ============================================================================

USE `FactureNormaliseeRDC`;

-- ============================================================================
-- PARTIE 1: Ajout des permissions PlainteClient
-- ============================================================================

INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('PlainteClient.Create', 'PlainteClient', 'Create', 'Créer une plainte client', 1, NOW()),
    ('PlainteClient.Read', 'PlainteClient', 'Read', 'Voir une plainte client', 1, NOW()),
    ('PlainteClient.ReadAll', 'PlainteClient', 'ReadAll', 'Voir toutes les plaintes clients', 1, NOW()),
    ('PlainteClient.Update', 'PlainteClient', 'Update', 'Modifier une plainte client', 1, NOW()),
    ('PlainteClient.Delete', 'PlainteClient', 'Delete', 'Supprimer une plainte client', 1, NOW());

-- ============================================================================
-- PARTIE 2: Ajout des permissions CommunicationCampaign
-- ============================================================================

INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('CommunicationCampaign.Create', 'CommunicationCampaign', 'Create', 'Créer une campagne de communication', 1, NOW()),
    ('CommunicationCampaign.Read', 'CommunicationCampaign', 'Read', 'Voir une campagne de communication', 1, NOW()),
    ('CommunicationCampaign.ReadAll', 'CommunicationCampaign', 'ReadAll', 'Voir toutes les campagnes de communication', 1, NOW()),
    ('CommunicationCampaign.Update', 'CommunicationCampaign', 'Update', 'Modifier une campagne de communication', 1, NOW()),
    ('CommunicationCampaign.Delete', 'CommunicationCampaign', 'Delete', 'Supprimer une campagne de communication', 1, NOW());

-- ============================================================================
-- PARTIE 3: Ajout des permissions PanneSignalement
-- ============================================================================

INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('PanneSignalement.Create', 'PanneSignalement', 'Create', 'Créer un signalement de panne', 1, NOW()),
    ('PanneSignalement.Read', 'PanneSignalement', 'Read', 'Voir un signalement de panne', 1, NOW()),
    ('PanneSignalement.ReadAll', 'PanneSignalement', 'ReadAll', 'Voir tous les signalements de panne', 1, NOW()),
    ('PanneSignalement.Update', 'PanneSignalement', 'Update', 'Modifier un signalement de panne', 1, NOW()),
    ('PanneSignalement.Delete', 'PanneSignalement', 'Delete', 'Supprimer un signalement de panne', 1, NOW());

-- ============================================================================
-- PARTIE 4: Assignation des permissions aux rôles
-- ============================================================================

-- Récupérer les IDs des rôles
SET @super_admin_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Super-Admin' LIMIT 1);
SET @admin_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Admin' LIMIT 1);
SET @gerant_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Gerant' LIMIT 1);
SET @technicien_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Technicien' LIMIT 1);
SET @client_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Client' LIMIT 1);

-- Récupérer les IDs des nouvelles permissions
SET @plainte_create_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'PlainteClient.Create' LIMIT 1);
SET @plainte_read_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'PlainteClient.Read' LIMIT 1);
SET @plainte_readall_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'PlainteClient.ReadAll' LIMIT 1);
SET @plainte_update_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'PlainteClient.Update' LIMIT 1);
SET @plainte_delete_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'PlainteClient.Delete' LIMIT 1);

SET @campaign_create_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'CommunicationCampaign.Create' LIMIT 1);
SET @campaign_read_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'CommunicationCampaign.Read' LIMIT 1);
SET @campaign_readall_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'CommunicationCampaign.ReadAll' LIMIT 1);
SET @campaign_update_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'CommunicationCampaign.Update' LIMIT 1);
SET @campaign_delete_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'CommunicationCampaign.Delete' LIMIT 1);

SET @panne_create_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'PanneSignalement.Create' LIMIT 1);
SET @panne_read_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'PanneSignalement.Read' LIMIT 1);
SET @panne_readall_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'PanneSignalement.ReadAll' LIMIT 1);
SET @panne_update_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'PanneSignalement.Update' LIMIT 1);
SET @panne_delete_id = (SELECT IdPermission FROM Permissions WHERE Nom = 'PanneSignalement.Delete' LIMIT 1);

-- SUPER-ADMIN : Toutes les permissions (déjà géré automatiquement par le seeder)
-- On les ajoute quand même pour être sûr
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @super_admin_role_id, IdPermission, NOW()
FROM Permissions
WHERE Categorie IN ('PlainteClient', 'CommunicationCampaign', 'PanneSignalement')
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions 
    WHERE IdRole = @super_admin_role_id AND IdPermission = Permissions.IdPermission
);

-- ADMIN : Toutes les permissions pour les nouvelles entités
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @admin_role_id, IdPermission, NOW()
FROM Permissions
WHERE Categorie IN ('PlainteClient', 'CommunicationCampaign', 'PanneSignalement')
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions 
    WHERE IdRole = @admin_role_id AND IdPermission = Permissions.IdPermission
);

-- GERANT : Toutes les permissions pour les nouvelles entités
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @gerant_role_id, IdPermission, NOW()
FROM Permissions
WHERE Categorie IN ('PlainteClient', 'CommunicationCampaign', 'PanneSignalement')
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions 
    WHERE IdRole = @gerant_role_id AND IdPermission = Permissions.IdPermission
);

-- TECHNICIEN : Toutes les permissions pour PlainteClient et PanneSignalement
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @technicien_role_id, IdPermission, NOW()
FROM Permissions
WHERE Categorie IN ('PlainteClient', 'PanneSignalement')
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions 
    WHERE IdRole = @technicien_role_id AND IdPermission = Permissions.IdPermission
);

-- CLIENT : Création et lecture pour PlainteClient et PanneSignalement
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @client_role_id, IdPermission, NOW()
FROM Permissions
WHERE (Categorie = 'PlainteClient' AND Action IN ('Create', 'Read', 'ReadAll'))
   OR (Categorie = 'PanneSignalement' AND Action IN ('Create', 'Read', 'ReadAll'))
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions 
    WHERE IdRole = @client_role_id AND IdPermission = Permissions.IdPermission
);

-- ============================================================================
-- VÉRIFICATION FINALE
-- ============================================================================

SELECT 
    'Permissions créées' AS Type,
    COUNT(*) AS Nombre
FROM Permissions
WHERE Categorie IN ('PlainteClient', 'CommunicationCampaign', 'PanneSignalement');

SELECT 
    r.Nom AS Role,
    COUNT(rp.IdPermission) AS NombrePermissions
FROM Roles r
LEFT JOIN RolePermissions rp ON r.IdRole = rp.IdRole
LEFT JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE p.Categorie IN ('PlainteClient', 'CommunicationCampaign', 'PanneSignalement')
GROUP BY r.IdRole, r.Nom
ORDER BY r.Nom;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

