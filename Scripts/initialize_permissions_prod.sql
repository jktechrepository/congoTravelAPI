-- ============================================================================
-- INITIALISATION / RE-ASSIGNATION DES PERMISSIONS EN PRODUCTION
-- Base: MySQL / MariaDB
-- Idempotent : peut être relancé sans casser l'existant
-- Exécution : USE CongoTravelDB; puis source scripts/initialize_permissions_prod.sql;
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;

-- ============================================================================
-- 1) CRÉATION DES RÔLES DE RÉFÉRENCE
--    Les Niveaux sont indicatifs pour conserver l'ordre hiérarchique.
-- ============================================================================
INSERT INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
SELECT 'Super-Admin', 'Rôle racine, toutes permissions', 1, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Roles` WHERE `Nom` = 'Super-Admin');

INSERT INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
SELECT 'Admin', 'Gestion complète de son entité', 2, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Roles` WHERE `Nom` = 'Admin');

INSERT INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
SELECT 'Gerant', 'Gestion opérationnelle sans suppression critique', 3, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Roles` WHERE `Nom` = 'Gerant');

INSERT INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
SELECT 'Financier', 'Gestion financière (lecture/création factures)', 4, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Roles` WHERE `Nom` = 'Financier');

INSERT INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
SELECT 'Caissier', 'Gestion des paiements et validations', 5, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Roles` WHERE `Nom` = 'Caissier');

INSERT INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
SELECT 'Technicien', 'Support technique (lecture)', 6, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Roles` WHERE `Nom` = 'Technicien');

INSERT INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
SELECT 'Client', 'Accès client en lecture', 7, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Roles` WHERE `Nom` = 'Client');

-- Récupérer les IDs des rôles
SET @role_super_admin = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Super-Admin' LIMIT 1);
SET @role_admin       = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Admin' LIMIT 1);
SET @role_gerant      = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Gerant' LIMIT 1);
SET @role_financier   = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Financier' LIMIT 1);
SET @role_caissier    = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Caissier' LIMIT 1);
SET @role_technicien  = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Technicien' LIMIT 1);
SET @role_client      = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Client' LIMIT 1);

-- ============================================================================
-- 2) CRÉATION / MISE À JOUR DES PERMISSIONS (liste issue du PermissionSeeder)
-- ============================================================================
-- Helper macro : INSERT si absente
-- Societe
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Societe.Create', 'Societe', 'Create', 'Créer une société', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Societe.Create');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Societe.Read', 'Societe', 'Read', 'Voir une société', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Societe.Read');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Societe.ReadAll', 'Societe', 'ReadAll', 'Voir toutes les sociétés', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Societe.ReadAll');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Societe.Update', 'Societe', 'Update', 'Modifier une société', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Societe.Update');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Societe.Delete', 'Societe', 'Delete', 'Supprimer une société', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Societe.Delete');

-- Utilisateur
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Utilisateur.Create', 'Utilisateur', 'Create', 'Créer un utilisateur', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Utilisateur.Create');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Utilisateur.Read', 'Utilisateur', 'Read', 'Voir un utilisateur', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Utilisateur.Read');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Utilisateur.ReadAll', 'Utilisateur', 'ReadAll', 'Voir tous les utilisateurs', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Utilisateur.ReadAll');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Utilisateur.Update', 'Utilisateur', 'Update', 'Modifier un utilisateur', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Utilisateur.Update');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Utilisateur.Delete', 'Utilisateur', 'Delete', 'Supprimer un utilisateur', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Utilisateur.Delete');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Utilisateur.ChangePassword', 'Utilisateur', 'ChangePassword', 'Changer le mot de passe', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Utilisateur.ChangePassword');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Utilisateur.DeactivateSelf', 'Utilisateur', 'DeactivateSelf', 'Désactiver son propre compte utilisateur', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Utilisateur.DeactivateSelf');

-- Agent
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Agent.Create', 'Agent', 'Create', 'Créer un agent', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Agent.Create');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Agent.Read', 'Agent', 'Read', 'Voir un agent', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Agent.Read');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Agent.ReadAll', 'Agent', 'ReadAll', 'Voir tous les agents', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Agent.ReadAll');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Agent.Update', 'Agent', 'Update', 'Modifier un agent', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Agent.Update');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Agent.Delete', 'Agent', 'Delete', 'Supprimer un agent', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Agent.Delete');

-- Client
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Client.Create', 'Client', 'Create', 'Créer un client', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Client.Create');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Client.Read', 'Client', 'Read', 'Voir un client', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Client.Read');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Client.ReadAll', 'Client', 'ReadAll', 'Voir tous les clients', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Client.ReadAll');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Client.Update', 'Client', 'Update', 'Modifier un client', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Client.Update');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Client.Delete', 'Client', 'Delete', 'Supprimer un client', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Client.Delete');

-- CategorieClient
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'CategorieClient.Create', 'CategorieClient', 'Create', 'Créer une catégorie client', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'CategorieClient.Create');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'CategorieClient.Read', 'CategorieClient', 'Read', 'Voir une catégorie client', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'CategorieClient.Read');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'CategorieClient.ReadAll', 'CategorieClient', 'ReadAll', 'Voir toutes les catégories clients', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'CategorieClient.ReadAll');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'CategorieClient.Update', 'CategorieClient', 'Update', 'Modifier une catégorie client', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'CategorieClient.Update');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'CategorieClient.Delete', 'CategorieClient', 'Delete', 'Supprimer une catégorie client', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'CategorieClient.Delete');

-- Facture
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Facture.Create', 'Facture', 'Create', 'Créer une facture', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Facture.Create');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Facture.Read', 'Facture', 'Read', 'Voir une facture', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Facture.Read');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Facture.ReadAll', 'Facture', 'ReadAll', 'Voir toutes les factures', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Facture.ReadAll');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Facture.Update', 'Facture', 'Update', 'Modifier une facture', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Facture.Update');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Facture.Delete', 'Facture', 'Delete', 'Supprimer une facture', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Facture.Delete');

-- Role
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Role.Create', 'Role', 'Create', 'Créer un rôle', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Role.Create');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Role.Read', 'Role', 'Read', 'Voir un rôle', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Role.Read');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Role.ReadAll', 'Role', 'ReadAll', 'Voir tous les rôles', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Role.ReadAll');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Role.Update', 'Role', 'Update', 'Modifier un rôle', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Role.Update');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Role.Delete', 'Role', 'Delete', 'Supprimer un rôle', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Role.Delete');

-- Permission
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Permission.Create', 'Permission', 'Create', 'Créer une permission', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Permission.Create');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Permission.Read', 'Permission', 'Read', 'Voir une permission', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Permission.Read');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Permission.ReadAll', 'Permission', 'ReadAll', 'Voir toutes les permissions', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Permission.ReadAll');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Permission.Update', 'Permission', 'Update', 'Modifier une permission', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Permission.Update');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Permission.Delete', 'Permission', 'Delete', 'Supprimer une permission', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Permission.Delete');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Permission.Assign', 'Permission', 'Assign', 'Assigner une permission à un rôle', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Permission.Assign');
INSERT INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
SELECT 'Permission.Revoke', 'Permission', 'Revoke', 'Retirer une permission d''un rôle', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `Permissions` WHERE `Nom` = 'Permission.Revoke');

-- ============================================================================
-- 3) ASSIGNATION DES PERMISSIONS AUX RÔLES
--    Logique alignée sur PermissionSeeder.AssignPermissionsToRolesAsync
-- ============================================================================
-- Helper: assigner toutes les permissions filtrées si absentes

-- Super-Admin : toutes les permissions actives
INSERT INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @role_super_admin, p.`IdPermission`, NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @role_super_admin IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM `RolePermissions` rp
      WHERE rp.`IdRole` = @role_super_admin AND rp.`IdPermission` = p.`IdPermission`
  );

-- Admin : tout sauf création/suppression de société
INSERT INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @role_admin, p.`IdPermission`, NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @role_admin IS NOT NULL
  AND (
      (p.`Categorie` = 'Societe' AND p.`Action` IN ('Read','ReadAll','Update')) OR
      p.`Categorie` IN ('Utilisateur','Agent','Client','CategorieClient','Facture')
  )
  AND NOT EXISTS (
      SELECT 1 FROM `RolePermissions` rp
      WHERE rp.`IdRole` = @role_admin AND rp.`IdPermission` = p.`IdPermission`
  );

-- Gerant : comme Admin mais sans Facture.Update/Delete
INSERT INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @role_gerant, p.`IdPermission`, NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @role_gerant IS NOT NULL
  AND (
      (p.`Categorie` = 'Societe' AND p.`Action` IN ('Read','ReadAll','Update')) OR
      p.`Categorie` IN ('Utilisateur','Agent','Client','CategorieClient') OR
      (p.`Categorie` = 'Facture' AND p.`Action` NOT IN ('Update','Delete'))
  )
  AND NOT EXISTS (
      SELECT 1 FROM `RolePermissions` rp
      WHERE rp.`IdRole` = @role_gerant AND rp.`IdPermission` = p.`IdPermission`
  );

-- Financier : lecture/création factures + lecture clients/catégories
INSERT INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @role_financier, p.`IdPermission`, NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @role_financier IS NOT NULL
  AND (
      (p.`Categorie` = 'Facture' AND p.`Action` NOT IN ('Update','Delete')) OR
      (p.`Categorie` = 'Client' AND p.`Action` IN ('Read','ReadAll')) OR
      (p.`Categorie` = 'CategorieClient' AND p.`Action` IN ('Read','ReadAll'))
  )
  AND NOT EXISTS (
      SELECT 1 FROM `RolePermissions` rp
      WHERE rp.`IdRole` = @role_financier AND rp.`IdPermission` = p.`IdPermission`
  );

-- Caissier : lecture/maj factures + lecture clients/catégories
INSERT INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @role_caissier, p.`IdPermission`, NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @role_caissier IS NOT NULL
  AND (
      (p.`Categorie` = 'Facture' AND p.`Action` IN ('Read','ReadAll','Update')) OR
      (p.`Categorie` = 'Client' AND p.`Action` IN ('Read','ReadAll')) OR
      (p.`Categorie` = 'CategorieClient' AND p.`Action` IN ('Read','ReadAll'))
  )
  AND NOT EXISTS (
      SELECT 1 FROM `RolePermissions` rp
      WHERE rp.`IdRole` = @role_caissier AND rp.`IdPermission` = p.`IdPermission`
  );

-- Technicien : lecture sur Societe / Agent / Utilisateur
INSERT INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @role_technicien, p.`IdPermission`, NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @role_technicien IS NOT NULL
  AND (
      (p.`Categorie` = 'Agent' AND p.`Action` IN ('Read','ReadAll')) OR
      (p.`Categorie` = 'Societe' AND p.`Action` IN ('Read','ReadAll')) OR
      (p.`Categorie` = 'Utilisateur' AND p.`Action` IN ('Read','ReadAll'))
  )
  AND NOT EXISTS (
      SELECT 1 FROM `RolePermissions` rp
      WHERE rp.`IdRole` = @role_technicien AND rp.`IdPermission` = p.`IdPermission`
  );

-- Client : CongoTravel transport (sans legacy Facture / CategorieClient)
DELETE rp FROM `RolePermissions` rp
INNER JOIN `Permissions` p ON p.`IdPermission` = rp.`IdPermission`
WHERE rp.`IdRole` = @role_client
  AND @role_client IS NOT NULL
  AND p.`Categorie` IN ('Facture', 'CategorieClient');

INSERT INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @role_client, p.`IdPermission`, NOW()
FROM `Permissions` p
WHERE p.`Statut` = 1
  AND @role_client IS NOT NULL
  AND (
      (p.`Categorie` = 'Client' AND p.`Action` IN ('Read','ReadAll')) OR
      (p.`Categorie` = 'PlainteClient' AND p.`Action` IN ('Create','Read','ReadAll')) OR
      (p.`Categorie` = 'ClientDashboard' AND p.`Action` = 'ReadAll') OR
      (p.`Categorie` = 'Reservation' AND p.`Action` IN ('Create','Read','ReadAll')) OR
      (p.`Categorie` = 'Paiement' AND p.`Action` IN ('Read','ReadAll')) OR
      (p.`Categorie` = 'Billet' AND p.`Action` IN ('Read','ReadAll')) OR
      (p.`Categorie` = 'Voyage' AND p.`Action` IN ('Read','ReadAll')) OR
      (p.`Categorie` = 'Destination' AND p.`Action` IN ('Read','ReadAll'))
  )
  AND NOT EXISTS (
      SELECT 1 FROM `RolePermissions` rp
      WHERE rp.`IdRole` = @role_client AND rp.`IdPermission` = p.`IdPermission`
  );

-- Utilisateur.DeactivateSelf : tous les rôles actifs (auto-désactivation compte)
INSERT INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, NOW()
FROM `Roles` r
INNER JOIN `Permissions` p ON p.`Nom` = 'Utilisateur.DeactivateSelf' AND p.`Statut` = 1
WHERE r.`Statut` = 1
  AND NOT EXISTS (
      SELECT 1 FROM `RolePermissions` rp
      WHERE rp.`IdRole` = r.`IdRole` AND rp.`IdPermission` = p.`IdPermission`
  );

-- ============================================================================
-- 4) FINAL
-- ============================================================================
COMMIT;
SET FOREIGN_KEY_CHECKS = 1;

SELECT '✅ Permissions et rôles synchronisés (script production idempotent)' AS Resultat;

